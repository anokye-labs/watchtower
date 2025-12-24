# Avalonia MCP Proxy Platform - Architecture

## Overview

The Avalonia MCP Proxy Platform enables AI agents (Claude, GitHub Copilot, etc.) to interact with Avalonia applications through the Model Context Protocol (MCP). The platform consists of three main components:

1. **Avalonia.Mcp.Core** - Reusable library for embedding MCP handlers in Avalonia apps
2. **Avalonia.McpProxy** - Standalone proxy server that federates multiple app handlers
3. **Client Apps** (e.g., WatchTower) - Avalonia applications using the core library

## System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    AI Agent (Claude, Copilot)                    │
│                                                                   │
│  Capabilities:                                                    │
│  • Discover tools via MCP protocol                              │
│  • Execute tools on any connected Avalonia app                  │
│  • Receive real-time feedback                                   │
│  • Iterate on development/testing workflows                     │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             │ MCP Protocol (stdio)
                             │ JSON-RPC 2.0
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│              Avalonia.McpProxy (Standalone Server)               │
│                                                                   │
│  ┌────────────────┐  ┌────────────────┐  ┌──────────────────┐  │
│  │  Stdio Handler │  │  App Registry  │  │  Tool Router     │  │
│  │                │  │                │  │                  │  │
│  │ • MCP Protocol │  │ • Track Apps   │  │ • Route Calls    │  │
│  │ • List Tools   │  │ • Track Tools  │  │ • Forward Results│  │
│  │ • Call Tools   │  │ • Live Updates │  │ • Error Handling │  │
│  └────────────────┘  └────────────────┘  └──────────────────┘  │
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │          TCP Listener (localhost:5100)                    │  │
│  │  • Accepts app connections                                │  │
│  │  • Handles registration messages                          │  │
│  │  • Maintains persistent connections                       │  │
│  └──────────────────────────────────────────────────────────┘  │
└──────────────┬──────────────┬──────────────┬──────────────────┘
               │              │              │
               │ TCP          │ TCP          │ TCP
               │              │              │
┌──────────────▼─┐  ┌─────────▼──┐  ┌───────▼──────┐
│  WatchTower    │  │  App 2     │  │  App N       │
│                │  │            │  │              │
│  Avalonia.Mcp  │  │  Avalonia  │  │  Avalonia    │
│  .Core         │  │  .Mcp.Core │  │  .Mcp.Core   │
│  (Embedded)    │  │  (Embedded)│  │  (Embedded)  │
│                │  │            │  │              │
│  • Standard    │  │  • Standard│  │  • Standard  │
│    Tools       │  │    Tools   │  │    Tools     │
│  • Custom      │  │  • Custom  │  │  • Custom    │
│    Tools       │  │    Tools   │  │    Tools     │
│  • TCP Client  │  │  • TCP     │  │  • TCP       │
│                │  │    Client  │  │    Client    │
└────────────────┘  └────────────┘  └──────────────┘
```

## Component Details

### 1. Avalonia.Mcp.Core

**Purpose**: Embeddable library that provides MCP capabilities to any Avalonia application.

**Key Classes**:

- **IMcpHandler**: Main interface for MCP functionality
  - Manages tool registration
  - Handles tool execution
  - Manages connection to proxy

- **McpHandler**: Default implementation of IMcpHandler
  - Tool catalog management
  - Tool invocation routing
  - Connection lifecycle

- **StandardUiTools**: Pre-built UI interaction tools
  - ClickElement(x, y)
  - TypeText(text)
  - CaptureScreenshot(format)
  - GetElementTree(maxDepth)
  - FindElement(selector)
  - WaitForElement(selector, timeoutMs)

- **Transport Layer**:
  - ITransportClient: Abstract transport interface
  - TcpTransportClient: TCP-based implementation
  - TransportClientFactory: Creates appropriate transport

**Integration Pattern**:

```csharp
// In your Avalonia app's service registration:
services.AddMcpHandler(config =>
{
    config.ApplicationName = "MyApp";
    config.ProxyEndpoint = "tcp://localhost:5100";
    config.AutoConnect = true;
}, registerStandardTools: true);
```

### 2. Avalonia.McpProxy

**Purpose**: Standalone server that aggregates multiple app handlers and exposes unified MCP interface.

**Key Classes**:

- **ProxyServer**: Main server implementation
  - Stdio handler for agent communication (MCP protocol)
  - TCP listener for app connections
  - Message routing between agents and apps

- **AppRegistry**: Manages connected applications
  - Tracks registered apps
  - Maintains tool catalog
  - Handles app lifecycle (connect/disconnect)

- **ProxyConfiguration**: Server configuration
  - Bind address for TCP listener
  - Expected app list (informational)
  - Logging and connection limits

**Startup Flow**:

1. Load configuration from `.mcpproxy.json`
2. Start TCP listener on configured address
3. Start stdio handler for MCP protocol
4. Wait for app connections and agent requests

**Message Flow**:

```
Agent → Proxy (stdio):
  {"jsonrpc":"2.0","method":"tools/list","id":1}

Proxy → Agent (stdio):
  {"jsonrpc":"2.0","result":{"tools":[...]},"id":1}

Agent → Proxy (stdio):
  {"jsonrpc":"2.0","method":"tools/call","params":{"name":"WatchTower:ClickElement","arguments":{"x":100,"y":50}},"id":2}

Proxy → App (TCP):
  {"tool":"ClickElement","parameters":{"x":100,"y":50}}

App → Proxy (TCP):
  {"success":true,"data":{"x":100,"y":50,"clicked":true}}

Proxy → Agent (stdio):
  {"jsonrpc":"2.0","result":{"content":[{"type":"text","text":"..."}]},"id":2}
```

### 3. Client Applications (WatchTower)

**Purpose**: Avalonia applications that embed the MCP handler for agent interaction.

**Integration**:

- Add `Avalonia.Mcp.Core` package reference
- Register MCP handler in DI container
- Optionally register custom domain-specific tools
- Run application (auto-connects to proxy if configured)

## Tool Namespacing

Tools are automatically namespaced by application name to avoid conflicts:

- Raw tool name: `ClickElement`
- Namespaced name: `WatchTower:ClickElement`

This allows multiple apps to expose tools with the same name without conflicts.

Example tool catalog from proxy:

```json
{
  "tools": [
    {"name": "WatchTower:ClickElement", "description": "..."},
    {"name": "WatchTower:TypeText", "description": "..."},
    {"name": "WatchTower:CaptureScreenshot", "description": "..."},
    {"name": "AdminTool:ClickElement", "description": "..."},
    {"name": "AdminTool:ResetDatabase", "description": "..."}
  ]
}
```

## Connection Lifecycle

### App Startup

1. App starts and initializes services (including MCP handler)
2. If `AutoConnect: true`, handler immediately attempts connection
3. Handler creates TCP client and connects to proxy endpoint
4. Handler sends registration message with app name and tools
5. Proxy adds app to registry and acknowledges registration
6. Connection remains open for bidirectional communication

### App Shutdown

1. App shutdown triggered (user closes, Ctrl+C, etc.)
2. MCP handler Dispose() called
3. Handler disconnects TCP connection
4. Proxy detects disconnection and marks app as offline
5. Proxy removes app's tools from aggregated catalog

### Reconnection

1. If connection lost, handler attempts reconnect (if `AutoConnect: true`)
2. Reconnection interval controlled by `ReconnectIntervalMs` (default: 5000ms)
3. On successful reconnect, re-sends registration message
4. Proxy treats as new connection and re-adds tools

## Security Model

**Current Implementation**: Simple, trust-based security suitable for local development.

- **Proxy**: Listens only on localhost (not exposed to network)
- **Apps**: Connect only to localhost proxy
- **No Authentication**: Trust all apps on localhost
- **No Authorization**: All tools exposed to agents

**Future Enhancements** (not in current scope):

- API key authentication for app connections
- Role-based access control for tool execution
- TLS/SSL for encrypted communication
- Network-exposed proxy with proper security

## Configuration

### Proxy Configuration (.mcpproxy.json)

```json
{
  "Proxy": {
    "BindAddress": "localhost:5100",
    "LogLevel": "Information",
    "MaxConnections": 50,
    "Apps": [
      {
        "Name": "WatchTower",
        "Endpoint": "tcp://localhost:5000",
        "Description": "Main application"
      }
    ]
  }
}
```

### App Configuration (In-Code)

```csharp
services.AddMcpHandler(config =>
{
    config.ApplicationName = "WatchTower";
    config.ProxyEndpoint = "tcp://localhost:5100";  // Connect TO proxy
    config.AutoConnect = true;
    config.ReconnectIntervalMs = 5000;
    config.HeadlessMode = false;
}, registerStandardTools: true);
```

## Deployment Scenarios

### Local Development (Current)

- Proxy runs on localhost:5100
- Apps connect to localhost:5100
- Agent connects via stdio to proxy
- All components on same machine

### Multi-Machine Development (Future)

- Proxy runs on dedicated server (e.g., dev-server:5100)
- Apps connect from different machines
- Requires network security (authentication, TLS)

### CI/CD (Future)

- Proxy and apps run in containers
- Headless mode for all apps
- Automated testing via agent scripts

## Extension Points

### Custom Tools

Apps can register custom domain-specific tools:

```csharp
handler.RegisterTool(
    new McpToolDefinition
    {
        Name = "ExecuteQuery",
        Description = "Execute a database query",
        InputSchema = new
        {
            type = "object",
            properties = new
            {
                query = new { type = "string" }
            }
        }
    },
    async (parameters) =>
    {
        var query = parameters["query"].ToString();
        var result = await database.ExecuteAsync(query);
        return McpToolResult.Ok(result);
    }
);
```

### Custom Transports

Implement `ITransportClient` for new transport protocols:

```csharp
public class HttpSseTransportClient : ITransportClient
{
    // Implementation for HTTP/SSE transport
}
```

### Custom MCP Handler

Extend or replace `McpHandler` for specialized behavior:

```csharp
public class CustomMcpHandler : McpHandler
{
    // Override methods for custom behavior
}
```

## Performance Considerations

- **TCP Connections**: Persistent, low overhead
- **Message Serialization**: JSON (text-based, human-readable)
- **Tool Execution**: Async/await throughout
- **Concurrency**: Multi-threaded, handles multiple apps and requests

**Typical Latencies**:

- Tool discovery: < 10ms
- Tool execution: < 100ms (depends on tool complexity)
- Screenshot capture: < 200ms

## Observability

### Logging

All components use `Microsoft.Extensions.Logging`:

- **Core Library**: Logs tool registration, execution, connection events
- **Proxy**: Logs app registration, tool routing, agent requests
- **Apps**: Log MCP handler lifecycle events

### Monitoring (Future)

- Tool execution metrics (count, latency, success rate)
- App connection status (online, offline, reconnecting)
- Agent activity (requests per minute, popular tools)

## Limitations

**Current Version**:

- Localhost-only (no network exposure)
- No authentication/authorization
- TCP transport only (Named Pipes and HTTP/SSE are placeholders)
- Standard tools are stubs (need actual Avalonia input system integration)
- No tool execution timeout
- No request queueing or throttling

**Future Improvements**:

- Network-exposed proxy with security
- Complete transport implementations
- Full Avalonia input system integration
- Request rate limiting
- Tool execution monitoring and analytics

## License

MIT License - Open source, reusable by any team with Avalonia applications.
