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

        _log($"Launching: {path} {args}");

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
