using Avalonia.Mcp.Core.Models;
using Avalonia.McpProxy.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

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
    private readonly ConcurrentDictionary<string, TcpClient> _appConnections = new();
    private readonly ConcurrentDictionary<string, NetworkStream> _appStreams = new();
    private long _nextCorrelationId;
    private readonly ConcurrentDictionary<long, TaskCompletionSource<McpToolResult>> _pendingRequests = new();
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
            _appStreams[connectionId] = stream;
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
            _appConnections.TryRemove(connectionId, out _);
            _appStreams.TryRemove(connectionId, out _);
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
                else if (type == "toolResponse" && root.TryGetProperty("correlationId", out var corrIdElement))
                {
                    // Handle tool execution response
                    var correlationId = corrIdElement.GetInt64();
                        
                    // Parse the result
                    McpToolResult result;
                    if (root.TryGetProperty("result", out var resultElement))
                    {
                        if (resultElement.ValueKind == JsonValueKind.Null)
                        {
                            result = McpToolResult.Fail("Null result");
                        }
                        else
                        {
                            var deserializedResult = JsonSerializer.Deserialize<McpToolResult>(resultElement.GetRawText());
                            result = deserializedResult ?? McpToolResult.Fail("Null result");
                        }
                    }
                    else
                    {
                        result = McpToolResult.Fail("Missing result property");
                    }

                    // Complete the pending request
                    if (_pendingRequests.TryRemove(correlationId, out var tcs))
                    {
                        tcs.TrySetResult(result);
                        _logger.LogDebug("Completed pending request {CorrelationId}", correlationId);
                    }
                    else
                    {
                        _logger.LogWarning("Received response for unknown correlation ID: {CorrelationId}", correlationId);
                    }
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
        JsonElement root = default;
        try
        {
            _logger.LogDebug("Received agent message: {Message}", message);

            var json = JsonDocument.Parse(message);
            root = json.RootElement;

            // MCP protocol: handle initialize, list tools, execute tool, etc.
            if (root.TryGetProperty("method", out var methodElement))
            {
                var method = methodElement.GetString();

                if (method == "initialize")
                {
                    await HandleInitializeAsync(root, cancellationToken);
                }
                else if (method == "notifications/initialized")
                {
                    // Client confirmed initialization - nothing to do
                    _logger.LogDebug("Client initialized notification received");
                }
                else if (method == "tools/list")
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
            var requestId = root.TryGetProperty("id", out var idEl) ? (int?)idEl.GetInt32() : null;
            await SendErrorToAgentAsync("Error processing request", requestId, cancellationToken);
        }
    }

    private async Task HandleInitializeAsync(JsonElement request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Handling MCP initialize request");

            var response = new
            {
                jsonrpc = "2.0",
                id = request.TryGetProperty("id", out var idEl) ? idEl.GetInt32() : 0,
                result = new
                {
                    protocolVersion = "2024-11-05",
                    capabilities = new
                    {
                        tools = new { listChanged = false }
                    },
                    serverInfo = new
                    {
                        name = "avalonia-mcp-proxy",
                        version = "1.0.0"
                    }
                }
            };

            await SendToAgentAsync(JsonSerializer.Serialize(response), cancellationToken);
            _logger.LogInformation("MCP initialize response sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling initialize");
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
        var requestId = request.TryGetProperty("id", out var idEl) ? (int?)idEl.GetInt32() : null;

        try
        {
            if (!request.TryGetProperty("params", out var paramsEl) ||
                !paramsEl.TryGetProperty("name", out var nameEl))
            {
                await SendErrorToAgentAsync("Missing tool name", requestId, cancellationToken);
                return;
            }

            var toolName = nameEl.GetString()!;
            var app = _registry.FindAppByTool(toolName);

            if (app == null)
            {
                await SendErrorToAgentAsync($"Tool not found: {toolName}", requestId, cancellationToken);
                return;
            }

            // Check if app is still connected
            if (!_appStreams.TryGetValue(app.ConnectionId, out var stream))
            {
                await SendErrorToAgentAsync($"App '{app.Name}' is not connected", requestId, cancellationToken);
                return;
            }

            // Parse tool arguments - store as object to preserve JSON structure
            object? arguments = null;
            if (paramsEl.TryGetProperty("arguments", out var argsEl))
            {
                // Keep as JsonElement to preserve exact JSON structure without type conversion
                arguments = JsonSerializer.Deserialize<JsonElement>(argsEl.GetRawText());
            }

            // Generate correlation ID
            var correlationId = Interlocked.Increment(ref _nextCorrelationId);

            // Create TaskCompletionSource for this request
            var tcs = new TaskCompletionSource<McpToolResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingRequests[correlationId] = tcs;

            try
            {
                // Build tool invocation message for the app
                var invocationMessage = new
                {
                    type = "toolInvocation",
                    correlationId = correlationId,
                    tool = toolName,
                    parameters = arguments
                };

                var messageJson = JsonSerializer.Serialize(invocationMessage) + "\n";
                var messageBytes = Encoding.UTF8.GetBytes(messageJson);

                // Send to app
                await stream.WriteAsync(messageBytes, cancellationToken);
                await stream.FlushAsync(cancellationToken);

                _logger.LogInformation("Forwarded tool '{ToolName}' to app '{AppName}' with correlation ID {CorrelationId}",
                    toolName, app.Name, correlationId);

                // Wait for response with timeout
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), timeoutCts.Token);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                McpToolResult result;
                if (completedTask == timeoutTask)
                {
                    // Timeout occurred (but verify that the request is still pending to avoid race conditions)
                    if (_pendingRequests.TryRemove(correlationId, out _))
                    {
                        result = McpToolResult.Fail("Tool execution timed out after 30 seconds");
                        _logger.LogWarning("Tool '{ToolName}' timed out (correlation ID: {CorrelationId})", toolName, correlationId);
                    }
                    else
                    {
                        // A response won the race against the timeout; use the actual tool result instead of reporting a timeout.
                        result = await tcs.Task;
                    }
                }
                else
                {
                    // Got response, cancel the timeout task to free resources
                    timeoutCts.Cancel();
                    result = await tcs.Task;
                }

                // Convert result to MCP protocol response
                var response = new
                {
                    jsonrpc = "2.0",
                    id = requestId ?? 0,
                    result = new
                    {
                        content = new[]
                        {
                            new
                            {
                                type = "text",
                                text = result.Success
                                    ? JsonSerializer.Serialize(result.Data)
                                    : $"Error: {result.Error}"
                            }
                        }
                    }
                };

                await SendToAgentAsync(JsonSerializer.Serialize(response), cancellationToken);
            }
            catch (Exception ex)
            {
                // Clean up pending request on exception
                _pendingRequests.TryRemove(correlationId, out _);
                _logger.LogError(ex, "Error forwarding tool call to app");
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling call tool");
            await SendErrorToAgentAsync($"Error executing tool: {ex.Message}", requestId, cancellationToken);
        }
    }

    private async Task SendToAgentAsync(string message, CancellationToken cancellationToken)
    {
        await Console.Out.WriteLineAsync(message.AsMemory(), cancellationToken);
        await Console.Out.FlushAsync(cancellationToken);
    }

    private async Task SendErrorToAgentAsync(string error, int? requestId, CancellationToken cancellationToken)
    {
        var response = new
        {
            jsonrpc = "2.0",
            id = requestId,
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
        if (_disposed)
            return;

        StopAsync().GetAwaiter().GetResult();
        _cts?.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
