namespace Avalonia.Mcp.Core.Transport;

/// <summary>
/// Interface for transport clients that connect to the MCP proxy.
/// </summary>
public interface ITransportClient : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets whether the client is connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Event raised when a message is received.
    /// </summary>
    event EventHandler<string>? MessageReceived;

    /// <summary>
    /// Event raised when connection state changes.
    /// </summary>
    event EventHandler<bool>? ConnectionStateChanged;

    /// <summary>
    /// Connects to the transport endpoint.
    /// </summary>
    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the transport endpoint.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Sends a message through the transport.
    /// </summary>
    Task SendMessageAsync(string message, CancellationToken cancellationToken = default);
}
