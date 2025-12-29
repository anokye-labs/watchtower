# Avalonia.McpProxy

Standalone MCP (Model Context Protocol) proxy server that federates multiple Avalonia application handlers, enabling AI agents to interact with any Avalonia applications through a single unified interface.

## Features

- **Multi-App Federation**: Connect multiple Avalonia apps simultaneously
- **Tool Aggregation**: Automatically aggregates tools from all connected apps
- **Namespace Isolation**: Tools are namespaced by app name (e.g., `WatchTower:ClickElement`)
- **Live Discovery**: Apps can connect/disconnect dynamically without proxy restart
- **MCP Protocol**: Exposes standard MCP interface via stdio for agent communication
- **TCP Transport**: Accepts connections from Avalonia apps via TCP
- **Configuration File**: Simple JSON configuration for expected apps
- **Logging & Observability**: Built-in logging for debugging and monitoring

## Installation

### As a .NET Tool (Recommended)

```bash
# Install globally
dotnet tool install -g Avalonia.McpProxy

# Run
avalonia-mcp-proxy --stdio --yes
```

### From Source

```bash
# Clone and build
git clone https://github.com/anokye-labs/watchtower
cd watchtower
dotnet build src/Avalonia.McpProxy/Avalonia.McpProxy.csproj

# Run
dotnet run --project src/Avalonia.McpProxy/Avalonia.McpProxy.csproj -- --stdio --yes
```

## Quick Start

### 1. Create Configuration File

Create `.mcpproxy.json` in your working directory:

```json
{
  "Proxy": {
    "BindAddress": "localhost:5100",
    "LogLevel": "Information",
    "MaxConnections": 50,
    "Apps": [
      {
        "Name": "MyApp",
        "Endpoint": "tcp://localhost:5000",
        "Description": "My Avalonia application"
      },
      {
        "Name": "AnotherApp",
        "Endpoint": "tcp://localhost:5001",
        "Description": "Another Avalonia app"
      }
    ]
  }
}
```

### 2. Start the Proxy

```bash
# Stdio mode (for MCP agent communication)
dotnet run --project src/Avalonia.McpProxy/Avalonia.McpProxy.csproj -- --stdio --yes

# With custom config
MCP_CONFIG=my-config.json dotnet run --project src/Avalonia.McpProxy/Avalonia.McpProxy.csproj -- --stdio
```

### 3. Start Your Avalonia Apps

Your Avalonia apps with embedded MCP handlers will automatically connect to the proxy when they start (if configured with `AutoConnect: true`).

### 4. Connect Your Agent

Configure your IDE or agent to use the proxy. Example VS Code configuration:

```json
{
  "mcpServers": {
    "avalonia-apps": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "src/Avalonia.McpProxy/Avalonia.McpProxy.csproj",
        "--",
        "--stdio",
        "--yes"
      ],
      "env": {
        "MCP_CONFIG": ".mcpproxy.json"
      }
    }
  }
}
```

## Architecture

```
┌─────────────────────────────────────────────┐
│ Agent Client (Claude, Copilot, etc.)        │
│ Sees: One MCP server, unified tool set      │
└────────────────────┬────────────────────────┘
                     │ MCP Protocol (stdio)
                     ▼
┌─────────────────────────────────────────────┐
│  Avalonia MCP Proxy (This Project)          │
│  • Federates tools from all apps            │
│  • Routes tool calls to correct app         │
│  • Maintains connections to handlers        │
│  • TCP Listener on localhost:5100           │
└────────────────────┬────────────────────────┘
         ┌───────────┼───────────────────┐
         ▼           ▼           ▼       ▼
     ┌────────┐   ┌────────┐   ┌────────┐ ┌────────┐
     │  App1   │  │  App2   │  │  App3   │ │ App N  │
     │ (TCP    │  │ (TCP    │  │ (TCP    │ │ (TCP   │
     │ 5000)   │  │ 5001)   │  │ 5002)   │ │ 500N)  │
     └────────┘  └────────┘  └────────┘ └────────┘
```

## Command Line Options

- `--stdio`: Run in stdio mode for MCP agent communication (required)
- `--yes`: Auto-accept all prompts (for non-interactive mode)
- `--config <path>`: Path to configuration file (default: `.mcpproxy.json`)

## Configuration

### Proxy Configuration

- **BindAddress**: Address and port for TCP listener (default: `localhost:5100`)
- **LogLevel**: Logging level - `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical` (default: `Information`)
- **MaxConnections**: Maximum concurrent app connections (default: `50`)
- **Apps**: List of expected applications (for documentation purposes)

### App Configuration

Each app in the `Apps` list has:

- **Name**: Application name (must match the name in the app's MCP handler config)
- **Endpoint**: Expected endpoint where the app will connect FROM (informational)
- **Description**: Human-readable description of the application

**Note**: The proxy listens on `BindAddress`, and apps connect TO the proxy. The `Endpoint` in app config is for documentation only.

## How It Works

### 1. Proxy Startup

1. Proxy starts and reads configuration from `.mcpproxy.json`
2. Opens TCP listener on configured `BindAddress` (e.g., `localhost:5100`)
3. Starts stdio handler for agent communication
4. Waits for app connections

### 2. App Registration

1. Avalonia app with embedded MCP handler starts
2. App connects to proxy via TCP (e.g., `tcp://localhost:5100`)
3. App sends registration message with its name and tool catalog
4. Proxy registers app and adds its tools to the aggregated catalog

### 3. Agent Interaction

1. Agent connects to proxy via stdio (MCP protocol)
2. Agent lists available tools → Proxy returns aggregated tools from all apps
3. Agent calls a tool (e.g., `WatchTower:ClickElement`)
4. Proxy routes the call to the correct app (WatchTower)
5. App executes tool and returns result
6. Proxy forwards result to agent

### 4. Live Updates

- Apps can connect/disconnect at any time
- Proxy automatically updates tool catalog
- Agents always see current available tools

## Logging

The proxy logs to console with configurable levels:

```bash
# Debug mode
dotnet run --project src/Avalonia.McpProxy/Avalonia.McpProxy.csproj -- --stdio --yes
# Edit .mcpproxy.json: "LogLevel": "Debug"

# Example output:
[12:34:56 INF] Avalonia MCP Proxy v1.0.0
[12:34:56 INF] TCP listener started on localhost:5100
[12:34:57 INF] Accepted connection: abc123-...
[12:34:57 INF] Registered app 'WatchTower' with 6 tools
[12:34:58 INF] Received agent message: {"method":"tools/list",...}
```

## Troubleshooting

### Apps Won't Connect

1. Check that proxy is running and listening on correct port
2. Verify app's `ProxyEndpoint` matches proxy's `BindAddress`
3. Check firewall settings (allow localhost connections)
4. Enable debug logging: `"LogLevel": "Debug"` in `.mcpproxy.json`

### Tools Not Appearing

1. Check app registration logs in proxy output
2. Verify app sent tools in registration message
3. Use `tools/list` to query proxy directly

### Connection Lost

1. Proxy automatically marks app as disconnected
2. App will auto-reconnect if `AutoConnect: true`
3. Check logs for connection errors

## Development

### Build

```bash
dotnet build src/Avalonia.McpProxy/Avalonia.McpProxy.csproj
```

### Run in Development

```bash
dotnet run --project src/Avalonia.McpProxy/Avalonia.McpProxy.csproj -- --stdio --yes
```

### Package as Tool

```bash
dotnet pack src/Avalonia.McpProxy/Avalonia.McpProxy.csproj -c Release
dotnet tool install -g --add-source ./nupkg Avalonia.McpProxy
```

## License

MIT License - see LICENSE file for details.

## Contributing

Contributions are welcome! Please see CONTRIBUTING.md for guidelines.

## Support

- **Issues**: https://github.com/anokye-labs/watchtower/issues
- **Discussions**: https://github.com/anokye-labs/watchtower/discussions
