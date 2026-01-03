# MCP Tool Routing Implementation

## Overview

This document describes the implementation of bidirectional tool execution routing between the MCP proxy server and connected Avalonia applications.

## Architecture

The tool routing system uses a request/response correlation pattern with the following components:

### 1. Correlation Infrastructure (ProxyServer)

- **Correlation ID Generation**: Thread-safe counter using `Interlocked.Increment`
- **Pending Requests**: `ConcurrentDictionary<long, TaskCompletionSource<McpToolResult>>` tracks in-flight requests
- **Stream Management**: `ConcurrentDictionary<string, NetworkStream>` stores app TCP connections

### 2. Request Flow

```
Agent → Proxy (tools/call) → App (toolInvocation) → Execution → App (toolResponse) → Proxy (result) → Agent
```

### 3. Message Formats

#### Tool Invocation (Proxy → App)

```json
{
  "type": "toolInvocation",
  "correlationId": 123,
  "tool": "WatchTower:ClickElement",
  "parameters": {
    "x": 100,
    "y": 50
  }
}
```

#### Tool Response (App → Proxy)

```json
{
  "type": "toolResponse",
  "correlationId": 123,
  "result": {
    "success": true,
    "data": { "clicked": true },
    "error": null
  }
}
```

## Implementation Details

### ProxyServer Changes

#### Added Fields

```csharp
private readonly ConcurrentDictionary<string, NetworkStream> _appStreams = new();
private long _nextCorrelationId;
private readonly ConcurrentDictionary<long, TaskCompletionSource<McpToolResult>> _pendingRequests = new();
```

#### HandleCallToolAsync

1. Validates tool exists and app is connected
2. Generates unique correlation ID
3. Creates `TaskCompletionSource` for response tracking
4. Sends tool invocation to app via TCP
5. Waits for response with 30-second timeout
6. Converts result to MCP protocol format
7. Sends response back to agent

#### HandleAppMessageAsync

Handles two message types:

- **register**: App registration (existing)
- **toolResponse**: Tool execution results (new)
  - Extracts correlation ID
  - Looks up pending request
  - Completes TaskCompletionSource with result

### McpHandler Changes

#### OnMessageReceived

1. Parses incoming message type
2. Extracts correlation ID, tool name, and parameters
3. Creates `McpToolInvocation` object
4. Executes tool via existing `ExecuteToolAsync`
5. Wraps result in response message with correlation ID
6. Sends response back to proxy

## Timeout Handling

Default timeout: **30 seconds**

When a timeout occurs:
1. Pending request is removed from dictionary
2. `McpToolResult.Fail("Tool execution timed out after 30 seconds")` is created
3. Error is sent back to agent in MCP format

## Error Handling

### App Disconnection

If an app disconnects while a request is pending:
- The proxy detects broken connection during write
- Exception is caught in `HandleCallToolAsync`
- Pending request is cleaned up
- Error is sent to agent

### Unknown Correlation ID

If a response arrives with an unknown correlation ID:
- Warning is logged
- Response is ignored
- Original request eventually times out

## Testing

Test coverage includes:

1. **ProxyServer_CanBeCreated**: Basic instantiation
2. **AppRegistry_RegisterApp_StoresAppCorrectly**: App registration
3. **AppRegistry_FindAppByTool_ReturnsCorrectApp**: Tool lookup
4. **AppRegistry_FindAppByTool_ReturnsNullForUnknownTool**: Missing tool handling
5. **AppRegistry_GetAllTools_ReturnsAllToolsFromConnectedApps**: Multi-app tool aggregation
6. **AppRegistry_MarkDisconnected_UpdatesConnectionStatus**: Disconnection handling

All tests pass: **6/6**

## Performance Characteristics

- **Correlation ID Generation**: O(1) atomic operation
- **Pending Request Lookup**: O(1) concurrent dictionary access
- **Memory Overhead**: ~100 bytes per pending request
- **Typical Latency**: < 100ms for simple tools (depends on tool execution time)

## Thread Safety

All operations are thread-safe:
- Correlation ID generation uses `Interlocked.Increment`
- Pending requests use `ConcurrentDictionary`
- App streams use `ConcurrentDictionary`
- TaskCompletionSource operations are thread-safe

## Future Enhancements

1. **Configurable Timeout**: Make timeout duration configurable per tool
2. **Request Queueing**: Add rate limiting and request queuing (blocked by this issue)
3. **Metrics**: Add telemetry for request latency and success rates
4. **Cancellation**: Support cancellation of in-flight requests
5. **Streaming Results**: Support tools that stream partial results

## Related Issues

- Parent: #23 - Avalonia MCP Proxy
- Blocks: #51 - Avalonia input system integration
- Blocks: #52 - Tool execution timeout
- Blocks: #53 - Request queueing and rate limiting
