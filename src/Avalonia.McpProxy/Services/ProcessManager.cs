using System;
using System.Diagnostics;
using System.Text.Json;

namespace Avalonia.McpProxy.Services;

/// <summary>
/// Launches and tracks Avalonia apps.
/// Sets MCP_PROXY_ENDPOINT env var so apps connect back to the proxy.
/// </summary>
public class ProcessManager
{
    private readonly Action<string> _log;
    private readonly int _proxyPort;

    public ProcessManager(Action<string> log, int proxyPort)
    {
        _log = log;
        _proxyPort = proxyPort;
    }

    public async Task<string> LaunchAppAsync(JsonElement arguments, CancellationToken ct)
    {
        var path = arguments.GetProperty("path").GetString()!;
        var args = arguments.TryGetProperty("args", out var a) ? a.GetString() : null;
        var workingDir = arguments.TryGetProperty("working_directory", out var w) ? w.GetString() : null;

        _log($"Launching: {path}");

        var startInfo = new ProcessStartInfo
        {
            FileName = path,
            Arguments = args ?? "",
            WorkingDirectory = workingDir ?? Path.GetDirectoryName(path) ?? "",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = false
        };

        // Tell the app where to connect back to
        startInfo.EnvironmentVariables["MCP_PROXY_ENDPOINT"] = $"tcp://localhost:{_proxyPort}";

        var process = new Process { StartInfo = startInfo };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                _log($"[{Path.GetFileName(path)}] {e.Data}");
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                _log($"[{Path.GetFileName(path)}:ERR] {e.Data}");
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        _log($"Process started with PID {process.Id}");

        // Wait briefly to check if the process is alive
        await Task.Delay(TimeSpan.FromSeconds(3), ct);

        if (process.HasExited)
        {
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
}
