using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Avalonia.Mcp.Core.Models;

namespace Avalonia.McpProxy.Services;

/// <summary>
/// Manages connections to diagnostic-enabled apps.
/// Apps listen on ports; proxy connects to them.
/// State is persisted so proxy can reconnect after restart.
/// </summary>
public class AppConnectionManager
{
    private readonly Action<string> _log;
    private readonly Action<string, int, int> _onAppConnected;
    private readonly Action<int> _onAppDisconnected;
    private readonly ConcurrentDictionary<int, AppConnection> _connections = new();
    private readonly string _stateFilePath;
    private readonly McpProxyBridge _mcpBridge;
    private CancellationTokenSource? _cts;

    public AppConnectionManager(
        Action<string> log,
        Action<string, int, int> onAppConnected,
        Action<int> onAppDisconnected)
    {
        _log = log;
        _onAppConnected = onAppConnected;
        _onAppDisconnected = onAppDisconnected;
        _stateFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AvaloniaProxy",
            "apps.json");
        _mcpBridge = new McpProxyBridge(log, this);
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        
        // Load persisted app state and try to reconnect
        LoadAndReconnect();
        
        // Start MCP stdio bridge for agent communication
        _ = _mcpBridge.StartAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        
        foreach (var conn in _connections.Values)
        {
            conn.Dispose();
        }
        _connections.Clear();
    }

    public void ReconnectAll()
    {
        LoadAndReconnect();
    }

    /// <summary>
    /// Register a new app that was just launched. Called by ProcessManager.
    /// </summary>
    public void RegisterLaunchedApp(int port, int pid, string path)
    {
        var appInfo = new PersistedAppInfo
        {
            Port = port,
            Pid = pid,
            Path = path,
            LaunchedAt = DateTime.UtcNow
        };
        
        SaveAppInfo(appInfo);
        _ = ConnectToAppAsync(port, _cts?.Token ?? CancellationToken.None);
    }

    /// <summary>
    /// Get all currently connected apps with their tools.
    /// </summary>
    public IEnumerable<(string Name, int Port, List<McpToolDefinition> Tools)> GetConnectedApps()
    {
        foreach (var conn in _connections.Values)
        {
            if (conn.IsConnected)
            {
                yield return (conn.AppName, conn.Port, conn.Tools);
            }
        }
    }

    /// <summary>
    /// Invoke a tool on a connected app.
    /// </summary>
    public async Task<(bool Success, string? Data, string? Error)> InvokeToolAsync(
        int port, string toolName, JsonElement parameters, CancellationToken ct)
    {
        if (!_connections.TryGetValue(port, out var conn) || !conn.IsConnected)
        {
            return (false, null, $"App on port {port} is not connected");
        }
        
        return await conn.InvokeToolAsync(toolName, parameters, ct);
    }

    private void LoadAndReconnect()
    {
        var apps = LoadPersistedApps();
        _log($"Found {apps.Count} persisted apps");
        
        foreach (var app in apps)
        {
            _ = ConnectToAppAsync(app.Port, _cts?.Token ?? CancellationToken.None);
        }
    }

    private async Task ConnectToAppAsync(int port, CancellationToken ct)
    {
        // Don't reconnect if already connected
        if (_connections.TryGetValue(port, out var existing) && existing.IsConnected)
        {
            _log($"Already connected to port {port}");
            return;
        }

        try
        {
            _log($"Connecting to app on port {port}...");
            
            var client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", port, ct);
            
            var conn = new AppConnection(port, client, _log);
            _connections[port] = conn;
            
            // Send handshake and wait for response
            var (appName, tools) = await conn.HandshakeAsync(ct);
            
            _log($"Connected to {appName} on port {port} with {tools.Count} tools");
            _onAppConnected(appName, port, tools.Count);
            
            // Start listening for disconnection
            _ = conn.MonitorConnectionAsync(ct).ContinueWith(_ =>
            {
                _log($"App on port {port} disconnected");
                _onAppDisconnected(port);
            }, TaskScheduler.Default);
        }
        catch (SocketException)
        {
            _log($"Could not connect to port {port} - app may not be running");
            RemoveAppInfo(port);
        }
        catch (Exception ex)
        {
            _log($"Error connecting to port {port}: {ex.Message}");
        }
    }

    private List<PersistedAppInfo> LoadPersistedApps()
    {
        try
        {
            if (File.Exists(_stateFilePath))
            {
                var json = File.ReadAllText(_stateFilePath);
                return JsonSerializer.Deserialize<List<PersistedAppInfo>>(json) ?? new();
            }
        }
        catch (Exception ex)
        {
            _log($"Error loading app state: {ex.Message}");
        }
        return new();
    }

    private void SaveAppInfo(PersistedAppInfo app)
    {
        try
        {
            var dir = Path.GetDirectoryName(_stateFilePath)!;
            Directory.CreateDirectory(dir);
            
            var apps = LoadPersistedApps();
            apps.RemoveAll(a => a.Port == app.Port);
            apps.Add(app);
            
            var json = JsonSerializer.Serialize(apps, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_stateFilePath, json);
        }
        catch (Exception ex)
        {
            _log($"Error saving app state: {ex.Message}");
        }
    }

    private void RemoveAppInfo(int port)
    {
        try
        {
            var apps = LoadPersistedApps();
            apps.RemoveAll(a => a.Port == port);
            
            var json = JsonSerializer.Serialize(apps, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_stateFilePath, json);
        }
        catch { }
    }
}

internal class PersistedAppInfo
{
    public int Port { get; set; }
    public int Pid { get; set; }
    public string Path { get; set; } = "";
    public DateTime LaunchedAt { get; set; }
}

internal class AppConnection : IDisposable
{
    private readonly int _port;
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly Action<string> _log;
    private long _correlationId;

    public int Port => _port;
    public string AppName { get; private set; } = "";
    public List<McpToolDefinition> Tools { get; } = new();
    public bool IsConnected => _client.Connected;

    public AppConnection(int port, TcpClient client, Action<string> log)
    {
        _port = port;
        _client = client;
        _stream = client.GetStream();
        _log = log;
    }

    public async Task<(string AppName, List<McpToolDefinition> Tools)> HandshakeAsync(CancellationToken ct)
    {
        // Send handshake request
        var handshake = JsonSerializer.Serialize(new { type = "handshake" });
        await SendAsync(handshake, ct);
        
        // Wait for handshake response
        var response = await ReceiveOneAsync(ct);
        var json = JsonDocument.Parse(response);
        var root = json.RootElement;
        
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
                var data = result.TryGetProperty("data", out var d) ? d.GetString() : null;
                var error = result.TryGetProperty("error", out var e) ? e.GetString() : null;
                return (success, data, error);
            }
        }
        
        return (false, null, "Cancelled");
    }

    public async Task MonitorConnectionAsync(CancellationToken ct)
    {
        var buffer = new byte[1];
        try
        {
            while (!ct.IsCancellationRequested && _client.Connected)
            {
                // Just wait for disconnection
                await Task.Delay(1000, ct);
                
                // Check if still connected by peeking
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
