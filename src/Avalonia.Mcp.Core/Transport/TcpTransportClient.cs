using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Avalonia.Mcp.Core.Transport;

/// <summary>
/// TCP-based transport client for connecting to the MCP proxy.
/// </summary>
public class TcpTransportClient : ITransportClient
{
    private readonly string _host;
    private readonly int _port;
    private readonly ILogger? _logger;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private CancellationTokenSource? _receiveCts;
    private Task? _receiveTask;
    private bool _disposed;

    public bool IsConnected => _client?.Connected ?? false;

    public event EventHandler<string>? MessageReceived;
    public event EventHandler<bool>? ConnectionStateChanged;

    public TcpTransportClient(string host, int port, ILogger? logger = null)
    {
        _host = host;
        _port = port;
        _logger = logger;
    }

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Connecting to TCP endpoint: {Host}:{Port}", _host, _port);

            _client = new TcpClient();
            await _client.ConnectAsync(_host, _port, cancellationToken);
            _stream = _client.GetStream();

            // Start receive loop
            _receiveCts = new CancellationTokenSource();
            _receiveTask = Task.Run(() => ReceiveLoopAsync(_receiveCts.Token), _receiveCts.Token);

            ConnectionStateChanged?.Invoke(this, true);
            _logger?.LogInformation("Connected to TCP endpoint");

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to connect to TCP endpoint");
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            _receiveCts?.Cancel();
            
            if (_receiveTask != null)
            {
                await _receiveTask;
            }

            _stream?.Close();
            _client?.Close();

            ConnectionStateChanged?.Invoke(this, false);
            _logger?.LogInformation("Disconnected from TCP endpoint");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during disconnect");
        }
    }

    public async Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        if (_stream == null || !IsConnected)
            throw new InvalidOperationException("Not connected");

        try
        {
            var bytes = Encoding.UTF8.GetBytes(message + "\n"); // Line-delimited messages
            await _stream.WriteAsync(bytes, cancellationToken);
            await _stream.FlushAsync(cancellationToken);
            _logger?.LogDebug("Sent message: {Message}", message);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error sending message");
            throw;
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        var messageBuffer = new StringBuilder();

        try
        {
            while (!cancellationToken.IsCancellationRequested && _stream != null)
            {
                var bytesRead = await _stream.ReadAsync(buffer, cancellationToken);
                
                if (bytesRead == 0)
                {
                    // Connection closed
                    _logger?.LogInformation("Connection closed by remote host");
                    ConnectionStateChanged?.Invoke(this, false);
                    break;
                }

                var text = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                messageBuffer.Append(text);

                // Process line-delimited messages
                var messages = messageBuffer.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
                
                if (messageBuffer.ToString().EndsWith('\n'))
                {
                    // All messages are complete
                    foreach (var msg in messages)
                    {
                        MessageReceived?.Invoke(this, msg.Trim());
                    }
                    messageBuffer.Clear();
                }
                else
                {
                    // Last message is incomplete, keep it in buffer
                    for (int i = 0; i < messages.Length - 1; i++)
                    {
                        MessageReceived?.Invoke(this, messages[i].Trim());
                    }
                    messageBuffer.Clear();
                    messageBuffer.Append(messages[^1]);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogInformation("Receive loop cancelled");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in receive loop");
            ConnectionStateChanged?.Invoke(this, false);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        DisconnectAsync().GetAwaiter().GetResult();
        _receiveCts?.Dispose();
        _stream?.Dispose();
        _client?.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
