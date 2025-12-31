using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WatchTower.Tools.McpServer.Models;

namespace WatchTower.Tools.McpServer;

/// <summary>
/// Manages TCP connection to a connected application and handles message sending.
/// </summary>
public class AppConnection : IDisposable
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppConnection"/> class.
    /// </summary>
    /// <param name="client">The TCP client connected to the app.</param>
    public AppConnection(TcpClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _stream = _client.GetStream();
    }

    /// <summary>
    /// Gets a value indicating whether the connection is still active.
    /// </summary>
    public bool IsConnected => _client.Connected;

    /// <summary>
    /// Sends a JSON-RPC 2.0 request to the connected app via TCP with newline-delimited framing.
    /// </summary>
    /// <param name="request">The JSON-RPC request to send.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous send operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the connection is closed.</exception>
    public async Task SendAsync(JsonRpcRequest request, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AppConnection));
        }

        if (!IsConnected)
        {
            throw new InvalidOperationException("Connection is not active.");
        }

        // Serialize the request to JSON
        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Append newline for message framing (newline-delimited JSON)
        var message = json + "\n";
        var messageBytes = Encoding.UTF8.GetBytes(message);

        // Send the message
        await _stream.WriteAsync(messageBytes, cancellationToken).ConfigureAwait(false);
        await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Disposes the connection and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            _stream?.Dispose();
            _client?.Dispose();
        }
        finally
        {
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}
