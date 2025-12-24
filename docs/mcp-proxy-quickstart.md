# MCP Proxy Quick Start Guide

This guide walks you through setting up and testing the Avalonia MCP Proxy Platform with WatchTower.

## Prerequisites

- .NET 10 SDK installed
- VS Code (optional, for MCP integration)
- Terminal access

## Step 1: Build the Solution

```bash
cd watchtower
dotnet build
```

Expected output: All 3 projects build successfully (Avalonia.Mcp.Core, Avalonia.McpProxy, WatchTower)

## Step 2: Start the MCP Proxy

In one terminal, start the proxy server:

```bash
dotnet run --project src/Avalonia.McpProxy/Avalonia.McpProxy.csproj -- --stdio --yes
```

The proxy will:
- Load configuration from `.mcpproxy.json`
- Start TCP listener on localhost:5100
- Wait for app connections
- Listen on stdin for MCP agent commands

Expected output:
```
[INFO] Avalonia MCP Proxy v1.0.0
[INFO] Connecting Avalonia applications to AI agents via MCP
[INFO] Loading configuration from: .mcpproxy.json
[INFO] Starting MCP Proxy Server
[INFO] Bind address: localhost:5100
[INFO] TCP listener started on localhost:5100
[INFO] Starting stdio handler for agent communication
[INFO] MCP Proxy Server started successfully
```

## Step 3: Start WatchTower

In another terminal, start WatchTower:

```bash
dotnet run --project WatchTower/WatchTower.csproj
```

WatchTower will:
- Initialize services (including MCP handler)
- Auto-connect to proxy at localhost:5100
- Register its tools with the proxy
- Display the application window

Look for MCP-related log messages:
```
[INFO] MCP handler registered
[INFO] Connecting to proxy: tcp://localhost:5100
[INFO] Connected to TCP endpoint
[INFO] Sent registration message with 6 tools
```

In the proxy terminal, you should see:
```
[INFO] Accepted connection: abc123-...
[INFO] Registered app 'WatchTower' with 6 tools (connection: abc123-...)
```

## Step 4: Query Available Tools (Manual Test)

In the proxy terminal (stdin), send a tools/list request:

```json
{"jsonrpc":"2.0","method":"tools/list","id":1}
```

Expected response (on stdout):
```json
{
  "jsonrpc":"2.0",
  "id":1,
  "result":{
    "tools":[
      {"name":"WatchTower:ClickElement","description":"Click on a UI element at the specified coordinates","inputSchema":{...}},
      {"name":"WatchTower:TypeText","description":"Type text into the focused element","inputSchema":{...}},
      {"name":"WatchTower:CaptureScreenshot","description":"Capture a screenshot of the application window","inputSchema":{...}},
      {"name":"WatchTower:GetElementTree","description":"Get the current UI element tree structure","inputSchema":{...}},
      {"name":"WatchTower:FindElement","description":"Find a UI element by selector","inputSchema":{...}},
      {"name":"WatchTower:WaitForElement","description":"Wait for a UI element to appear","inputSchema":{...}}
    ]
  }
}
```

## Step 5: VS Code MCP Integration (Optional)

If you want to use the proxy with VS Code's MCP support:

1. Copy `mcp.json.example` to your VS Code settings location
2. Open VS Code command palette (Ctrl+Shift+P)
3. Search for "MCP: Connect to Server"
4. Select "avalonia-apps"

The VS Code extension will start the proxy and connect to it automatically.

## Troubleshooting

### Proxy won't start

**Error**: "Address already in use"
- Another process is using port 5100
- Solution: Change `BindAddress` in `.mcpproxy.json` to a different port
- Update WatchTower's `ProxyEndpoint` in `StartupOrchestrator.cs` to match

### WatchTower won't connect

**Error**: "Connection refused"
- Proxy is not running
- Solution: Start proxy first (Step 2)

**Check connection settings**:
- Proxy: listens on `localhost:5100` (from `.mcpproxy.json`)
- WatchTower: connects to `tcp://localhost:5100` (from `StartupOrchestrator.cs`)

### No tools appearing

**Check registration**:
1. Look for "MCP handler registered" in WatchTower logs
2. Look for "Registered app 'WatchTower' with N tools" in proxy logs
3. If missing, check DI registration in `StartupOrchestrator.cs`

### Tools not executing

**Current limitation**: Standard tools are implemented as stubs. They return success but don't actually interact with the UI yet. This requires deeper Avalonia input system integration (future work).

## Next Steps

### For Developers

1. **Implement actual tool logic**: Replace stubs in `StandardUiTools.cs` with real Avalonia input system calls
2. **Add custom tools**: Register domain-specific tools in WatchTower for your workflows
3. **Headless mode**: Test running WatchTower with `HeadlessMode: true` for CI/CD scenarios

### For Agent Integration

1. **Configure your agent**: Add proxy to your agent's MCP server list
2. **Test discovery**: Have agent list available tools
3. **Test execution**: Have agent call tools and verify results
4. **Iterative workflow**: Use agent to inspect UI, suggest changes, verify implementation

### For Multi-App Testing

1. **Create second app**: Clone WatchTower or create new Avalonia app
2. **Add MCP Core**: Reference `Avalonia.Mcp.Core` library
3. **Register handler**: Use different `ApplicationName` and `ProxyEndpoint` port
4. **Update config**: Add second app to `.mcpproxy.json`
5. **Test federation**: Verify both apps' tools appear in proxy

## Configuration Reference

### .mcpproxy.json

```json
{
  "Proxy": {
    "BindAddress": "localhost:5100",  // Where proxy listens for apps
    "LogLevel": "Information",         // Trace, Debug, Information, Warning, Error
    "MaxConnections": 50,              // Max concurrent app connections
    "Apps": [                          // Expected apps (informational)
      {
        "Name": "WatchTower",
        "Endpoint": "tcp://localhost:5000",  // Not used (apps connect TO proxy)
        "Description": "Main application"
      }
    ]
  }
}
```

### StartupOrchestrator.cs (WatchTower)

```csharp
services.AddMcpHandler(config =>
{
    config.ApplicationName = "WatchTower";           // Used for tool namespacing
    config.ProxyEndpoint = "tcp://localhost:5100";   // Where to connect
    config.AutoConnect = true;                        // Connect on startup
    config.ReconnectIntervalMs = 5000;               // Reconnect delay
    config.HeadlessMode = false;                     // GUI mode
}, registerStandardTools: true);                     // Include standard UI tools
```

## Known Limitations

1. **Standard tools are stubs**: They return success but don't actually interact with UI
2. **TCP only**: Named Pipes and HTTP/SSE transports are placeholders
3. **Localhost only**: No network exposure or security
4. **No authentication**: All localhost connections trusted
5. **Tool routing incomplete**: Proxy lists tools but doesn't fully route execution to apps yet

These are documented as future enhancements and don't prevent the platform from demonstrating the architecture and integration patterns.

## Support

- **Issues**: https://github.com/anokye-labs/watchtower/issues
- **Architecture**: See `docs/mcp-proxy-architecture.md`
- **Core Library**: See `src/Avalonia.Mcp.Core/README.md`
- **Proxy Server**: See `src/Avalonia.McpProxy/README.md`
