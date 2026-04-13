using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Mcp.Core.Models;

namespace Avalonia.McpProxy.Services;

/// <summary>
/// Bridges MCP stdio protocol to connected diagnostic apps.
/// Receives MCP requests from agents, routes to apps, returns responses.
/// Tools: register_app, unregister_app, list_apps, start_app, stop_process, list_processes,
/// plus app-specific tools routed via AppName:ToolName pattern.
/// </summary>
public class McpProxyBridge
{
    private readonly Action<string> _log;
    private readonly AppConnectionManager _connectionManager;
    private readonly AppRegistry _registry;
    private readonly ProcessManager _processManager;

    public McpProxyBridge(Action<string> log, AppConnectionManager connectionManager, AppRegistry registry)
    {
        _log = log;
        _connectionManager = connectionManager;
        _registry = registry;
        _processManager = new ProcessManager(log, connectionManager.ListenPort);
    }

    /// <summary>
    /// Expose the ProcessManager so the ViewModel can query process state.
    /// </summary>
    public ProcessManager ProcessManager => _processManager;

    public async Task StartAsync(CancellationToken ct)
    {
        _log("MCP bridge ready on stdio");
        
        using var stdin = Console.OpenStandardInput();
        using var stdout = Console.OpenStandardOutput();
        using var reader = new StreamReader(stdin, Encoding.UTF8);
        using var writer = new StreamWriter(stdout, Encoding.UTF8) { AutoFlush = true };

        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line == null) break;

            try
            {
                await HandleMessageAsync(line, writer, ct);
            }
            catch (Exception ex)
            {
                _log($"MCP error: {ex.Message}");
            }
        }
    }

    private async Task HandleMessageAsync(string message, StreamWriter writer, CancellationToken ct)
    {
        var json = JsonDocument.Parse(message);
        var root = json.RootElement;

        if (!root.TryGetProperty("method", out var methodEl))
            return;

        var method = methodEl.GetString();
        var id = root.TryGetProperty("id", out var idEl) ? idEl : default;

        switch (method)
        {
            case "initialize":
                await SendResponseAsync(writer, id, new
                {
                    protocolVersion = "2024-11-05",
                    capabilities = new { tools = new { listChanged = false } },
                    serverInfo = new { name = "avalonia-mcp-proxy", version = "3.0.0" }
                });
                break;

            case "tools/list":
                await HandleToolsListAsync(writer, id, ct);
                break;

            case "tools/call":
                await HandleToolCallAsync(writer, id, root, ct);
                break;
        }
    }

    private async Task HandleToolsListAsync(StreamWriter writer, JsonElement id, CancellationToken ct)
    {
        var tools = new List<object>();

        // --- Proxy management tools ---

        tools.Add(new
        {
            name = "register_app",
            description = "Register an app with the proxy. Registration is persistent and survives proxy restarts. Use start_app to launch a registered app.",
            inputSchema = new
            {
                type = "object",
                properties = new Dictionary<string, object>
                {
                    ["name"] = new { type = "string", description = "Unique name for the app (e.g. 'WatchTower')" },
                    ["path"] = new { type = "string", description = "Path to the application executable" },
                    ["args"] = new { type = "string", description = "Optional command line arguments" },
                    ["working_directory"] = new { type = "string", description = "Optional working directory" }
                },
                required = new[] { "name", "path" }
            }
        });

        tools.Add(new
        {
            name = "unregister_app",
            description = "Remove an app from the registry. Does not stop running instances.",
            inputSchema = new
            {
                type = "object",
                properties = new Dictionary<string, object>
                {
                    ["name"] = new { type = "string", description = "Name of the registered app to remove" }
                },
                required = new[] { "name" }
            }
        });

        tools.Add(new
        {
            name = "list_apps",
            description = "List all registered apps with their running instance count and connected tool count.",
            inputSchema = new { type = "object", properties = new Dictionary<string, object>() }
        });

        tools.Add(new
        {
            name = "start_app",
            description = "Start an instance of a registered app. The app will connect back to the proxy automatically.",
            inputSchema = new
            {
                type = "object",
                properties = new Dictionary<string, object>
                {
                    ["name"] = new { type = "string", description = "Name of the registered app to start" }
                },
                required = new[] { "name" }
            }
        });

        tools.Add(new
        {
            name = "stop_process",
            description = "Stop all running processes for a given app name and disconnect it from the proxy.",
            inputSchema = new
            {
                type = "object",
                properties = new Dictionary<string, object>
                {
                    ["app_name"] = new { type = "string", description = "Name of the app whose processes to stop" }
                },
                required = new[] { "app_name" }
            }
        });

        tools.Add(new
        {
            name = "list_processes",
            description = "List all running process instances grouped by app name, with connection status.",
            inputSchema = new { type = "object", properties = new Dictionary<string, object>() }
        });

        // --- App-specific tools from connected apps (namespaced as AppName:ToolName) ---
        foreach (var (appName, appTools) in _connectionManager.GetConnectedApps())
        {
            foreach (var tool in appTools)
            {
                tools.Add(new
                {
                    name = tool.Name,
                    description = tool.Description,
                    inputSchema = tool.InputSchema
                });
            }
        }

        await SendResponseAsync(writer, id, new { tools });
    }

    private async Task HandleToolCallAsync(StreamWriter writer, JsonElement id, JsonElement root, CancellationToken ct)
    {
        var paramsEl = root.GetProperty("params");
        var toolName = paramsEl.GetProperty("name").GetString()!;
        var arguments = paramsEl.TryGetProperty("arguments", out var args) ? args : default;

        string resultText;

        switch (toolName)
        {
            case "register_app":
                resultText = HandleRegisterApp(arguments);
                break;

            case "unregister_app":
                resultText = HandleUnregisterApp(arguments);
                break;

            case "list_apps":
                resultText = HandleListApps();
                break;

            case "start_app":
                resultText = await HandleStartAppAsync(arguments, ct);
                break;

            case "stop_process":
                resultText = HandleStopProcess(arguments);
                break;

            case "list_processes":
                resultText = HandleListProcesses();
                break;

            default:
                if (toolName.Contains(':'))
                {
                    // App-specific tool: "AppName:ToolName"
                    var parts = toolName.Split(':', 2);
                    var appName = parts[0];
                    var (success, data, error) = await _connectionManager.InvokeToolAsync(appName, toolName, arguments, ct);
                    resultText = success ? data! : JsonSerializer.Serialize(new { error = error ?? $"App '{appName}' not connected" });
                }
                else
                {
                    resultText = JsonSerializer.Serialize(new { error = $"Unknown tool: {toolName}" });
                }
                break;
        }

        await SendResponseAsync(writer, id, new
        {
            content = new[] { new { type = "text", text = resultText } }
        });
    }

    private string HandleRegisterApp(JsonElement arguments)
    {
        var name = arguments.GetProperty("name").GetString()!;
        var path = arguments.GetProperty("path").GetString()!;
        var args = arguments.TryGetProperty("args", out var a) ? a.GetString() : null;
        var workDir = arguments.TryGetProperty("working_directory", out var w) ? w.GetString() : null;

        var success = _registry.Register(name, path, args, workDir);
        return JsonSerializer.Serialize(new { success, name, message = $"App '{name}' registered. Use start_app to launch it." });
    }

    private string HandleUnregisterApp(JsonElement arguments)
    {
        var name = arguments.GetProperty("name").GetString()!;
        var success = _registry.Unregister(name);
        return JsonSerializer.Serialize(new
        {
            success,
            name,
            message = success ? $"App '{name}' unregistered." : $"App '{name}' not found in registry."
        });
    }

    private string HandleListApps()
    {
        var connectedApps = _connectionManager.GetConnectedApps().ToList();
        var apps = new List<object>();

        foreach (var reg in _registry.GetApps())
        {
            var processes = _processManager.GetProcessesForApp(reg.Name);
            var connected = connectedApps.FirstOrDefault(c =>
                c.Name.Equals(reg.Name, StringComparison.OrdinalIgnoreCase));

            apps.Add(new
            {
                name = reg.Name,
                path = reg.Path,
                args = reg.Args,
                working_directory = reg.WorkingDirectory,
                registered_at = reg.RegisteredAt,
                running_instances = processes.Count,
                pids = processes.Select(p => p.Pid).ToList(),
                connected = connected.Name != null,
                tool_count = connected.Tools?.Count ?? 0,
                tools = connected.Tools?.Select(t => t.Name).ToList() ?? new List<string>()
            });
        }

        return JsonSerializer.Serialize(new { apps, count = apps.Count });
    }

    private async Task<string> HandleStartAppAsync(JsonElement arguments, CancellationToken ct)
    {
        var name = arguments.GetProperty("name").GetString()!;
        var app = _registry.GetApp(name);

        if (app == null)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = $"App '{name}' not found in registry. Use register_app first."
            });
        }

        return await _processManager.LaunchRegisteredAppAsync(app, ct);
    }

    private string HandleStopProcess(JsonElement arguments)
    {
        var appName = arguments.GetProperty("app_name").GetString()!;
        _log($"Stopping all processes for app: {appName}");

        var killed = _processManager.StopAllForApp(appName);
        _connectionManager.DisconnectApp(appName);

        return JsonSerializer.Serialize(new
        {
            status = "stopped",
            app_name = appName,
            processes_killed = killed,
            message = $"Stopped {killed} process(es) for '{appName}' and disconnected."
        });
    }

    private string HandleListProcesses()
    {
        var processes = _processManager.GetRunningProcesses();
        var connectedApps = _connectionManager.GetConnectedApps()
            .ToDictionary(c => c.Name, c => c.Tools, StringComparer.OrdinalIgnoreCase);

        var grouped = processes
            .GroupBy(p => p.AppName, StringComparer.OrdinalIgnoreCase)
            .Select(g => new
            {
                app_name = g.Key,
                connected = connectedApps.ContainsKey(g.Key),
                tool_count = connectedApps.TryGetValue(g.Key, out var tools) ? tools.Count : 0,
                instances = g.Select(p => new
                {
                    pid = p.Pid,
                    started_at = p.StartedAt,
                    is_running = p.IsRunning
                }).ToList()
            })
            .ToList();

        return JsonSerializer.Serialize(new { groups = grouped, total_processes = processes.Count });
    }

    private async Task SendResponseAsync(StreamWriter writer, JsonElement id, object result)
    {
        var response = new
        {
            jsonrpc = "2.0",
            id = id.ValueKind == JsonValueKind.Number ? id.GetInt64() : (object)id.GetString()!,
            result
        };

        var json = JsonSerializer.Serialize(response);
        await writer.WriteLineAsync(json);
    }
}
