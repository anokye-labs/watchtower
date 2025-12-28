using Avalonia.Mcp.Core.Models;

namespace Avalonia.Mcp.Core.Handlers;

/// <summary>
/// Interface for the embedded MCP handler in Avalonia applications.
/// Handles registration of tools and execution of tool invocations.
/// </summary>
public interface IMcpHandler : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets the application name.
    /// </summary>
    string ApplicationName { get; }

    /// <summary>
    /// Gets whether the handler is connected to the proxy.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Event raised when connection state changes.
    /// </summary>
    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    /// <summary>
    /// Registers a tool with the handler.
    /// </summary>
    /// <param name="tool">The tool definition.</param>
    /// <param name="handler">The handler function to execute the tool.</param>
    void RegisterTool(McpToolDefinition tool, Func<Dictionary<string, object>?, Task<McpToolResult>> handler);

    /// <summary>
    /// Gets all registered tools.
    /// </summary>
    IReadOnlyList<McpToolDefinition> GetTools();

    /// <summary>
    /// Executes a tool invocation.
    /// </summary>
    Task<McpToolResult> ExecuteToolAsync(McpToolInvocation invocation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Connects to the MCP proxy.
    /// </summary>
    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the MCP proxy.
    /// </summary>
    Task DisconnectAsync();
}

/// <summary>
/// Event arguments for connection state changes.
/// </summary>
public class ConnectionStateChangedEventArgs : EventArgs
{
    public bool IsConnected { get; }
    public string? Message { get; }

    public ConnectionStateChangedEventArgs(bool isConnected, string? message = null)
    {
        IsConnected = isConnected;
        Message = message;
    }
}
