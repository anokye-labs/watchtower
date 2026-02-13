# MCP Proxy Quick Start Guide

This guide walks you through setting up and testing the Avalonia MCP Proxy Platform with WatchTower.

## Prerequisites

- .NET 10 SDK installed
- VS Code (optional, for MCP integration)
- Terminal access
- Windows 10/11 (proxy is win-x64 native)

## Step 1: Build the Solution

```bash
cd watchtower
dotnet build WatchTower.slnx
```

Expected output: All projects build successfully (Avalonia.Mcp.Core, Avalonia.McpProxy, WatchTower, tests).

## Step 2: Start WatchTower (with Diagnostic Listener)

WatchTower embeds a `DiagnosticListener` that the proxy will connect to. Start it first:

```bash
dotnet run --project WatchTower/WatchTower.csproj
```

WatchTower will:
- Initialize services (including diagnostic listener)
- Listen on a random TCP port for proxy connections
- Print `DIAGNOSTIC_PORT:NNNN` to stdout
- Display the application window

Look for the diagnostic port announcement in output:
```
DIAGNOSTIC_PORT:51234
```

## Step 3: Start the MCP Proxy

The proxy is a system tray application. Start it:

```bash
dotnet run --project src/Avalonia.McpProxy/Avalonia.McpProxy.csproj
```

The proxy will:
- Show a system tray icon
- Load any persisted app state from `%LOCALAPPDATA%/AvaloniaProxy/apps.json`
- Attempt to reconnect to known apps
- Start the MCP stdio bridge for agent communication

Right-click the tray icon to:
- **Show Logs** - Open the log viewer window
- **Reconnect All Apps** - Retry connections to known apps
- **Exit** - Shut down the proxy

## Step 4: Connect an Agent

### VS Code MCP Integration

To use the proxy with VS Code's MCP support, configure your MCP settings to launch the proxy:

```json
{
  "mcpServers": {
    "avalonia-apps": {
      "command": "dotnet",
      "args": ["run", "--project", "src/Avalonia.McpProxy/Avalonia.McpProxy.csproj"]
    }
  }
}
```

### Manual Testing (stdin)

You can test the MCP protocol by typing JSON-RPC into the proxy's stdin:

**List available tools:**
```json
{"jsonrpc":"2.0","method":"initialize","id":0,"params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}
{"jsonrpc":"2.0","method":"tools/list","id":1}
```

**Launch an app:**
```json
{"jsonrpc":"2.0","method":"tools/call","params":{"name":"launch_app","arguments":{"path":"path/to/WatchTower.exe"}},"id":2}
```

**List connected apps:**
```json
{"jsonrpc":"2.0","method":"tools/call","params":{"name":"list_apps","arguments":{}},"id":3}
```

**Call an app tool:**
```json
{"jsonrpc":"2.0","method":"tools/call","params":{"name":"WatchTower:GetElementTree","arguments":{"maxDepth":3}},"id":4}
```

## How It Works

### Connection Flow

1. App starts and opens a `DiagnosticListener` on a random TCP port
2. App prints `DIAGNOSTIC_PORT:NNNN` to stdout
3. Proxy discovers the port (via `launch_app` tool or persisted state)
4. Proxy connects TO the app via TCP
5. Proxy sends handshake, app responds with name and tool list
6. Proxy federates the app's tools under `AppName:ToolName` namespace

### Proxy Management Tools

The proxy exposes three built-in tools to agents:

| Tool | Description |
|------|-------------|
| `launch_app` | Launch an Avalonia app and discover its diagnostic port |
| `list_apps` | List all connected apps with tool counts |
| `stop_app` | Send shutdown signal to an app by port |

### Tool Namespacing

All app tools are namespaced to avoid conflicts:
- App registers tool `ClickElement`
- Proxy exposes it as `WatchTower:ClickElement`

## Troubleshooting

### No tools appearing

1. Check that WatchTower is running and printed `DIAGNOSTIC_PORT:NNNN`
2. Check proxy log window (right-click tray icon > Show Logs)
3. Look for "Connected to [AppName] on port NNNN with N tools" in logs
4. If the proxy doesn't know about the app, use `launch_app` or restart both

### App not connecting

- The proxy connects TO the app, not the other way around
- Ensure the app's DiagnosticListener is running (check stdout for port announcement)
- Try "Reconnect All Apps" from the tray menu

### stdout corruption (broken JSON-RPC)

All proxy logging goes to stderr, not stdout. If you see non-JSON output on stdout, check for stray `Console.WriteLine` calls - they should be `Console.Error.WriteLine`.

### Proxy tray icon not visible

- The proxy runs as a system tray app with no main window
- Check the system tray / notification area (may need to expand it)
- On first launch, Windows may hide the icon - check overflow area

## Known Limitations

1. **Standard UI tools need deeper integration**: They work through DiagnosticListener but need actual Avalonia input system wiring for full functionality
2. **TCP only**: Named Pipes transport is not yet implemented
3. **Localhost only**: No network exposure or security
4. **No authentication**: All localhost connections are trusted
5. **Dual patterns in Core**: Legacy `McpHandler`/`TcpTransportClient` coexists with new `DiagnosticListener`

## Next Steps

### For Developers

1. **Wire up DiagnosticListener in your app**: Embed the listener and register tools
2. **Add custom tools**: Register domain-specific tools via DiagnosticListener
3. **Test with agents**: Configure VS Code or Claude Desktop to use the proxy

### For Multi-App Testing

1. Start multiple Avalonia apps with DiagnosticListener
2. Each announces its own port
3. Use `launch_app` or persisted state for proxy to discover them
4. Verify all apps' tools appear namespaced in `tools/list`

## Support

- **Issues**: https://github.com/anokye-labs/watchtower/issues
- **Architecture**: See `docs/mcp-proxy-architecture.md`
- **Core Library**: See `src/Avalonia.Mcp.Core/README.md`
- **Proxy**: See `src/Avalonia.McpProxy/README.md`
