using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Avalonia.McpProxy.Services;

/// <summary>
/// Launches and tracks Avalonia apps with diagnostic support.
/// Captures the diagnostic port from app stdout, then registers with connection manager.
/// </summary>
public partial class ProcessManager
{
    private readonly Action<string> _log;
    private readonly AppConnectionManager _connectionManager;

    public ProcessManager(Action<string> log, AppConnectionManager connectionManager)
    {
        _log = log;
        _connectionManager = connectionManager;
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

        var process = new Process { StartInfo = startInfo };
        var portTcs = new TaskCompletionSource<int>();
        var outputBuffer = new List<string>();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            
            outputBuffer.Add(e.Data);
            _log($"[{Path.GetFileName(path)}] {e.Data}");

            // Look for diagnostic port announcement
            var match = DiagnosticPortRegex().Match(e.Data);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var port))
            {
                portTcs.TrySetResult(port);
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

        // Wait for diagnostic port with timeout
        var portTask = portTcs.Task;
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), ct);

        var completed = await Task.WhenAny(portTask, timeoutTask);

        if (completed == portTask)
        {
            var port = await portTask;
            _log($"App listening on diagnostic port {port}");
            
            // Register with connection manager so it connects
            _connectionManager.RegisterLaunchedApp(port, process.Id, path);

            return JsonSerializer.Serialize(new
            {
                status = "launched",
                pid = process.Id,
                diagnostic_port = port,
                message = $"App launched and listening on port {port}"
            });
        }
        else
        {
            _log("Timeout waiting for diagnostic port - app may not support diagnostics");
            
            return JsonSerializer.Serialize(new
            {
                status = "launched_no_diagnostics",
                pid = process.Id,
                message = "App launched but did not announce a diagnostic port. It may not have diagnostic support enabled."
            });
        }
    }

    [GeneratedRegex(@"DIAGNOSTIC_PORT:(\d+)")]
    private static partial Regex DiagnosticPortRegex();
}
