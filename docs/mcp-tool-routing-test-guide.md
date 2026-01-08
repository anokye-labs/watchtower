# MCP Tool Routing - Integration Test Guide

## Prerequisites

1. Build the solution:
   ```bash
   dotnet build
   ```

2. Have the MCP proxy server ready to run
3. Have WatchTower (or another app using Avalonia.Mcp.Core) ready to run

## Manual Integration Test Scenario

### Step 1: Start the Proxy Server

```bash
cd src/Avalonia.McpProxy
dotnet run
```

Expected output:
```
info: Avalonia.McpProxy.Server.ProxyServer[0]
      Starting MCP Proxy Server
info: Avalonia.McpProxy.Server.ProxyServer[0]
      TCP listener started on localhost:5100
info: Avalonia.McpProxy.Server.ProxyServer[0]
      Starting stdio handler for agent communication
info: Avalonia.McpProxy.Server.ProxyServer[0]
      MCP Proxy Server started successfully
```

### Step 2: Start WatchTower App

In another terminal:
```bash
cd WatchTower
dotnet run
```

Expected proxy output:
```
info: Avalonia.McpProxy.Server.ProxyServer[0]
      Accepted connection: {ConnectionId}
info: Avalonia.McpProxy.Server.AppRegistry[0]
      Registered app 'WatchTower' with {N} tools (connection: {ConnectionId})
```

### Step 3: List Available Tools via Proxy

Send a `tools/list` request to the proxy's stdin:

```json
{"jsonrpc":"2.0","method":"tools/list","id":1}
```

Expected output:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "tools": [
      {
        "name": "WatchTower:ClickElement",
        "description": "Clicks an element at the specified coordinates",
        "inputSchema": { ... }
      },
      {
        "name": "WatchTower:TypeText",
        "description": "Types text into the focused element",
        "inputSchema": { ... }
      }
      // ... more tools
    ]
  }
}
```

### Step 4: Call a Tool

Send a `tools/call` request:

```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "WatchTower:ClickElement",
    "arguments": {
      "x": 100,
      "y": 50
    }
  },
  "id": 2
}
```

Expected proxy logs:
```
info: Avalonia.McpProxy.Server.ProxyServer[0]
      Forwarded tool 'WatchTower:ClickElement' to app 'WatchTower' with correlation ID 1
debug: Avalonia.McpProxy.Server.ProxyServer[0]
      Completed pending request 1
```

Expected output (success):
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"clicked\":true,\"x\":100,\"y\":50}"
      }
    ]
  }
}
```

Expected output (error):
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "Error: Element not found at coordinates"
      }
    ]
  }
}
```

### Step 5: Test Timeout Handling

Call a tool that takes longer than 30 seconds (if available) or disconnect the app mid-request.

Expected output after 30s:
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "Error: Tool execution timed out after 30 seconds"
      }
    ]
  }
}
```

Expected proxy logs:
```
warn: Avalonia.McpProxy.Server.ProxyServer[0]
      Tool 'WatchTower:SomeTool' timed out (correlation ID: 2)
```

### Step 6: Test App Disconnection

1. Close the WatchTower app
2. Try to call a tool

Expected proxy logs:
```
info: Avalonia.McpProxy.Server.ProxyServer[0]
      Connection closed: {ConnectionId}
info: Avalonia.McpProxy.Server.AppRegistry[0]
      Marked app 'WatchTower' as disconnected
```

Expected output:
```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "error": {
    "code": -32000,
    "message": "App 'WatchTower' is not connected"
  }
}
```

## Expected Behavior Summary

✅ **Success Cases:**
- Tools list returns all registered tools from connected apps
- Tool calls route to correct app based on tool name prefix
- Tool execution results return to agent
- Multiple apps can connect and expose different tools

✅ **Error Cases:**
- Unknown tool returns error
- Disconnected app returns error
- Tool execution timeout returns error after 30s
- Tool execution errors are properly propagated

✅ **Connection Management:**
- Apps can connect and disconnect dynamically
- Disconnected apps' tools are removed from tool list
- Reconnected apps re-register their tools

## Automated Integration Tests

To create automated integration tests:

1. Create a mock app that connects to the proxy
2. Register fake tools with known behavior
3. Send tool invocation requests via proxy stdin
4. Verify responses match expected format
5. Test timeout scenarios with delayed responses
6. Test error scenarios with failing tools

See `src/Avalonia.McpProxy.Tests/Integration/` for examples (to be implemented).

## Troubleshooting

### "Tool not found" error
- Check that the app is connected
- Verify tool name includes app prefix (e.g., "WatchTower:ToolName")
- Check app registered successfully

### "App not connected" error
- Verify app is running
- Check TCP connection on port 5100
- Review proxy logs for connection acceptance

### Timeout after 30 seconds
- Tool execution is taking too long
- Check app logs for tool execution errors
- Consider increasing timeout (requires code change)

### No response from proxy
- Check proxy is running
- Verify stdin message is valid JSON-RPC
- Check proxy logs for parsing errors
