using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Avalonia.Mcp.Core.Diagnostics;

/// <summary>
/// Diagnostic listener that accepts connections from the MCP proxy.
/// Apps listen; proxy connects. This allows proxy to restart and reconnect.
/// </summary>
public class DiagnosticListener : IDisposable
{
    private readonly string _appName;
    private readonly List<DiagnosticTool> _tools;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private readonly List<DiagnosticClient> _clients = new();
    private readonly object _lock = new();
    private bool _disposed;

    public int Port { get; private set; }
    public bool IsListening => _listener != null;
    
    /// <summary>
    /// Fired when a tool is invoked by the proxy.
    /// </summary>
    public event Func<string, JsonElement, Task<DiagnosticResult>>? OnToolInvoked;

    public DiagnosticListener(string appName, IEnumerable<DiagnosticTool> tools)
    {
        _appName = appName;
        _tools = tools.ToList();
    }

    /// <summary>
    /// Start listening on an available port. Returns the port number.
    /// </summary>
    public int Start(int preferredPort = 0)
    {
        if (_listener != null)
            throw new InvalidOperationException("Already listening");

        _cts = new CancellationTokenSource();
        
        // Find an available port (0 = let OS pick)
        _listener = new TcpListener(IPAddress.Loopback, preferredPort);
        _listener.Start();
        
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        
        // Start accepting connections
        _ = AcceptConnectionsAsync(_cts.Token);
        
        Console.WriteLine($"[Diagnostics] Listening on port {Port}");
        
        return Port;
    }

    /// <summary>
    /// Stop listening and disconnect all clients.
    /// </summary>
    public void Stop()
    {
        _cts?.Cancel();
        _listener?.Stop();
        _listener = null;
        
        lock (_lock)
        {
            foreach (var client in _clients)
            {
                client.Dispose();
            }
            _clients.Clear();
        }
        
        Console.WriteLine("[Diagnostics] Stopped listening");
    }

    private async Task AcceptConnectionsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener != null)
        {
            try
            {
                var tcpClient = await _listener.AcceptTcpClientAsync(ct);
                Console.WriteLine($"[Diagnostics] Proxy connected from {tcpClient.Client.RemoteEndPoint}");
                
                var client = new DiagnosticClient(tcpClient, _appName, _tools, OnToolInvoked);
                
                lock (_lock)
                {
                    _clients.Add(client);
                }
                
                _ = client.HandleConnectionAsync(ct).ContinueWith(_ =>
                {
                    lock (_lock)
                    {
                        _clients.Remove(client);
                    }
                    client.Dispose();
                    Console.WriteLine("[Diagnostics] Proxy disconnected");
                }, TaskScheduler.Default);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Diagnostics] Accept error: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _cts?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Handles a single proxy connection.
/// </summary>
internal class DiagnosticClient : IDisposable
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly string _appName;
    private readonly List<DiagnosticTool> _tools;
    private readonly Func<string, JsonElement, Task<DiagnosticResult>>? _onToolInvoked;

    public DiagnosticClient(
        TcpClient client, 
        string appName, 
        List<DiagnosticTool> tools,
        Func<string, JsonElement, Task<DiagnosticResult>>? onToolInvoked)
    {
        _client = client;
        _stream = client.GetStream();
        _appName = appName;
        _tools = tools;
        _onToolInvoked = onToolInvoked;
    }

    public async Task HandleConnectionAsync(CancellationToken ct)
    {
        var buffer = new byte[16384];
        var messageBuffer = new StringBuilder();

        try
        {
            while (_client.Connected && !ct.IsCancellationRequested)
            {
                var read = await _stream.ReadAsync(buffer, ct);
                if (read == 0) break;

                var data = Encoding.UTF8.GetString(buffer, 0, read);
                messageBuffer.Append(data);

                // Process complete messages (newline-delimited)
                var content = messageBuffer.ToString();
                var lines = content.Split('\n');

                // Process all complete lines
                for (int i = 0; i < lines.Length - 1; i++)
                {
                    var line = lines[i].Trim();
                    if (!string.IsNullOrEmpty(line))
                    {
                        await HandleMessageAsync(line, ct);
                    }
                }

                // Keep incomplete last line in buffer
                messageBuffer.Clear();
                messageBuffer.Append(lines[^1]);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Console.WriteLine($"[Diagnostics] Client error: {ex.Message}");
        }
    }

    private async Task HandleMessageAsync(string message, CancellationToken ct)
    {
        try
        {
            var json = JsonDocument.Parse(message);
            var root = json.RootElement;

            if (!root.TryGetProperty("type", out var typeEl))
                return;

            var type = typeEl.GetString();

            switch (type)
            {
                case "handshake":
                    await SendHandshakeResponseAsync(ct);
                    break;

                case "toolInvocation":
                    await HandleToolInvocationAsync(root, ct);
                    break;
                    
                case "shutdown":
                    Console.WriteLine("[Diagnostics] Shutdown requested by proxy");
                    Environment.Exit(0);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Diagnostics] Message error: {ex.Message}");
        }
    }

    private async Task SendHandshakeResponseAsync(CancellationToken ct)
    {
        var response = JsonSerializer.Serialize(new
        {
            type = "handshakeResponse",
            appName = _appName,
            pid = Environment.ProcessId,
            tools = _tools.Select(t => new
            {
                name = t.Name,
                description = t.Description,
                inputSchema = t.InputSchema
            })
        });

        await SendAsync(response, ct);
        Console.WriteLine($"[Diagnostics] Sent handshake: {_appName} with {_tools.Count} tools");
    }

    private async Task HandleToolInvocationAsync(JsonElement root, CancellationToken ct)
    {
        var correlationId = root.GetProperty("correlationId").GetInt64();
        var toolName = root.GetProperty("tool").GetString()!;
        var parameters = root.TryGetProperty("parameters", out var p) ? p : default;

        Console.WriteLine($"[Diagnostics] Tool call: {toolName}");

        DiagnosticResult result;
        try
        {
            if (_onToolInvoked != null)
            {
                result = await _onToolInvoked(toolName, parameters);
            }
            else
            {
                result = DiagnosticResult.Fail($"No handler for tool: {toolName}");
            }
        }
        catch (Exception ex)
        {
            result = DiagnosticResult.Fail($"Tool error: {ex.Message}");
        }

        var response = JsonSerializer.Serialize(new
        {
            type = "toolResponse",
            correlationId,
            result = new
            {
                success = result.Success,
                data = result.Data,
                error = result.ErrorMessage
            }
        });

        await SendAsync(response, ct);
    }

    private async Task SendAsync(string message, CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(message + "\n");
        await _stream.WriteAsync(bytes, ct);
        await _stream.FlushAsync(ct);
    }

    public void Dispose()
    {
        _stream.Dispose();
        _client.Dispose();
    }
}

/// <summary>
/// Defines a diagnostic tool exposed by the app.
/// </summary>
public class DiagnosticTool
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required object InputSchema { get; init; }
}

/// <summary>
/// Result from a diagnostic tool invocation.
/// </summary>
public class DiagnosticResult
{
    public bool Success { get; init; }
    public string? Data { get; init; }
    public string? ErrorMessage { get; init; }

    public static DiagnosticResult Ok(string data) => new() { Success = true, Data = data };
    public static DiagnosticResult Fail(string error) => new() { Success = false, ErrorMessage = error };
}
