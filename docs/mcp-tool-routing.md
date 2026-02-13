# MCP Tool Routing Implementation

## Overview

This document describes the implementation of bidirectional tool execution routing between the MCP proxy and connected Avalonia applications.

## Architecture

The tool routing system uses a correlation-based request/response pattern across three service classes:

### Components

- **McpProxyBridge** (`Services/McpProxyBridge.cs`): Receives MCP JSON-RPC requests from agents via stdio, routes tool calls to the appropriate app
- **AppConnectionManager** (`Services/AppConnectionManager.cs`): Manages TCP connections to apps, handles handshakes, and forwards tool invocations
- **AppConnection** (internal, in `AppConnectionManager.cs`): Per-app TCP connection with correlation-based request/response tracking

### Request Flow

```
Agent -> Proxy (stdio, tools/call)
  -> McpProxyBridge.HandleToolCallAsync
    -> Parse "AppName:ToolName" format
    -> Find app by name via AppConnectionManager.GetConnectedApps()
    -> AppConnectionManager.InvokeToolAsync(port, toolName, parameters)
      -> AppConnection.InvokeToolAsync
        -> Send toolInvocation with correlationId over TCP
        -> Wait for response with matching correlationId
      <- Return (success, data, error)
    <- Return result
  <- Send MCP response on stdout
<- Agent receives result
```

### Message Formats

#### Tool Invocation (Proxy -> App, TCP)

```json
{
  "type": "toolInvocation",
  "correlationId": 1,
  "tool": "ClickElement",
  "parameters": {
    "x": 100,
    "y": 50
  }
}
```

Note: The tool name sent to the app is the raw name (e.g., `ClickElement`), not the namespaced name (e.g., `WatchTower:ClickElement`). The proxy strips the app prefix before forwarding.

#### Tool Response (App -> Proxy, TCP)

```json
{
  "correlationId": 1,
  "result": {
    "success": true,
    "data": "{\"clicked\":true,\"x\":100,\"y\":50}",
    "error": null
  }
}
```

## Implementation Details

### Correlation Infrastructure (AppConnection)

```csharp
private long _correlationId; // Thread-safe counter via Interlocked.Increment
```

Each `AppConnection` maintains its own correlation ID counter. When `InvokeToolAsync` is called:

1. Generates unique correlation ID via `Interlocked.Increment`
2. Serializes tool invocation with correlation ID
3. Sends over TCP (line-delimited JSON)
4. Reads responses in a loop until matching correlation ID found
5. Returns parsed result

### Tool Name Routing (McpProxyBridge)

The bridge handles three categories of tools:

1. **Proxy management tools**: `launch_app`, `list_apps`, `stop_app` - handled directly
2. **App tools** (contain `:`): Split on `:` to get app name and raw tool name, look up app by name, forward to app
3. **Unknown tools**: Return error

### Connection Management (AppConnectionManager)

- Connections stored in `ConcurrentDictionary<int, AppConnection>` keyed by port
- State persisted to `%LOCALAPPDATA%/AvaloniaProxy/apps.json`
- On startup, loads persisted apps and attempts reconnection
- Connection monitoring via `AppConnection.MonitorConnectionAsync` (polls for disconnection)

## Thread Safety

All operations are thread-safe:
- Correlation ID generation uses `Interlocked.Increment`
- Connections stored in `ConcurrentDictionary`
- TCP stream operations are serialized per-connection

## Error Handling

### App Not Connected

If the target app is not in `GetConnectedApps()`, the bridge returns:
```json
{"error": "App 'AppName' not connected"}
```

### App Disconnected Mid-Request

If TCP read fails during `ReceiveOneAsync`, an `IOException` is thrown and the connection is marked disconnected.

### Unknown Tool

Tools without a `:` separator that don't match proxy management tools return:
```json
{"error": "Unknown tool: toolName"}
```

## Testing

Test coverage focuses on the ViewModel and model layer:

1. **ProxyViewModel_CanBeCreated**: Basic instantiation with correct defaults
2. **ProxyViewModel_ClearLogsCommand**: Log clearing behavior
3. **ConnectedAppInfo**: Model property storage and change notification
4. **RelayCommand**: Command execution and CanExecute behavior

Integration tests for the full TCP routing path require a running DiagnosticListener and are documented in `docs/mcp-tool-routing-test-guide.md`.

## Future Enhancements

1. **Configurable Timeout**: Per-tool timeout configuration
2. **Request Queueing**: Rate limiting and request queuing
3. **Metrics**: Telemetry for request latency and success rates
4. **Cancellation**: Support cancellation of in-flight requests
5. **Streaming Results**: Support tools that stream partial results

## Related Issues

- Parent: #23 - Avalonia MCP Proxy
- Blocks: #51 - Avalonia input system integration
- Blocks: #52 - Tool execution timeout
- Blocks: #53 - Request queueing and rate limiting
