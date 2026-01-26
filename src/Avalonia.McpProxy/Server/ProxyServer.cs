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
    
    // Relay mode fields - used when another proxy is already running
    private bool _isRelayMode;
    private TcpClient? _relayClient;
    private NetworkStream? _relayStream;
    private Task? _relayReceiveTask;
    
    // Relay client tracking - connections that act as MCP relays
    private readonly ConcurrentDictionary<string, NetworkStream> _relayClients = new();
    
    // Relay ready signal - used to wait for relay connection before forwarding
    private readonly TaskCompletionSource<bool> _relayReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
    
    // Pending relay requests - correlate responses by request ID
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingRelayRequests = new();

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

        // Check if we need relay mode FIRST (before starting stdio)
        var parts = _config.BindAddress.Split(':');
        var host = parts.Length > 0 ? parts[0] : "localhost";
        var port = parts.Length > 1 ? int.Parse(parts[1]) : 5100;
        
        using var testSocket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, 
            System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
        try
        {
            testSocket.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port));
            testSocket.Close();
            // Port is free - we'll be the primary proxy
            _logger.LogInformation("Port {Port} is available - running as primary proxy", port);
        }
        catch (System.Net.Sockets.SocketException)
        {
            // Port in use - set up relay mode BEFORE starting stdio
            _logger.LogInformation("Port {Port} in use - setting up relay mode first", port);
            _isRelayMode = true;
            await SetupRelayConnectionAsync(host, port, _cts.Token);
        }

        // NOW start stdio handler (relay is ready if needed)
        _stdinTask = Task.Run(() => HandleStdioAsync(_cts.Token), _cts.Token);

        // Start TCP listener for app connections (skip if relay mode)
        if (!_isRelayMode)
        {
            await StartTcpListenerAsync(_cts.Token);
        }

        _logger.LogInformation("MCP Proxy Server started successfully");
    }
    
    private async Task SetupRelayConnectionAsync(string host, int port, CancellationToken cancellationToken)
    {
        _relayClient = new TcpClient();
        await _relayClient.ConnectAsync(host, port, cancellationToken);
        _relayStream = _relayClient.GetStream();
        _logger.LogInformation("Connected to existing proxy as relay client");
        
        // Register as a relay client
        var registration = JsonSerializer.Serialize(new { type = "mcp_relay" });
        var regBytes = Encoding.UTF8.GetBytes(registration + "\n");
        await _relayStream.WriteAsync(regBytes, cancellationToken);
        await _relayStream.FlushAsync(cancellationToken);
        _logger.LogInformation("Sent relay registration");
        
        // Start relay receive loop
        _relayReceiveTask = Task.Run(() => RelayReceiveLoopAsync(cancellationToken), cancellationToken);
        
        // Signal that relay is ready
        _relayReady.TrySetResult(true);
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
        catch (System.Net.Sockets.SocketException ex) when (ex.SocketErrorCode == System.Net.Sockets.SocketError.AddressAlreadyInUse)
        {
            // Port already in use - another proxy instance is running
            // Switch to relay mode - connect as client and forward requests
            _logger.LogInformation("Port {BindAddress} already in use - switching to relay mode", _config.BindAddress);
            _tcpListener = null;
            _isRelayMode = true;
            
            // Connect to the existing proxy as a relay client
            var parts = _config.BindAddress.Split(':');
            var host = parts.Length > 0 ? parts[0] : "localhost";
            var port = parts.Length > 1 ? int.Parse(parts[1]) : 5100;
            
            _relayClient = new TcpClient();
            await _relayClient.ConnectAsync(host, port, cancellationToken);
            _relayStream = _relayClient.GetStream();
            _logger.LogInformation("Connected to existing proxy as relay client");
            
            // Register as a relay client
            var registration = JsonSerializer.Serialize(new { type = "mcp_relay" });
            var regBytes = Encoding.UTF8.GetBytes(registration + "\n");
            await _relayStream.WriteAsync(regBytes, cancellationToken);
            await _relayStream.FlushAsync(cancellationToken);
            _logger.LogInformation("Sent relay registration");
            
            // Start relay receive loop
            _relayReceiveTask = Task.Run(() => RelayReceiveLoopAsync(cancellationToken), cancellationToken);
            
            // Signal that relay is ready
            _relayReady.TrySetResult(true);
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
                _logger.LogInformation("TCP raw data from {ConnectionId}: {Data}", connectionId, text.Length > 200 ? text.Substring(0, 200) : text);
                messageBuffer.Append(text);

                // Process line-delimited messages
                var messages = messageBuffer.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);

                if (messageBuffer.ToString().EndsWith('\n'))
                {
                    foreach (var msg in messages)
                    {
                        _logger.LogInformation("Processing complete message from {ConnectionId}: {Msg}", connectionId, msg.Length > 100 ? msg.Substring(0, 100) : msg);
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
            _logger.LogInformation("HandleAppMessageAsync: received message from {ConnectionId}: {Message}", 
                connectionId, message.Length > 200 ? message.Substring(0, 200) + "..." : message);
            
            var json = JsonDocument.Parse(message);
            var root = json.RootElement;

            if (root.TryGetProperty("type", out var typeElement))
            {
                var type = typeElement.GetString();
                _logger.LogInformation("HandleAppMessageAsync: message type = {Type}", type);

                if (type == "register")
                {
                    var appName = root.GetProperty("appName").GetString()!;
                    var tools = root.TryGetProperty("tools", out var toolsElement)
                        ? JsonSerializer.Deserialize<List<McpToolDefinition>>(toolsElement.GetRawText())
                        : null;

                    _registry.RegisterApp(connectionId, appName, tools);
                }
                else if (type == "mcp_relay")
                {
                    // This is a relay client - another proxy instance forwarding MCP traffic
                    _relayClients[connectionId] = stream;
                    _logger.LogInformation("Registered relay client: {ConnectionId}", connectionId);
                    
                    // Send acknowledgment
                    var ack = JsonSerializer.Serialize(new { type = "mcp_relay_ack" });
                    var ackBytes = Encoding.UTF8.GetBytes(ack + "\n");
                    await stream.WriteAsync(ackBytes, cancellationToken);
                    await stream.FlushAsync(cancellationToken);
                }
                else if (type == "mcp_request" && _relayClients.ContainsKey(connectionId))
                {
                    // MCP request from a relay client - handle it and send response back
                    if (root.TryGetProperty("payload", out var payloadElement))
                    {
                        var payload = payloadElement.GetRawText();
                        var response = await HandleMcpRequestAsync(payload, cancellationToken);
                        
                        // Send response back to relay
                        var responseMsg = JsonSerializer.Serialize(new { type = "mcp_response", payload = response });
                        var responseBytes = Encoding.UTF8.GetBytes(responseMsg + "\n");
                        await stream.WriteAsync(responseBytes, cancellationToken);
                        await stream.FlushAsync(cancellationToken);
                    }
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
                _logger.LogInformation("Waiting for stdin input...");
                var line = await reader.ReadLineAsync(cancellationToken);
                _logger.LogInformation("Received from stdin: {Length} chars", line?.Length ?? 0);

                if (line == null)
                {
                    // Don't shut down when stdin closes - keep running for TCP connections
                    // This allows WatchTower apps to connect even after the MCP client disconnects
                    _logger.LogInformation("Stdin closed, but keeping server running for app connections");
                    
                    // Wait indefinitely for app connections or cancellation
                    try
                    {
                        await Task.Delay(Timeout.Infinite, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // Normal shutdown
                    }
                    break;
                }

                // Log the raw message and method
                _logger.LogInformation("Raw message: {Line}", line.Length > 100 ? line.Substring(0, 100) + "..." : line);
                try
                {
                    var peek = JsonDocument.Parse(line);
                    var method = peek.RootElement.TryGetProperty("method", out var m) ? m.GetString() : "no-method";
                    var id = peek.RootElement.TryGetProperty("id", out var i) ? i.ToString() : "no-id";
                    _logger.LogInformation("Processing MCP method={Method} id={Id}", method, id);
                }
                catch (Exception ex) { _logger.LogInformation("Parse error: {Error}", ex.Message); }
                
                await HandleAgentMessageAsync(line, cancellationToken);
                _logger.LogInformation("HandleAgentMessageAsync completed");
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

    private async Task RelayReceiveLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting relay receive loop");
            var buffer = new byte[65536];
            var messageBuffer = new StringBuilder();
            
            while (!cancellationToken.IsCancellationRequested && _relayStream != null)
            {
                var bytesRead = await _relayStream.ReadAsync(buffer, cancellationToken);
                if (bytesRead == 0)
                {
                    _logger.LogInformation("Relay connection closed");
                    break;
                }
                
                var chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                messageBuffer.Append(chunk);
                
                // Process complete lines
                var content = messageBuffer.ToString();
                int newlineIndex;
                while ((newlineIndex = content.IndexOf('\n')) >= 0)
                {
                    var line = content.Substring(0, newlineIndex);
                    content = content.Substring(newlineIndex + 1);
                    
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        _logger.LogDebug("Relay received: {Line}", line);
                        
                        // Unwrap the mcp_response
                        try
                        {
                            var json = JsonDocument.Parse(line);
                            if (json.RootElement.TryGetProperty("type", out var typeEl))
                            {
                                var type = typeEl.GetString();
                                if (type == "mcp_response" && json.RootElement.TryGetProperty("payload", out var payloadEl))
                                {
                                    // Payload might be a string or an object depending on how it was serialized
                                    string payload;
                                    if (payloadEl.ValueKind == JsonValueKind.String)
                                    {
                                        payload = payloadEl.GetString()!;
                                    }
                                    else
                                    {
                                        payload = payloadEl.GetRawText();
                                    }
                                    
                                    // Extract response ID to complete the pending request
                                    var payloadDoc = JsonDocument.Parse(payload);
                                    var responseId = payloadDoc.RootElement.TryGetProperty("id", out var idEl) ? idEl.ToString() : null;
                                    
                                    _logger.LogInformation("Relay received response for id={Id}, payload length={Length}", responseId, payload.Length);
                                    _logger.LogDebug("Relay response payload: {Payload}", payload.Length > 500 ? payload.Substring(0, 500) + "..." : payload);
                                    
                                    if (responseId != null && _pendingRelayRequests.TryGetValue(responseId, out var tcs))
                                    {
                                        tcs.TrySetResult(payload);
                                    }
                                    else
                                    {
                                        // No pending request, send directly
                                        await SendToAgentAsync(payload, cancellationToken);
                                    }
                                }
                                else if (type == "mcp_relay_ack")
                                {
                                    _logger.LogInformation("Relay registration acknowledged");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error parsing relay message");
                            // Not a wrapped message, forward as-is
                            await SendToAgentAsync(line, cancellationToken);
                        }
                    }
                }
                messageBuffer.Clear();
                messageBuffer.Append(content);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Relay receive loop cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in relay receive loop");
        }
    }

    private async Task HandleAgentMessageAsync(string message, CancellationToken cancellationToken)
    {
        JsonElement root = default;
        try
        {
            _logger.LogDebug("Received agent message: {Message}", message);

            // In relay mode, forward non-initialize requests to existing proxy
            // Initialize is handled locally for fast response
            if (_isRelayMode && _relayStream != null)
            {
                var msgJson = JsonDocument.Parse(message);
                var method = msgJson.RootElement.TryGetProperty("method", out var m) ? m.GetString() : null;
                var requestId = msgJson.RootElement.TryGetProperty("id", out var idEl) ? idEl.ToString() : null;
                
                // Handle initialize locally for fast response
                if (method == "initialize")
                {
                    _logger.LogInformation("Relay mode: handling initialize locally");
                    await HandleInitializeAsync(msgJson.RootElement, cancellationToken);
                    return;
                }
                
                // Notifications don't need responses
                if (requestId == null)
                {
                    _logger.LogInformation("Relay mode: ignoring notification {Method}", method);
                    return;
                }
                
                // Forward request and wait for response
                _logger.LogInformation("Relay mode: forwarding {Method} (id={Id}) to existing proxy", method, requestId);
                var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                _pendingRelayRequests[requestId] = tcs;
                
                try
                {
                    var wrapped = JsonSerializer.Serialize(new { type = "mcp_request", payload = msgJson.RootElement });
                    var bytes = Encoding.UTF8.GetBytes(wrapped + "\n");
                    await _relayStream.WriteAsync(bytes, cancellationToken);
                    await _relayStream.FlushAsync(cancellationToken);
                    
                    // Wait for response with timeout
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));
                    var response = await tcs.Task.WaitAsync(timeoutCts.Token);
                    
                    _logger.LogInformation("Relay mode: got response for {Method}, sending to agent", method);
                    await SendToAgentAsync(response, cancellationToken);
                }
                finally
                {
                    _pendingRelayRequests.TryRemove(requestId, out _);
                }
                return;
            }

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

    private async Task<string> HandleMcpRequestAsync(string payload, CancellationToken cancellationToken)
    {
        // Parse and handle MCP request, returning the response
        string? method = null;
        int requestId = 0;
        
        try
        {
            var json = JsonDocument.Parse(payload);
            var root = json.RootElement;
            
            if (root.TryGetProperty("method", out var methodElement))
            {
                method = methodElement.GetString();
                requestId = root.TryGetProperty("id", out var idEl) ? idEl.GetInt32() : 0;
                
                if (method == "initialize")
                {
                    return JsonSerializer.Serialize(new
                    {
                        jsonrpc = "2.0",
                        id = requestId,
                        result = new
                        {
                            protocolVersion = "2024-11-05",
                            capabilities = new { tools = new { listChanged = false } },
                            serverInfo = new { name = "avalonia-mcp-proxy", version = "1.0.0" }
                        }
                    });
                }
                else if (method == "tools/list")
                {
                    var tools = _registry.GetAllTools();
                    var apps = _registry.GetAllApps();
                    _logger.LogInformation("tools/list: {AppCount} apps registered, {ToolCount} tools total", apps.Count, tools.Count);
                    foreach (var app in apps)
                    {
                        _logger.LogInformation("  App '{AppName}': {ToolCount} tools, connected={Connected}", app.Name, app.Tools.Count, app.IsConnected);
                    }
                    return JsonSerializer.Serialize(new
                    {
                        jsonrpc = "2.0",
                        id = requestId,
                        result = new
                        {
                            tools = tools.Select(t => new
                            {
                                name = t.Name,
                                description = t.Description,
                                inputSchema = t.InputSchema
                            }).ToList()
                        }
                    });
                }
                else if (method == "tools/call")
                {
                    var toolName = root.GetProperty("params").GetProperty("name").GetString()!;
                    var app = _registry.FindAppByTool(toolName);
                    
                    if (app == null || !_appStreams.TryGetValue(app.ConnectionId, out var stream))
                    {
                        return JsonSerializer.Serialize(new
                        {
                            jsonrpc = "2.0",
                            id = requestId,
                            error = new { code = -32602, message = $"Tool not found or app not connected: {toolName}" }
                        });
                    }
                    
                    // Parse arguments
                    object? arguments = null;
                    if (root.GetProperty("params").TryGetProperty("arguments", out var argsEl))
                    {
                        arguments = JsonSerializer.Deserialize<JsonElement>(argsEl.GetRawText());
                    }
                    
                    // Execute tool via app connection
                    var correlationId = Interlocked.Increment(ref _nextCorrelationId);
                    var tcs = new TaskCompletionSource<McpToolResult>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _pendingRequests[correlationId] = tcs;
                    
                    var invocationMessage = JsonSerializer.Serialize(new
                    {
                        type = "toolInvocation",
                        correlationId = correlationId,
                        tool = toolName,
                        parameters = arguments
                    }) + "\n";
                    
                    await stream.WriteAsync(Encoding.UTF8.GetBytes(invocationMessage), cancellationToken);
                    await stream.FlushAsync(cancellationToken);
                    
                    // Wait for result with timeout
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), timeoutCts.Token);
                    var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
                    
                    McpToolResult result;
                    if (completedTask == timeoutTask)
                    {
                        _pendingRequests.TryRemove(correlationId, out _);
                        result = McpToolResult.Fail("Tool execution timed out");
                    }
                    else
                    {
                        timeoutCts.Cancel();
                        result = await tcs.Task;
                    }
                    
                    var textContent = result.Success 
                        ? JsonSerializer.Serialize(result.Data) 
                        : result.Error ?? "Unknown error";
                    
                    return JsonSerializer.Serialize(new
                    {
                        jsonrpc = "2.0",
                        id = requestId,
                        result = new
                        {
                            content = new[] { new { type = "text", text = textContent } },
                            isError = !result.Success
                        }
                    });
                }
            }
            
            return JsonSerializer.Serialize(new { jsonrpc = "2.0", id = requestId, error = new { code = -32601, message = $"Unknown method: {method}" } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MCP request from relay");
            return JsonSerializer.Serialize(new { jsonrpc = "2.0", id = requestId, error = new { code = -32603, message = ex.Message } });
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
        _logger.LogInformation("SendToAgentAsync: writing {Length} chars to stdout", message.Length);
        await Console.Out.WriteLineAsync(message.AsMemory(), cancellationToken);
        await Console.Out.FlushAsync(cancellationToken);
        _logger.LogInformation("SendToAgentAsync: write complete");
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
