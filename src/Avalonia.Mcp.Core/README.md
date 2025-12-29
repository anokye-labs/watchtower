# Avalonia.Mcp.Core

Embedded MCP (Model Context Protocol) handler library for Avalonia applications. This library enables AI agents (Claude, GitHub Copilot, etc.) to interact with Avalonia applications through a unified interface.

## Features

- **Standard UI Interaction Tools**: Click, Type, Screenshot, Element Tree inspection, Find Element, Wait for Element
- **Transport Abstraction**: TCP, Named Pipes (Windows), HTTP/SSE support
- **Headless & GUI Support**: Works in both headless and GUI modes
- **Easy Integration**: Simple DI extension methods for Avalonia apps
- **Tool Registration**: Register custom domain-specific tools
- **Namespace Isolation**: Tools are automatically namespaced by application name

## Installation

Add the package reference to your Avalonia application:

```bash
dotnet add package Avalonia.Mcp.Core
```

Or add it to your `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Avalonia.Mcp.Core" Version="1.0.0" />
</ItemGroup>
```

## Quick Start

### 1. Register MCP Handler in Your App

In your `App.axaml.cs` or startup configuration:

```csharp
using Avalonia.Mcp.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;

// In your service registration code:
services.AddMcpHandler(config =>
{
    config.ApplicationName = "MyAvaloniaApp";
    config.ProxyEndpoint = "tcp://localhost:5000";
    config.AutoConnect = true;
    config.HeadlessMode = false;
}, registerStandardTools: true);
```

### 2. Run the MCP Proxy

The MCP proxy must be running to federate your app's tools to agents:

```bash
dotnet run --project Avalonia.McpProxy -- --stdio --yes
```

### 3. Connect Your Agent

Configure your IDE or agent to use the MCP proxy. See `mcp.json.example` for VS Code configuration.

## Standard Tools

The following standard tools are registered automatically when `registerStandardTools: true`:

- **ClickElement(x, y)**: Click at specified coordinates
- **TypeText(text)**: Type text into focused element
- **CaptureScreenshot(format)**: Capture application screenshot (PNG/JPG)
- **GetElementTree(maxDepth)**: Get UI element tree structure
- **FindElement(selector)**: Find element by name, type, or automation ID
- **WaitForElement(selector, timeoutMs)**: Wait for element to appear

## Custom Tools

You can register custom domain-specific tools:

```csharp
var handler = serviceProvider.GetRequiredService<IMcpHandler>();

handler.RegisterTool(
    new McpToolDefinition
    {
        Name = "ResetAppState",
        Description = "Reset the application to its initial state",
        InputSchema = new { type = "object", properties = new { } }
    },
    async (parameters) =>
    {
        // Your implementation
        await ResetApplicationAsync();
        return McpToolResult.Ok(new { reset = true });
    }
);
```

## Architecture

```
┌─────────────────────────────────────────────┐
│ Your Avalonia Application                   │
│                                              │
│  ┌──────────────────────────────────────┐  │
│  │ Avalonia.Mcp.Core                     │  │
│  │ • IMcpHandler (embedded)              │  │
│  │ • Standard UI Tools                   │  │
│  │ • Custom Tools                        │  │
│  │ • TCP/Pipe Transport                  │  │
│  └──────────────┬───────────────────────┘  │
└─────────────────┼───────────────────────────┘
                  │ TCP Connection
                  ▼
┌─────────────────────────────────────────────┐
│ Avalonia.McpProxy (Standalone)              │
│ • Aggregates tools from all apps            │
│ • Routes tool calls                         │
│ • Exposes MCP interface to agents           │
└─────────────────┬───────────────────────────┘
                  │ stdio (MCP Protocol)
                  ▼
┌─────────────────────────────────────────────┐
│ AI Agent (Claude, Copilot, etc.)           │
└─────────────────────────────────────────────┘
```

## Configuration

### McpHandlerConfiguration

- **ApplicationName**: Unique name for your app (used in tool namespacing)
- **ProxyEndpoint**: Connection string to MCP proxy (`tcp://host:port` or `pipe://pipename`)
- **AutoConnect**: Whether to auto-connect on startup (default: `true`)
- **ReconnectIntervalMs**: Reconnection interval in milliseconds (default: `5000`)
- **HeadlessMode**: Whether to run in headless mode (default: `false`)

## Development

### Build

```bash
dotnet build src/Avalonia.Mcp.Core/Avalonia.Mcp.Core.csproj
```

### Test

```bash
dotnet test src/Avalonia.Mcp.Core.Tests/
```

## License

MIT License - see LICENSE file for details.

## Contributing

Contributions are welcome! Please see CONTRIBUTING.md for guidelines.

## Support

- **Issues**: https://github.com/anokye-labs/watchtower/issues
- **Discussions**: https://github.com/anokye-labs/watchtower/discussions
