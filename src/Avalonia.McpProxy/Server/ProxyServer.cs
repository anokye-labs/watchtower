using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Avalonia.Mcp.Core.Models;
using Avalonia.McpProxy.Models;
using Microsoft.Extensions.Logging;

namespace Avalonia.McpProxy.Server;

/// <summary>
/// Main MCP proxy server that federates multiple Avalonia application handlers.
/// Communicates with agents via stdio (MCP protocol) and with apps via TCP.
/// </summary>
public class ProxyServer : IDisposable
{
    private readonly ProxyConfiguration _config;
    private readonly AppRegistry _registry;
    private readonly ILogger<ProxyServer> _logger;
    private TcpListener? _tcpListener;
    private CancellationTokenSource? _cts;
    private Task? _listenerTask;
    private Task? _stdinTask;
    private readonly Dictionary<string, TcpClient> _appConnections = new();
    private bool _disposed;

    public ProxyServer(ProxyConfiguration config, AppRegistry registry, ILogger<ProxyServer> logger)
    {
        _config = config;
        _registry = registry;
        _logger = logger;
    }

    /// <summary>
    /// Starts the proxy server in stdio mode (for MCP agent communication).
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting MCP Proxy Server");
        _logger.LogInformation("Bind address: {BindAddress}", _config.BindAddress);
        _logger.LogInformation("Max connections: {MaxConnections}", _config.MaxConnections);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Start TCP listener for app connections
        await StartTcpListenerAsync(_cts.Token);

        // Start stdio handler for agent communication
        _stdinTask = Task.Run(() => HandleStdioAsync(_cts.Token), _cts.Token);

        _logger.LogInformation("MCP Proxy Server started successfully");
    }

    private async Task StartTcpListenerAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Parse bind address
            var parts = _config.BindAddress.Split(':');
            var host = parts.Length > 0 ? parts[0] : "localhost";
            var port = parts.Length > 1 ? int.Parse(parts[1]) : 5100;

            var ipAddress = host == "localhost" || host == "127.0.0.1"
                ? IPAddress.Loopback
                : IPAddress.Parse(host);

            _tcpListener = new TcpListener(ipAddress, port);
            _tcpListener.Start();

            _logger.LogInformation("TCP listener started on {Host}:{Port}", host, port);

            // Start accepting connections
            _listenerTask = Task.Run(() => AcceptConnectionsAsync(cancellationToken), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start TCP listener");
            throw;
        }
    }

    private async Task AcceptConnectionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _tcpListener != null)
            {
                var client = await _tcpListener.AcceptTcpClientAsync(cancellationToken);
                var connectionId = Guid.NewGuid().ToString();

                _logger.LogInformation("Accepted connection: {ConnectionId}", connectionId);

                // Handle this connection in a separate task
                _ = Task.Run(() => HandleAppConnectionAsync(client, connectionId, cancellationToken), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("TCP listener stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in TCP listener");
        }
    }

    private async Task HandleAppConnectionAsync(TcpClient client, string connectionId, CancellationToken cancellationToken)
    {
        try
        {
            _appConnections[connectionId] = client;
            var stream = client.GetStream();
            var buffer = new byte[8192];
            var messageBuffer = new StringBuilder();

            while (!cancellationToken.IsCancellationRequested && client.Connected)
            {
                var bytesRead = await stream.ReadAsync(buffer, cancellationToken);

                if (bytesRead == 0)
                {
                    _logger.LogInformation("Connection closed: {ConnectionId}", connectionId);
                    break;
                }

                var text = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                messageBuffer.Append(text);

                // Process line-delimited messages
                var messages = messageBuffer.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);

                if (messageBuffer.ToString().EndsWith('\n'))
                {
                    foreach (var msg in messages)
                    {
                        await HandleAppMessageAsync(msg.Trim(), connectionId, stream, cancellationToken);
                    }
                    messageBuffer.Clear();
                }
                else
                {
                    for (int i = 0; i < messages.Length - 1; i++)
                    {
                        await HandleAppMessageAsync(messages[i].Trim(), connectionId, stream, cancellationToken);
                    }
                    messageBuffer.Clear();
                    messageBuffer.Append(messages[^1]);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling app connection: {ConnectionId}", connectionId);
        }
        finally
        {
            _registry.MarkDisconnected(connectionId);
            _appConnections.Remove(connectionId);
            client.Close();
        }
    }

    private async Task HandleAppMessageAsync(string message, string connectionId, NetworkStream stream, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonDocument.Parse(message);
            var root = json.RootElement;

            if (root.TryGetProperty("type", out var typeElement))
            {
                var type = typeElement.GetString();

                if (type == "register")
                {
                    var appName = root.GetProperty("appName").GetString()!;
                    var tools = root.TryGetProperty("tools", out var toolsElement)
                        ? JsonSerializer.Deserialize<List<McpToolDefinition>>(toolsElement.GetRawText())
                        : null;

                    _registry.RegisterApp(connectionId, appName, tools);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling app message from {ConnectionId}", connectionId);
        }
    }

    private async Task HandleStdioAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting stdio handler for agent communication");

            using var reader = Console.In;
            
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                
                if (line == null)
                {
                    _logger.LogInformation("Stdin closed, shutting down");
                    break;
                }

                await HandleAgentMessageAsync(line, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Stdio handler stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in stdio handler");
        }
    }

    private async Task HandleAgentMessageAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Received agent message: {Message}", message);

            var json = JsonDocument.Parse(message);
            var root = json.RootElement;

            // MCP protocol: handle list tools, execute tool, etc.
            if (root.TryGetProperty("method", out var methodElement))
            {
                var method = methodElement.GetString();

                if (method == "tools/list")
                {
                    await HandleListToolsAsync(root, cancellationToken);
                }
                else if (method == "tools/call")
                {
                    await HandleCallToolAsync(root, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling agent message");
            await SendErrorToAgentAsync("Error processing request", cancellationToken);
        }
    }

    private async Task HandleListToolsAsync(JsonElement request, CancellationToken cancellationToken)
    {
        try
        {
            var tools = _registry.GetAllTools();
            
            var response = new
            {
                jsonrpc = "2.0",
                id = request.TryGetProperty("id", out var idEl) ? idEl.GetInt32() : 0,
                result = new
                {
                    tools = tools.Select(t => new
                    {
                        name = t.Name,
                        description = t.Description,
                        inputSchema = t.InputSchema
                    })
                }
            };

            await SendToAgentAsync(JsonSerializer.Serialize(response), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling list tools");
        }
    }

    private async Task HandleCallToolAsync(JsonElement request, CancellationToken cancellationToken)
    {
        try
        {
            if (!request.TryGetProperty("params", out var paramsEl) ||
                !paramsEl.TryGetProperty("name", out var nameEl))
            {
                await SendErrorToAgentAsync("Missing tool name", cancellationToken);
                return;
            }

            var toolName = nameEl.GetString()!;
            var app = _registry.FindAppByTool(toolName);

            if (app == null)
            {
                await SendErrorToAgentAsync($"Tool not found: {toolName}", cancellationToken);
                return;
            }

            // Forward the tool call to the app
            // For now, send a success response (actual routing will be implemented later)
            var response = new
            {
                jsonrpc = "2.0",
                id = request.TryGetProperty("id", out var idEl) ? idEl.GetInt32() : 0,
                result = new
                {
                    content = new[]
                    {
                        new { type = "text", text = $"Tool {toolName} executed (routing not fully implemented)" }
                    }
                }
            };

            await SendToAgentAsync(JsonSerializer.Serialize(response), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling call tool");
        }
    }

    private async Task SendToAgentAsync(string message, CancellationToken cancellationToken)
    {
        await Console.Out.WriteLineAsync(message.AsMemory(), cancellationToken);
        await Console.Out.FlushAsync(cancellationToken);
    }

    private async Task SendErrorToAgentAsync(string error, CancellationToken cancellationToken)
    {
        var response = new
        {
            jsonrpc = "2.0",
            error = new { code = -32000, message = error }
        };

        await SendToAgentAsync(JsonSerializer.Serialize(response), cancellationToken);
    }

    public async Task StopAsync()
    {
        _logger.LogInformation("Stopping MCP Proxy Server");

        _cts?.Cancel();

        if (_listenerTask != null)
        {
            await _listenerTask;
        }

        if (_stdinTask != null)
        {
            await _stdinTask;
        }

        _tcpListener?.Stop();

        foreach (var connection in _appConnections.Values)
        {
            connection.Close();
        }

        _logger.LogInformation("MCP Proxy Server stopped");
    }

    public void Dispose()
    {
        if (_disposed) return;

        StopAsync().GetAwaiter().GetResult();
        _cts?.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
