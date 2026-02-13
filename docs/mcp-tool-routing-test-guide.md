# MCP Tool Routing - Integration Test Guide

## Prerequisites

1. Build the solution:
   ```bash
   dotnet build WatchTower.slnx
   ```

2. Have the MCP proxy ready to run
3. Have WatchTower (or another app with DiagnosticListener) ready to run

## Manual Integration Test Scenario

### Step 1: Start WatchTower (with Diagnostic Listener)

```bash
dotnet run --project WatchTower/WatchTower.csproj
```

Watch for the diagnostic port announcement in output:
```
DIAGNOSTIC_PORT:51234
```

Note the port number - the proxy will connect to it.

### Step 2: Start the Proxy

```bash
dotnet run --project src/Avalonia.McpProxy/Avalonia.McpProxy.csproj
```

The proxy starts as a system tray app. Right-click the tray icon and select "Show Logs" to see activity.

Expected log output:
```
MCP Proxy starting...
MCP bridge ready on stdio
Found 0 persisted apps
```

### Step 3: Launch App via Proxy (Alternative to Step 1)

Instead of starting WatchTower manually, you can have the proxy launch it. Send this to the proxy's stdin:

```json
{"jsonrpc":"2.0","method":"initialize","id":0,"params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}
```

Then launch the app:
```json
{"jsonrpc":"2.0","method":"tools/call","params":{"name":"launch_app","arguments":{"path":"path/to/WatchTower.exe"}},"id":1}
```

Expected response:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"status\":\"launched\",\"pid\":12345,\"diagnostic_port\":51234,\"message\":\"App launched and listening on port 51234\"}"
      }
    ]
  }
}
```

Expected proxy logs:
```
Launching: path/to/WatchTower.exe
Process started with PID 12345
App listening on diagnostic port 51234
Connecting to app on port 51234...
Connected to WatchTower on port 51234 with 6 tools
```

### Step 4: List Available Tools

```json
{"jsonrpc":"2.0","method":"tools/list","id":2}
```

Expected response:
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "tools": [
      {"name": "launch_app", "description": "Launch an Avalonia app with diagnostic support...", "inputSchema": {...}},
      {"name": "list_apps", "description": "List all connected diagnostic apps...", "inputSchema": {...}},
      {"name": "stop_app", "description": "Stop a connected app...", "inputSchema": {...}},
      {"name": "WatchTower:ClickElement", "description": "[WatchTower] Clicks an element...", "inputSchema": {...}},
      {"name": "WatchTower:TypeText", "description": "[WatchTower] Types text...", "inputSchema": {...}},
      {"name": "WatchTower:CaptureScreenshot", "description": "[WatchTower] Captures a screenshot...", "inputSchema": {...}},
      {"name": "WatchTower:GetElementTree", "description": "[WatchTower] Gets the UI element tree...", "inputSchema": {...}},
      {"name": "WatchTower:FindElement", "description": "[WatchTower] Finds a UI element...", "inputSchema": {...}},
      {"name": "WatchTower:WaitForElement", "description": "[WatchTower] Waits for a UI element...", "inputSchema": {...}}
    ]
  }
}
```

### Step 5: Call a Tool

```json
{"jsonrpc":"2.0","method":"tools/call","params":{"name":"WatchTower:GetElementTree","arguments":{"maxDepth":3}},"id":3}
```

Expected response (success):
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{...tool result data...}"
      }
    ]
  }
}
```

### Step 6: List Connected Apps

```json
{"jsonrpc":"2.0","method":"tools/call","params":{"name":"list_apps","arguments":{}},"id":4}
```

Expected response:
```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"apps\":[{\"name\":\"WatchTower\",\"port\":51234,\"tool_count\":6,\"tools\":[\"ClickElement\",\"TypeText\",...]}],\"count\":1}"
      }
    ]
  }
}
```

### Step 7: Test App Disconnection

1. Close WatchTower
2. Check proxy logs for disconnection message
3. Try calling a WatchTower tool

Expected response:
```json
{
  "jsonrpc": "2.0",
  "id": 5,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"error\":\"App 'WatchTower' not connected\"}"
      }
    ]
  }
}
```

### Step 8: Test Reconnection

1. Restart WatchTower
2. In proxy tray menu, click "Reconnect All Apps" (or the proxy will auto-reconnect on next `launch_app`)
3. Verify tools reappear in `tools/list`

## Expected Behavior Summary

**Success Cases:**
- `initialize` returns protocol version and capabilities
- `tools/list` returns proxy management tools + all app tools (namespaced)
- `tools/call` routes to correct app based on `AppName:` prefix
- Tool results flow back through the proxy to the agent
- Multiple apps can connect with distinct namespaces

**Error Cases:**
- Unknown tool returns error message
- Disconnected app returns error message
- App tools stripped of namespace prefix when forwarded
- Proxy management tools (`launch_app`, `list_apps`, `stop_app`) always available

**Connection Management:**
- Proxy connects TO apps (inverted model)
- App state persisted across proxy restarts
- Reconnection via tray menu or on next launch
- Disconnected apps detected via connection monitoring

## Troubleshooting

### "App not connected" error
- Verify app is running and announced `DIAGNOSTIC_PORT:NNNN`
- Check proxy logs for connection attempt
- Try "Reconnect All Apps" from tray menu

### No tools appearing for app
- Check that DiagnosticListener handshake completed (look for "Connected to AppName" in logs)
- Verify app registered tools with the listener

### stdout corruption (garbled JSON)
- All proxy logging goes to stderr
- Check for stray `Console.WriteLine` calls (should be `Console.Error.WriteLine`)
- Framework logging should use `LogToStandardErrorThreshold = LogLevel.Trace`

### Proxy tray icon not visible
- Check system tray overflow area
- The proxy has no main window - it runs entirely in the tray
