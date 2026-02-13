# Avalonia MCP Proxy Platform - Architecture

## Overview

The Avalonia MCP Proxy Platform enables AI agents (Claude, GitHub Copilot, etc.) to interact with Avalonia applications through the Model Context Protocol (MCP). The platform consists of three main components:

1. **Avalonia.Mcp.Core** - Reusable library for embedding diagnostic listeners in Avalonia apps
2. **Avalonia.McpProxy** - System tray proxy app that federates multiple app handlers
3. **Client Apps** (e.g., WatchTower) - Avalonia applications using the core library

## System Architecture

```
+-------------------------------------------------------------------+
|                    AI Agent (Claude, Copilot)                      |
|                                                                    |
|  Capabilities:                                                     |
|  - Discover tools via MCP protocol                                 |
|  - Execute tools on any connected Avalonia app                     |
|  - Launch apps with diagnostic support                             |
|  - Receive real-time feedback                                      |
+-----------------------------+--------------------------------------+
                              |
                              | MCP Protocol (stdio)
                              | JSON-RPC 2.0
                              v
+-------------------------------------------------------------------+
|         Avalonia.McpProxy (System Tray Application)                |
|                                                                    |
|  +------------------+  +---------------------+  +----------------+ |
|  | McpProxyBridge   |  | AppConnectionManager|  | ProcessManager | |
|  |                  |  |                     |  |                | |
|  | - MCP Protocol   |  | - Track Connections |  | - Launch Apps  | |
|  | - List Tools     |  | - Persist State     |  | - Capture Port | |
|  | - Call Tools     |  | - Reconnect         |  | - Register App | |
|  +------------------+  +---------------------+  +----------------+ |
|                                                                    |
|  +------------------------------------------------------------+   |
|  |  System Tray Icon + Log Window (ProxyViewModel)             |   |
|  |  - Show connected apps                                      |   |
|  |  - View logs                                                |   |
|  |  - Reconnect all                                            |   |
|  +------------------------------------------------------------+   |
+-------+----------------+----------------+-------------------------+
        |                |                |
        | TCP            | TCP            | TCP
        | (proxy         | (proxy         | (proxy
        |  connects      |  connects      |  connects
        |  TO app)       |  TO app)       |  TO app)
        v                v                v
+----------------+  +------------+  +--------------+
|  WatchTower    |  |  App 2     |  |  App N       |
|                |  |            |  |              |
|  Diagnostic    |  |  Diagnostic|  |  Diagnostic  |
|  Listener      |  |  Listener  |  |  Listener    |
|  (TCP Server)  |  |  (TCP Srv) |  |  (TCP Srv)   |
|                |  |            |  |              |
|  - Standard    |  |  - Standard|  |  - Standard  |
|    Tools       |  |    Tools   |  |    Tools     |
|  - Custom      |  |  - Custom  |  |  - Custom    |
|    Tools       |  |    Tools   |  |    Tools     |
+----------------+  +------------+  +--------------+
```

### Connection Model (Inverted)

The proxy connects TO apps, not the other way around. Each app embeds a `DiagnosticListener` that listens on a random TCP port and announces it via stdout (`DIAGNOSTIC_PORT:NNNN`). The proxy discovers this port and initiates the connection.

This inversion simplifies app integration: apps just start a listener, and the proxy handles discovery and connection management.

## Component Details

### 1. Avalonia.Mcp.Core

**Purpose**: Embeddable library that provides diagnostic and MCP capabilities to any Avalonia application.

**Key Classes**:

- **DiagnosticListener** (`Diagnostics/`): TCP server embedded in apps
  - Listens on a random available port
  - Accepts proxy connections
  - Handles handshake, tool invocation, and shutdown messages
  - Defines `DiagnosticTool` and `DiagnosticResult` types

- **StandardUiTools**: Pre-built UI interaction tools
  - ClickElement(x, y)
  - TypeText(text)
  - CaptureScreenshot(format)
  - GetElementTree(maxDepth)
  - FindElement(selector)
  - WaitForElement(selector, timeoutMs)

- **IAvaloniaUiService / AvaloniaUiService**: UI interaction service
  - Click, type, screenshot, element tree inspection
  - Element search and wait

- **Models**: Shared types
  - `McpToolDefinition`: Tool name, description, input schema
  - `McpToolResult`: Success/failure with data/error
  - `McpToolInvocation`: Tool invocation request

**Legacy Components** (from earlier architecture, may be removed):
- `IMcpHandler / McpHandler`: MCP handler with auto-reconnect
- `ITransportClient / TcpTransportClient`: TCP transport layer
- `TransportClientFactory`: Transport creation (tcp:// supported, pipe:// not yet)
- `ServiceCollectionExtensions`: DI registration via `AddMcpHandler()`

### 2. Avalonia.McpProxy

**Purpose**: System tray application that aggregates multiple app handlers and exposes a unified MCP interface to AI agents.

**Key Classes**:

- **McpProxyBridge** (`Services/`): MCP stdio protocol bridge
  - Reads JSON-RPC from stdin, writes responses to stdout
  - Handles `initialize`, `tools/list`, `tools/call`
  - Exposes proxy management tools: `launch_app`, `list_apps`, `stop_app`
  - Federates tools from all connected apps with `AppName:tool` namespacing

- **AppConnectionManager** (`Services/`): Connection hub
  - Connects TO apps via TCP (inverted model)
  - Persists app state to `%LOCALAPPDATA%/AvaloniaProxy/apps.json`
  - Handles handshake, tool invocation routing, disconnection monitoring
  - Reconnects to known apps on startup

- **ProcessManager** (`Services/`): App launcher
  - Launches Avalonia apps with diagnostic support
  - Captures `DIAGNOSTIC_PORT:NNNN` from app stdout
  - Registers discovered apps with AppConnectionManager

- **ProxyViewModel** (`ViewModels/`): MVVM ViewModel
  - Observable collection of connected apps
  - Log buffer with 50KB cap
  - Clear and reconnect commands

- **App.axaml.cs**: Application entry point
  - System tray icon with context menu (Show Logs, Reconnect All, Exit)
  - `ShutdownMode.OnExplicitShutdown` (runs without main window)
  - Log window shown on demand

**Startup Flow**:

1. Avalonia app starts with system tray icon
2. ProxyViewModel created, starts AppConnectionManager
3. AppConnectionManager loads persisted app state and reconnects
4. McpProxyBridge starts reading MCP requests from stdin
5. Agents can also launch apps via `launch_app` tool

**Message Flow**:

```
Agent -> Proxy (stdio):
  {"jsonrpc":"2.0","method":"tools/list","id":1}

Proxy -> Agent (stdio):
  {"jsonrpc":"2.0","result":{"tools":[...]},"id":1}

Agent -> Proxy (stdio):
  {"jsonrpc":"2.0","method":"tools/call","params":{"name":"WatchTower:ClickElement","arguments":{"x":100,"y":50}},"id":2}

Proxy -> App (TCP, line-delimited JSON):
  {"type":"toolInvocation","correlationId":1,"tool":"ClickElement","parameters":{"x":100,"y":50}}

App -> Proxy (TCP):
  {"correlationId":1,"result":{"success":true,"data":"{...}"}}

Proxy -> Agent (stdio):
  {"jsonrpc":"2.0","result":{"content":[{"type":"text","text":"..."}]},"id":2}
```

### 3. Client Applications (WatchTower)

**Purpose**: Avalonia applications that embed a DiagnosticListener for agent interaction.

**Integration**:

- Add `Avalonia.Mcp.Core` project reference
- Start a DiagnosticListener on app startup
- Register standard and custom tools with the listener
- Print `DIAGNOSTIC_PORT:NNNN` to stdout so the proxy can discover it

## Tool Namespacing

Tools are automatically namespaced by application name to avoid conflicts:

- Raw tool name: `ClickElement`
- Namespaced name: `WatchTower:ClickElement`

This allows multiple apps to expose tools with the same name without conflicts.

## Proxy Management Tools

The proxy itself exposes three management tools to agents:

| Tool | Description |
|------|-------------|
| `launch_app` | Launch an Avalonia app with diagnostic support |
| `list_apps` | List all connected apps and their tools |
| `stop_app` | Send shutdown signal to an app by port |

## Security Model

**Current Implementation**: Simple, trust-based security suitable for local development.

- **Proxy**: Connects only to localhost apps
- **Apps**: Listen only on localhost
- **No Authentication**: Trust all apps on localhost
- **No Authorization**: All tools exposed to agents

## Performance Considerations

- **TCP Connections**: Persistent, low overhead, line-delimited JSON
- **Message Serialization**: JSON (text-based, human-readable)
- **Tool Execution**: Async/await throughout
- **Concurrency**: Handles multiple apps and requests concurrently
- **Correlation IDs**: Match responses to requests for multiplexed communication

## Limitations

**Current Version**:

- Localhost-only (no network exposure)
- No authentication/authorization
- TCP transport only (Named Pipes placeholder in Core)
- Standard UI tools need deeper Avalonia input system integration
- No tool execution timeout
- No request queueing or throttling
- Dual communication patterns in Core (legacy McpHandler + new DiagnosticListener)

## License

MIT License - Open source, reusable by any team with Avalonia applications.
