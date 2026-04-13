using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.McpProxy.Services;

/// <summary>
/// Launches and tracks Avalonia apps.
/// Sets MCP_PROXY_ENDPOINT env var so apps connect back to the proxy.
/// Tracks all running process instances keyed by PID.
/// </summary>
public class ProcessManager
{
    private readonly Action<string> _log;
    private readonly int _proxyPort;
    private readonly ConcurrentDictionary<int, TrackedProcess> _processes = new();

    public ProcessManager(Action<string> log, int proxyPort)
    {
        _log = log;
        _proxyPort = proxyPort;
    }

    /// <summary>
    /// Launch a registered app, track the process, and return the result.
    /// </summary>
    public async Task<string> LaunchRegisteredAppAsync(RegisteredApp app, CancellationToken ct)
    {
        _log($"Launching registered app: {app.Name} ({app.Path} {app.Args})");

        var process = LaunchProcess(app.Path, app.Args, app.WorkingDirectory);

        if (process == null)
            return JsonSerializer.Serialize(new { status = "error", message = "Failed to start process" });

        TrackProcess(process, app.Name);

        // Wait briefly to check if the process is alive
        await Task.Delay(TimeSpan.FromSeconds(3), ct);

        CleanupExited();

        if (process.HasExited)
        {
            _processes.TryRemove(process.Id, out _);
            return JsonSerializer.Serialize(new
            {
                status = "exited",
                pid = process.Id,
                exit_code = process.ExitCode,
                message = $"App exited immediately with code {process.ExitCode}"
            });
        }

        return JsonSerializer.Serialize(new
        {
            status = "launched",
            pid = process.Id,
            app_name = app.Name,
            message = $"App '{app.Name}' launched (PID {process.Id}). It will connect back to proxy on port {_proxyPort}."
        });
    }

    /// <summary>
    /// Legacy ad-hoc launch from JSON arguments. Also tracks the PID.
    /// </summary>
    public async Task<string> LaunchAppAsync(JsonElement arguments, CancellationToken ct)
    {
        var path = arguments.GetProperty("path").GetString()!;
        var args = arguments.TryGetProperty("args", out var a) ? a.GetString() : null;
        var workingDir = arguments.TryGetProperty("working_directory", out var w) ? w.GetString() : null;
        var appName = arguments.TryGetProperty("name", out var n) ? n.GetString() ?? "AdHoc" : "AdHoc";

        _log($"Launching: {path} {args}");

        var process = LaunchProcess(path, args, workingDir);

        if (process == null)
            return JsonSerializer.Serialize(new { status = "error", message = "Failed to start process" });

        TrackProcess(process, appName);

        await Task.Delay(TimeSpan.FromSeconds(3), ct);

        CleanupExited();

        if (process.HasExited)
        {
            _processes.TryRemove(process.Id, out _);
            return JsonSerializer.Serialize(new
            {
                status = "exited",
                pid = process.Id,
                exit_code = process.ExitCode,
                message = $"App exited immediately with code {process.ExitCode}"
            });
        }

        return JsonSerializer.Serialize(new
        {
            status = "launched",
            pid = process.Id,
            message = $"App launched (PID {process.Id}). It will connect back to proxy on port {_proxyPort}."
        });
    }

    /// <summary>
    /// Get all tracked processes (cleans up exited ones first).
    /// </summary>
    public IReadOnlyList<TrackedProcess> GetRunningProcesses()
    {
        CleanupExited();
        return _processes.Values.ToList().AsReadOnly();
    }

    /// <summary>
    /// Get tracked processes for a specific registered app name.
    /// </summary>
    public IReadOnlyList<TrackedProcess> GetProcessesForApp(string appName)
    {
        CleanupExited();
        return _processes.Values
            .Where(p => p.AppName.Equals(appName, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Stop a specific process by PID.
    /// </summary>
    public bool StopProcess(int pid)
    {
        if (_processes.TryRemove(pid, out var tracked))
        {
            try
            {
                if (!tracked.ProcessRef.HasExited)
                {
                    tracked.ProcessRef.Kill(entireProcessTree: true);
                    _log($"Killed process PID {pid} ({tracked.AppName})");
                }
            }
            catch (Exception ex)
            {
                _log($"Error killing PID {pid}: {ex.Message}");
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Stop all processes for a given app name.
    /// </summary>
    public int StopAllForApp(string appName)
    {
        var pids = _processes.Values
            .Where(p => p.AppName.Equals(appName, StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Pid)
            .ToList();

        var killed = 0;
        foreach (var pid in pids)
        {
            if (StopProcess(pid))
                killed++;
        }

        return killed;
    }

    private Process? LaunchProcess(string path, string? args, string? workingDir)
    {
        try
        {
            // Use cmd /C with SET to pass env var AND UseShellExecute=true for proper Windows GUI context.
            // UseShellExecute=true is required so child processes get a desktop session
            // (without it, processes launched from WSL2-interop parents have no GUI).
            var cmdArgs = $"/C \"SET MCP_PROXY_ENDPOINT=tcp://localhost:{_proxyPort} && \"{path}\" {args ?? ""}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = cmdArgs,
                WorkingDirectory = workingDir ?? Path.GetDirectoryName(path) ?? "",
                UseShellExecute = true,
                CreateNoWindow = false
            };

            var process = new Process { StartInfo = startInfo };
            process.Start();

            _log($"Process started with PID {process.Id}");
            return process;
        }
        catch (Exception ex)
        {
            _log($"Failed to launch process: {ex.Message}");
            return null;
        }
    }

    private void TrackProcess(Process process, string appName)
    {
        var tracked = new TrackedProcess
        {
            Pid = process.Id,
            AppName = appName,
            StartedAt = DateTime.UtcNow,
            ProcessRef = process
        };
        _processes[process.Id] = tracked;
    }

    /// <summary>
    /// Remove exited processes from tracking.
    /// </summary>
    private void CleanupExited()
    {
        var exited = _processes.Values
            .Where(p => { try { return p.ProcessRef.HasExited; } catch { return true; } })
            .Select(p => p.Pid)
            .ToList();

        foreach (var pid in exited)
        {
            if (_processes.TryRemove(pid, out var removed))
            {
                _log($"Cleaned up exited process PID {pid} ({removed.AppName})");
            }
        }
    }
}

public class TrackedProcess
{
    public int Pid { get; init; }
    public string AppName { get; init; } = "";
    public DateTime StartedAt { get; init; }
    public Process ProcessRef { get; init; } = null!;
    public bool IsRunning
    {
        get { try { return !ProcessRef.HasExited; } catch { return false; } }
    }
}
