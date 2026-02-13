using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using Avalonia.Mcp.Core.Models;

namespace Avalonia.McpProxy.Services;

/// <summary>
/// Bridges MCP stdio protocol to connected diagnostic apps.
/// Receives MCP requests from agents, routes to apps, returns responses.
/// </summary>
public class McpProxyBridge
{
    private readonly Action<string> _log;
    private readonly AppConnectionManager _connectionManager;
    private readonly ProcessManager _processManager;

    public McpProxyBridge(Action<string> log, AppConnectionManager connectionManager)
    {
        _log = log;
        _connectionManager = connectionManager;
        _processManager = new ProcessManager(log, connectionManager);
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _log("MCP bridge ready on stdio");
        
        using var stdin = Console.OpenStandardInput();
        using var stdout = Console.OpenStandardOutput();
        using var reader = new StreamReader(stdin, Encoding.UTF8);
        using var writer = new StreamWriter(stdout, Encoding.UTF8) { AutoFlush = true };

        var buffer = new StringBuilder();

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
                    serverInfo = new { name = "avalonia-mcp-proxy", version = "2.0.0" }
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

        // Proxy management tools
        tools.Add(new
        {
            name = "launch_app",
            description = "Launch an Avalonia app with diagnostic support. The app will start a diagnostic listener that the proxy can connect to.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    path = new { type = "string", description = "Path to the application executable" },
                    args = new { type = "string", description = "Optional command line arguments" },
                    working_directory = new { type = "string", description = "Optional working directory" }
                },
                required = new[] { "path" }
            }
        });

        tools.Add(new
        {
            name = "list_apps",
            description = "List all connected diagnostic apps and their available tools.",
            inputSchema = new { type = "object", properties = new { } }
        });

        tools.Add(new
        {
            name = "stop_app",
            description = "Stop a connected app by sending it a shutdown signal.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    port = new { type = "integer", description = "The diagnostic port of the app to stop" }
                },
                required = new[] { "port" }
            }
        });

        // Add tools from all connected apps
        foreach (var (appName, port, appTools) in _connectionManager.GetConnectedApps())
        {
            foreach (var tool in appTools)
            {
                tools.Add(new
                {
                    name = $"{appName}:{tool.Name}",
                    description = $"[{appName}] {tool.Description}",
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

        // Handle proxy management tools
        if (toolName == "launch_app")
        {
            resultText = await _processManager.LaunchAppAsync(arguments, ct);
        }
        else if (toolName == "list_apps")
        {
            resultText = GetAppsList();
        }
        else if (toolName == "stop_app")
        {
            var port = arguments.GetProperty("port").GetInt32();
            resultText = await StopAppAsync(port, ct);
        }
        else if (toolName.Contains(':'))
        {
            // App-specific tool: "AppName:tool_name"
            var parts = toolName.Split(':', 2);
            var appName = parts[0];
            var actualTool = parts[1];

            // Find the app by name
            var targetPort = -1;
            foreach (var (name, port, _) in _connectionManager.GetConnectedApps())
            {
                if (name.Equals(appName, StringComparison.OrdinalIgnoreCase))
                {
                    targetPort = port;
                    break;
                }
            }

            if (targetPort < 0)
            {
                resultText = JsonSerializer.Serialize(new { error = $"App '{appName}' not connected" });
            }
            else
            {
                var (success, data, error) = await _connectionManager.InvokeToolAsync(targetPort, actualTool, arguments, ct);
                resultText = success ? data! : JsonSerializer.Serialize(new { error });
            }
        }
        else
        {
            resultText = JsonSerializer.Serialize(new { error = $"Unknown tool: {toolName}" });
        }

        await SendResponseAsync(writer, id, new
        {
            content = new[] { new { type = "text", text = resultText } }
        });
    }

    private string GetAppsList()
    {
        var apps = new List<object>();
        
        foreach (var (name, port, tools) in _connectionManager.GetConnectedApps())
        {
            apps.Add(new
            {
                name,
                port,
                tool_count = tools.Count,
                tools = tools.Select(t => t.Name).ToList()
            });
        }

        return JsonSerializer.Serialize(new { apps, count = apps.Count });
    }

    private async Task<string> StopAppAsync(int port, CancellationToken ct)
    {
        var (success, _, error) = await _connectionManager.InvokeToolAsync(port, "__shutdown__", default, ct);
        
        // Also send shutdown message directly
        // The app's DiagnosticListener handles "shutdown" message type
        
        return success 
            ? JsonSerializer.Serialize(new { status = "shutdown_sent", port })
            : JsonSerializer.Serialize(new { error = error ?? "Failed to stop app" });
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
