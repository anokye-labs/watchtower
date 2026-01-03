using System.Collections.Concurrent;
using System.Text.Json;
using Avalonia.Mcp.Core.Models;
using Avalonia.Mcp.Core.Transport;
using Microsoft.Extensions.Logging;

namespace Avalonia.Mcp.Core.Handlers;

/// <summary>
/// Base implementation of the embedded MCP handler.
/// </summary>
public class McpHandler : IMcpHandler
{
    private readonly McpHandlerConfiguration _configuration;
    private readonly ILogger<McpHandler>? _logger;
    private readonly ConcurrentDictionary<string, McpToolDefinition> _tools = new();
    private readonly ConcurrentDictionary<string, Func<Dictionary<string, object>?, Task<McpToolResult>>> _toolHandlers = new();
    private ITransportClient? _transportClient;
    private bool _disposed;

    public string ApplicationName => _configuration.ApplicationName;
    public bool IsConnected => _transportClient?.IsConnected ?? false;

    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    public McpHandler(McpHandlerConfiguration configuration, ILogger<McpHandler>? logger = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger;
    }

    public void RegisterTool(McpToolDefinition tool, Func<Dictionary<string, object>?, Task<McpToolResult>> handler)
    {
        if (tool == null) throw new ArgumentNullException(nameof(tool));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        var namespacedName = $"{ApplicationName}:{tool.Name}";
        
        // Create a copy with namespaced name
        var namespacedTool = new McpToolDefinition
        {
            Name = namespacedName,
            Description = tool.Description,
            InputSchema = tool.InputSchema
        };
        
        _tools[namespacedName] = namespacedTool;
        _toolHandlers[namespacedName] = handler;

        _logger?.LogInformation("Registered tool: {ToolName}", namespacedName);
    }

    public IReadOnlyList<McpToolDefinition> GetTools()
    {
        return _tools.Values.ToList();
    }

    public async Task<McpToolResult> ExecuteToolAsync(McpToolInvocation invocation, CancellationToken cancellationToken = default)
    {
        if (invocation == null)
            return McpToolResult.Fail("Invocation is null");

        if (!_toolHandlers.TryGetValue(invocation.ToolName, out var handler))
        {
            _logger?.LogWarning("Tool not found: {ToolName}", invocation.ToolName);
            return McpToolResult.Fail($"Tool not found: {invocation.ToolName}");
        }

        try
        {
            _logger?.LogInformation("Executing tool: {ToolName}", invocation.ToolName);
            var result = await handler(invocation.Parameters);
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error executing tool: {ToolName}", invocation.ToolName);
            return McpToolResult.Fail($"Execution error: {ex.Message}");
        }
    }

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Connecting to proxy: {Endpoint}", _configuration.ProxyEndpoint);

            // Parse endpoint and create appropriate transport client
            _transportClient = TransportClientFactory.Create(_configuration.ProxyEndpoint, _logger);

            // Set up message handler
            _transportClient.MessageReceived += OnMessageReceived;
            _transportClient.ConnectionStateChanged += OnTransportConnectionStateChanged;

            // Connect
            var connected = await _transportClient.ConnectAsync(cancellationToken);

            if (connected)
            {
                // Send registration message
                await SendRegistrationMessageAsync(cancellationToken);
            }

            return connected;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to connect to proxy");
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_transportClient != null)
        {
            _logger?.LogInformation("Disconnecting from proxy");
            await _transportClient.DisconnectAsync();
            _transportClient.MessageReceived -= OnMessageReceived;
            _transportClient.ConnectionStateChanged -= OnTransportConnectionStateChanged;
        }
    }

    private async Task SendRegistrationMessageAsync(CancellationToken cancellationToken)
    {
        var registrationMessage = new
        {
            type = "register",
            appName = ApplicationName,
            tools = GetTools()
        };

        var json = JsonSerializer.Serialize(registrationMessage);
        await _transportClient!.SendMessageAsync(json, cancellationToken);
        _logger?.LogInformation("Sent registration message with {ToolCount} tools", _tools.Count);
    }

    private void OnMessageReceived(object? sender, string message)
    {
        // Fire and forget pattern to avoid async void
        _ = Task.Run(async () =>
        {
            try
            {
                _logger?.LogDebug("Received message: {Message}", message);

                // Parse the message to extract type and correlation ID
                var jsonDoc = JsonDocument.Parse(message);
                var root = jsonDoc.RootElement;

                // Check if this is a tool invocation message
                if (root.TryGetProperty("type", out var typeEl) && typeEl.GetString() == "toolInvocation")
                {
                    // Extract correlation ID
                    var correlationId = root.TryGetProperty("correlationId", out var corrIdEl)
                        ? corrIdEl.GetInt64()
                        : 0;

                    // Extract tool name and parameters
                    var toolName = root.GetProperty("tool").GetString()!;
                    Dictionary<string, object>? parameters = null;
                    if (root.TryGetProperty("parameters", out var paramsEl) && paramsEl.ValueKind != JsonValueKind.Null)
                    {
                        // Deserialize to Dictionary<string, JsonElement> first for safer handling
                        var paramDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(paramsEl.GetRawText());
                        if (paramDict != null)
                        {
                            parameters = new Dictionary<string, object>();
                            foreach (var kvp in paramDict)
                            {
                                parameters[kvp.Key] = kvp.Value;
                            }
                        }
                    }

                    // Create tool invocation
                    var invocation = new McpToolInvocation
                    {
                        ToolName = toolName,
                        Parameters = parameters
                    };

                    // Execute the tool
                    var result = await ExecuteToolAsync(invocation);

                    // Send response with correlation ID
                    var response = new
                    {
                        type = "toolResponse",
                        correlationId = correlationId,
                        result = result
                    };

                    var responseJson = JsonSerializer.Serialize(response) + "\n";
                    await _transportClient!.SendMessageAsync(responseJson);

                    _logger?.LogInformation("Executed tool '{ToolName}' with correlation ID {CorrelationId}, success: {Success}",
                        toolName, correlationId, result.Success);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error handling received message");
            }
        });
    }

    private void OnTransportConnectionStateChanged(object? sender, bool isConnected)
    {
        _logger?.LogInformation("Connection state changed: {IsConnected}", isConnected);
        ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(isConnected));
    }

    public void Dispose()
    {
        if (_disposed) return;

        // Dispose synchronously - disconnect will be handled by finalizer or explicit DisposeAsync call
        _transportClient?.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        await DisconnectAsync();
        _transportClient?.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
