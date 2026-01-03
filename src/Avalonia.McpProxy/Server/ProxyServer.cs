using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<string, TcpClient> _appConnections = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> _pendingRequests = new();
    private long _nextCorrelationId;
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
            _appConnections.TryRemove(connectionId, out _);
            client.Close();
        }
    }

    private async Task HandleAppMessageAsync(string message, string connectionId, NetworkStream stream, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonDocument.Parse(message);
            var root = json.RootElement;

            // Check if this is a JSON-RPC response (has "jsonrpc": "2.0" and "id")
            if (root.TryGetProperty("jsonrpc", out var jsonrpcElement)
                && jsonrpcElement.ValueKind == JsonValueKind.String
                && jsonrpcElement.GetString() == "2.0"
                && root.TryGetProperty("id", out var idElement))
            {
                var correlationId = idElement.GetString();
                if (!string.IsNullOrEmpty(correlationId) && _pendingRequests.TryRemove(correlationId, out var tcs))
                {
                    _logger.LogDebug("Received response for correlation ID: {CorrelationId}", correlationId);
                    tcs.TrySetResult(root);
                }
                else
                {
                    _logger.LogDebug(
                        "Received response for unknown or expired correlation ID: {CorrelationId} from {ConnectionId}. This may occur after a request timeout or cancellation.",
                        correlationId,
                        connectionId);
                }
                return;
            }

            // Handle registration messages
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
        JsonElement root = default;
        try
        {
            _logger.LogDebug("Received agent message: {Message}", message);

            var json = JsonDocument.Parse(message);
            root = json.RootElement;

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
            var requestId = root.TryGetProperty("id", out var idEl) ? (int?)idEl.GetInt32() : null;
            await SendErrorToAgentAsync("Error processing request", requestId, cancellationToken);
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

            // Get the TCP connection for this app
            if (!_appConnections.TryGetValue(app.ConnectionId, out var tcpClient) || !tcpClient.Connected)
            {
                await SendErrorToAgentAsync($"App '{app.Name}' is not connected", requestId, cancellationToken);
                return;
            }

            // Generate correlation ID for tracking the request/response
            var correlationId = Interlocked.Increment(ref _nextCorrelationId).ToString();

            // Create TaskCompletionSource for awaiting the response
            var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!_pendingRequests.TryAdd(correlationId, tcs))
            {
                await SendErrorToAgentAsync($"Correlation ID conflict: {correlationId}", requestId, cancellationToken);
                return;
            }

            try
            {
                // Build JSON-RPC 2.0 request for the app
                var appRequest = new
                {
                    jsonrpc = "2.0",
                    id = correlationId,
                    method = "tools/call",
                    @params = new
                    {
                        name = toolName,
                        arguments = paramsEl.TryGetProperty("arguments", out var argsEl) ? argsEl : (object?)null
                    }
                };

                // Serialize and send the request with newline delimiter
                var appRequestJson = JsonSerializer.Serialize(appRequest);
                var messageBytes = Encoding.UTF8.GetBytes(appRequestJson + "\n");

                var stream = tcpClient.GetStream();

                // Register cancellation callback before sending to ensure cleanup
                using var registration = cancellationToken.Register(() =>
                {
                    if (_pendingRequests.TryRemove(correlationId, out var cancelledTcs))
                    {
                        cancelledTcs.TrySetCanceled(cancellationToken);
                    }
                });

                await stream.WriteAsync(messageBytes, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

                _logger.LogDebug("Forwarded tool call '{ToolName}' to app '{AppName}' with correlation ID: {CorrelationId}",
                    toolName, app.Name, correlationId);

                // Wait for the response from the app with timeout
                var timeout = TimeSpan.FromMinutes(2);
                using var timeoutCts = new CancellationTokenSource(timeout);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                Task<JsonElement> responseTask;
                try
                {
                    linkedCts.Token.Register(() =>
                    {
                        if (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                        {
                            if (_pendingRequests.TryRemove(correlationId, out var timedOutTcs))
                            {
                                timedOutTcs.TrySetException(new TimeoutException($"Tool call '{toolName}' did not complete within {timeout.TotalSeconds} seconds."));
                            }
                        }
                    });

                    responseTask = tcs.Task;
                    var appResponse = await responseTask.ConfigureAwait(false);

                    // Forward the app's response to the agent
                    // JSON-RPC 2.0: response must have either "result" OR "error", not both
                    var hasError = appResponse.TryGetProperty("error", out var errorEl);
                    var hasResult = appResponse.TryGetProperty("result", out var resultEl);

                    if (!hasError && !hasResult)
                    {
                        // Malformed JSON-RPC response from app: missing both "result" and "error"
                        _logger.LogWarning("Received malformed response from app '{AppName}' for correlation ID: {CorrelationId} (missing both 'result' and 'error')",
                            app.Name, correlationId);
                        await SendErrorToAgentAsync("Malformed response from application: missing both 'result' and 'error'.", requestId, CancellationToken.None);
                        return;
                    }

                    // Use ternary operator to create response based on whether error or result is present
                    var agentResponse = hasError
                        ? JsonSerializer.Serialize(new { jsonrpc = "2.0", id = requestId ?? 0, error = errorEl })
                        : JsonSerializer.Serialize(new { jsonrpc = "2.0", id = requestId ?? 0, result = resultEl });

                    await SendToAgentAsync(agentResponse, cancellationToken);

                    _logger.LogDebug("Forwarded response from app '{AppName}' to agent for correlation ID: {CorrelationId}",
                        app.Name, correlationId);
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning("Tool call '{ToolName}' to app '{AppName}' timed out after {Timeout} seconds", toolName, app.Name, timeout.TotalSeconds);
                    _pendingRequests.TryRemove(correlationId, out _);
                    await SendErrorToAgentAsync($"Tool execution timed out after {timeout.TotalSeconds} seconds", requestId, CancellationToken.None);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Tool call '{ToolName}' to app '{AppName}' was canceled", toolName, app.Name);
                _pendingRequests.TryRemove(correlationId, out _);
                // Use CancellationToken.None to avoid throwing another OperationCanceledException
                await SendErrorToAgentAsync("Tool execution was canceled", requestId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error forwarding tool call '{ToolName}' to app '{AppName}'", toolName, app.Name);
                _pendingRequests.TryRemove(correlationId, out _);
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
        if (_disposed) return;

        StopAsync().GetAwaiter().GetResult();
        _cts?.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
