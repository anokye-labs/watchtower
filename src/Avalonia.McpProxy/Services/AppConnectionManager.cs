using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Avalonia.Mcp.Core.Models;

namespace Avalonia.McpProxy.Services;

/// <summary>
/// Listens on a TCP port for inbound connections from diagnostic-enabled apps.
/// Apps connect TO the proxy and send a registration message with their name and tools.
/// </summary>
public class AppConnectionManager
{
    private readonly Action<string> _log;
    private readonly Action<string, int> _onAppConnected;
    private readonly Action<string> _onAppDisconnected;
    private readonly ConcurrentDictionary<string, AppConnection> _connections = new(StringComparer.OrdinalIgnoreCase);
    private readonly int _listenPort;
    private readonly McpProxyBridge _mcpBridge;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;

    public int ListenPort => _listenPort;

    public AppConnectionManager(
        Action<string> log,
        Action<string, int> onAppConnected,
        Action<string> onAppDisconnected,
        int listenPort = 5100)
    {
        _log = log;
        _onAppConnected = onAppConnected;
        _onAppDisconnected = onAppDisconnected;
        _listenPort = listenPort;
        _mcpBridge = new McpProxyBridge(log, this);
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();

        // Start TCP listener for inbound app connections
        _ = ListenForAppsAsync(_cts.Token);

        // Start MCP stdio bridge for agent communication
        _ = _mcpBridge.StartAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _listener?.Stop();

        foreach (var conn in _connections.Values)
        {
            conn.Dispose();
        }
        _connections.Clear();
    }

    /// <summary>
    /// Disconnect and dispose a specific app connection by name.
    /// </summary>
    public void DisconnectApp(string appName)
    {
        if (_connections.TryRemove(appName, out var conn))
        {
            conn.Dispose();
            _onAppDisconnected(appName);
        }
    }

    /// <summary>
    /// Get all currently connected apps with their tools.
    /// </summary>
    public IEnumerable<(string Name, List<McpToolDefinition> Tools)> GetConnectedApps()
    {
        foreach (var conn in _connections.Values)
        {
            if (conn.IsConnected)
            {
                yield return (conn.AppName, conn.Tools);
            }
        }
    }

    /// <summary>
    /// Invoke a tool on a connected app by name.
    /// </summary>
    public async Task<(bool Success, string? Data, string? Error)> InvokeToolAsync(
        string appName, string toolName, JsonElement parameters, CancellationToken ct)
    {
        if (!_connections.TryGetValue(appName, out var conn) || !conn.IsConnected)
        {
            return (false, null, $"App '{appName}' is not connected");
        }

        return await conn.InvokeToolAsync(toolName, parameters, ct);
    }

    private async Task ListenForAppsAsync(CancellationToken ct)
    {
        try
        {
            _listener = new TcpListener(IPAddress.Loopback, _listenPort);
            _listener.Start();
            _log($"Listening for app connections on port {_listenPort}");

            while (!ct.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(ct);
                _log($"Inbound connection from {client.Client.RemoteEndPoint}");
                _ = HandleInboundConnectionAsync(client, ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _log($"Listener error: {ex.Message}");
        }
    }

    private async Task HandleInboundConnectionAsync(TcpClient client, CancellationToken ct)
    {
        try
        {
            var conn = new AppConnection(client, _log);

            // Read the first message — expect a registration
            var (appName, tools) = await conn.ReadRegistrationAsync(ct);

            // Store connection by app name (replaces any previous connection for same app)
            if (_connections.TryGetValue(appName, out var old))
            {
                old.Dispose();
            }
            _connections[appName] = conn;

            _log($"App '{appName}' registered with {tools.Count} tools");
            _onAppConnected(appName, tools.Count);

            // Monitor for disconnection
            _ = conn.MonitorConnectionAsync(ct).ContinueWith(t =>
            {
                _log($"App '{appName}' disconnected");
                _connections.TryRemove(appName, out var removed);
                removed?.Dispose();
                _onAppDisconnected(appName);
            }, TaskScheduler.Default);
        }
        catch (Exception ex)
        {
            _log($"Failed to handle inbound connection: {ex.Message}");
            client.Dispose();
        }
    }
}

internal class AppConnection : IDisposable
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly Action<string> _log;
    private long _correlationId;

    public string AppName { get; private set; } = "";
    public List<McpToolDefinition> Tools { get; } = new();
    public bool IsConnected => _client.Connected;

    public AppConnection(TcpClient client, Action<string> log)
    {
        _client = client;
        _stream = client.GetStream();
        _log = log;
    }

    /// <summary>
    /// Read the first message from the app, expecting:
    /// {"type":"register","appName":"...","tools":[...]}
    /// </summary>
    public async Task<(string AppName, List<McpToolDefinition> Tools)> ReadRegistrationAsync(CancellationToken ct)
    {
        var message = await ReceiveOneAsync(ct);
        var json = JsonDocument.Parse(message);
        var root = json.RootElement;

        var type = root.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : null;
        if (type != "register")
        {
            throw new InvalidOperationException($"Expected 'register' message, got '{type}'");
        }

        AppName = root.GetProperty("appName").GetString() ?? "Unknown";

        if (root.TryGetProperty("tools", out var toolsEl))
        {
            foreach (var tool in toolsEl.EnumerateArray())
            {
                Tools.Add(new McpToolDefinition
                {
                    Name = tool.GetProperty("name").GetString()!,
                    Description = tool.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
                    InputSchema = tool.TryGetProperty("inputSchema", out var s) ? s.Clone() : new { type = "object" }
                });
            }
        }

        return (AppName, Tools);
    }

    public async Task<(bool Success, string? Data, string? Error)> InvokeToolAsync(
        string toolName, JsonElement parameters, CancellationToken ct)
    {
        var correlationId = Interlocked.Increment(ref _correlationId);

        var request = JsonSerializer.Serialize(new
        {
            type = "toolInvocation",
            correlationId,
            tool = toolName,
            parameters
        });

        await SendAsync(request, ct);

        // Wait for response with matching correlation ID
        while (!ct.IsCancellationRequested)
        {
            var response = await ReceiveOneAsync(ct);
            var json = JsonDocument.Parse(response);
            var root = json.RootElement;

            if (root.TryGetProperty("correlationId", out var cid) && cid.GetInt64() == correlationId)
            {
                var result = root.GetProperty("result");
                var success = result.GetProperty("success").GetBoolean();
                var data = result.TryGetProperty("data", out var dataEl) ? dataEl.ToString() : null;
                var error = result.TryGetProperty("error", out var errEl) ? errEl.GetString() : null;
                return (success, data, error);
            }
        }

        return (false, null, "Cancelled");
    }

    public async Task MonitorConnectionAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && _client.Connected)
            {
                await Task.Delay(1000, ct);

                // Check if still connected by polling
                if (_client.Client.Poll(0, SelectMode.SelectRead) && _client.Client.Available == 0)
                {
                    break; // Disconnected
                }
            }
        }
        catch { }
    }

    private async Task SendAsync(string message, CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(message + "\n");
        await _stream.WriteAsync(bytes, ct);
        await _stream.FlushAsync(ct);
    }

    private async Task<string> ReceiveOneAsync(CancellationToken ct)
    {
        var buffer = new byte[16384];
        var sb = new StringBuilder();

        while (!ct.IsCancellationRequested)
        {
            var read = await _stream.ReadAsync(buffer, ct);
            if (read == 0) throw new IOException("Connection closed");

            var data = Encoding.UTF8.GetString(buffer, 0, read);
            sb.Append(data);

            var content = sb.ToString();
            var newlineIdx = content.IndexOf('\n');
            if (newlineIdx >= 0)
            {
                return content[..newlineIdx].Trim();
            }
        }

        throw new OperationCanceledException();
    }

    public void Dispose()
    {
        _stream.Dispose();
        _client.Dispose();
    }
}
