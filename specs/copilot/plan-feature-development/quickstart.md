# Quickstart: Federated Avalonia MCP Proxy Platform

**Feature Branch**: `002-federated-mcp-proxy`  
**Date**: 2025-11-29

## Overview

This guide explains how to integrate the MCP embedded handler into an Avalonia application and connect it to the federated MCP proxy.

---

## Prerequisites

- .NET 10 SDK
- Avalonia 11.3.9 or later
- An existing Avalonia application (e.g., WatchTower)

---

## Quick Integration (< 10 lines of code per SC-012)

### 1. Add Package Reference

```xml
<!-- In your .csproj file -->
<PackageReference Include="Avalonia.Mcp" Version="1.0.0" />
```

### 2. Configure appsettings.json

```json
{
  "McpEmbedded": {
    "AppId": "WatchTower",
    "SharedSecret": "YOUR_SHARED_SECRET_BASE64",
    "EnableHeadless": true,
    "AdvertiseEndpoint": true
  }
}
```

> **Security Note**: Generate a cryptographically secure secret using:
> ```bash
> # Linux/macOS
> openssl rand -base64 32
> # PowerShell
> [Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))
> ```
> Store secrets securely - consider using environment variables or a secrets manager in production.

### 3. Register in App.axaml.cs

```csharp
using Avalonia.Mcp;

public override void OnFrameworkInitializationCompleted()
{
    var services = new ServiceCollection();
    
    // Add MCP embedded handler (FR-001)
    services.AddMcpEmbeddedHandler(Configuration);
    
    // Build service provider
    var provider = services.BuildServiceProvider();
    
    // Start MCP handler - sets AVALONIA_MCP_ENDPOINT env var for proxy discovery
    provider.GetRequiredService<IMcpEmbeddedService>().StartAsync();
    
    base.OnFrameworkInitializationCompleted();
}
```

**That's it!** Your Avalonia app now exposes standard MCP tools and will be automatically discovered by the proxy.

---

## Standard Tools (FR-002)

Once integrated, these tools are automatically available:

| Tool | Description | Example Call |
|------|-------------|--------------|
| `ClickElement` | Click at coordinates | `{"x": 100, "y": 50}` |
| `TypeText` | Type text into focused element | `{"text": "hello@example.com"}` |
| `CaptureScreenshot` | Capture window screenshot | `{"format": "png"}` |
| `GetElementTree` | Get UI element hierarchy | `{"maxDepth": 5}` |
| `FindElement` | Find element by path/name | `{"path": "MainWindow/Button[0]"}` |
| `WaitForElement` | Wait for element visibility | `{"name": "LoginButton", "timeout": 5000}` |

---

## Adding Custom Tools (FR-003)

Register application-specific tools:

```csharp
public class WatchTowerMcpHandler
{
    // Static schema - parsed once at class initialization for efficiency
    private static readonly JsonElement ResetAppStateSchema = 
        JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new { },
            required = Array.Empty<string>()
        });

    public WatchTowerMcpHandler(IMcpEmbeddedService mcpService)
    {
        // Register custom tool (FR-003)
        mcpService.RegisterTool(new ToolDefinition
        {
            Name = "ResetAppState",
            Description = "Reset WatchTower to initial state",
            InputSchema = ResetAppStateSchema
        }, async (args, ct) =>
        {
            // Your reset logic here
            await ResetApplicationStateAsync();
            return new ToolResult
            {
                Content = new[] { new TextContent { Text = "App state reset successfully" } }
            };
        });
    }
}
```

---

## Running the Proxy

### Start the MCP Proxy Server

```bash
# From solution root
dotnet run --project Avalonia.Mcp.Proxy

# Or use published executable
./Avalonia.Mcp.Proxy
```

The proxy automatically discovers running Avalonia apps by scanning process environment variables 
for `AVALONIA_MCP_ENDPOINT`.

### Proxy Configuration (appsettings.json)

```json
{
  "McpProxy": {
    "SharedSecret": "YOUR_SHARED_SECRET_BASE64",
    "TokenExpiry": "00:30:00",
    "ToolTimeout": "00:00:30",
    "DiscoveryScanInterval": "00:00:05"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

---

## Connecting AI Agents

### Claude Desktop Configuration

Add to `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "avalonia-mcp-proxy": {
      "command": "/path/to/Avalonia.Mcp.Proxy",
      "args": [],
      "env": {
        "MCP_AUTH_TOKEN": "YOUR_AUTH_TOKEN"
      }
    }
  }
}
```

### VS Code / GitHub Copilot

Configure MCP server in VS Code settings:

```json
{
  "mcp.servers": {
    "avalonia-mcp-proxy": {
      "command": "/path/to/Avalonia.Mcp.Proxy",
      "args": []
    }
  }
}
```

---

## Usage Example: Agent Interaction

Once connected, an agent can interact with your app:

```text
Agent: I'll inspect the current UI state.

[Calls WatchTower:GetElementTree]
→ Returns hierarchical view of all UI elements

Agent: I see a button at path "MainWindow/Panel[0]/SubmitButton[0]". 
       Let me click it.

[Calls WatchTower:ClickElement {"x": 200, "y": 150}]
→ Button clicked, action triggered

Agent: I'll capture a screenshot to verify the result.

[Calls WatchTower:CaptureScreenshot {"format": "png"}]
→ Returns base64-encoded PNG image
```

---

## Headless Mode (FR-004, FR-005)

For CI/CD environments without displays:

```bash
# Set headless environment
export AVALONIA_HEADLESS=1

# Run your app - screenshots still work via SkiaSharp rendering
dotnet run --project WatchTower
```

Or programmatically:

```csharp
AppBuilder.Configure<App>()
    .UseHeadless(new AvaloniaHeadlessPlatformOptions
    {
        UseHeadlessDrawing = true
    })
    .StartWithClassicDesktopLifetime(args);
```

---

## Multiple Apps (User Story 1)

The proxy automatically discovers multiple apps running with `AVALONIA_MCP_ENDPOINT` env var:

```text
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   WatchTower    │     │    AdminTool    │     │   DataService   │
│  (AppId: WT)    │     │  (AppId: Admin) │     │  (AppId: Data)  │
│  AVALONIA_MCP_  │     │  AVALONIA_MCP_  │     │  AVALONIA_MCP_  │
│  ENDPOINT=...   │     │  ENDPOINT=...   │     │  ENDPOINT=...   │
└────────┬────────┘     └────────┬────────┘     └────────┬────────┘
         │                       │                       │
         │     Proxy scans process env vars              │
         └───────────────────────┼───────────────────────┘
                                 │
                        ┌────────┴────────┐
                        │    MCP Proxy    │
                        │ (discovers apps │
                        │  via env vars)  │
                        └────────┬────────┘
                                 │
                        ┌────────┴────────┐
                        │    AI Agent     │
                        │ (Claude, etc.)  │
                        └─────────────────┘

Agent sees tools:
- WT:ClickElement
- WT:GetElementTree
- Admin:ClickElement
- Admin:GetElementTree
- Data:ClickElement
- Data:GetElementTree
```

---

## Troubleshooting

### App Not Being Discovered

1. Check app has `AdvertiseEndpoint: true` in config
2. Verify `AVALONIA_MCP_ENDPOINT` env var is set in app process
3. Check proxy logs for discovery scan results
4. Verify shared secrets match in both configs

### Screenshots Return Empty

1. Ensure window is not minimized (GUI mode)
2. For headless: verify `UseHeadlessDrawing = true`
3. Check Avalonia.Skia package is referenced

### Tool Calls Timeout

1. Default timeout is 30 seconds (FR-021)
2. Adjust `ToolTimeout` in proxy config
3. Check app logs for errors during tool execution

### Duplicate App ID Error

1. Each app must have unique `AppId`
2. Use versioned IDs: `WatchTower-Dev`, `WatchTower-v1.2.0`

---

## Next Steps

- See [data-model.md](./data-model.md) for entity definitions
- See [contracts/mcp-tools.json](./contracts/mcp-tools.json) for complete API schema
- See [research.md](./research.md) for technical decisions

---

## Success Criteria Checklist

| Criteria | How to Verify |
|----------|---------------|
| SC-001: 3+ apps through single connection | Connect 3 apps, list tools from agent |
| SC-002: Correct tool routing | Call namespaced tool, verify correct app responds |
| SC-003: Dynamic connect/disconnect | Restart app, verify tool list updates |
| SC-004: Headless screenshots | Run in CI, capture screenshot |
| SC-005: Proxy startup < 2s | Time from launch to first tool call |
| SC-006: Tool execution < 100ms p95 | Benchmark tool calls (excluding screenshots) |
| SC-007: Screenshot < 200ms | Time CaptureScreenshot calls |
| SC-008: App registration < 1s | Time from app start to tools visible |
| SC-009: CI/CD headless | Run full workflow in GitHub Actions |
| SC-010: Full inspect-interact-verify | Complete loop without manual intervention |
| SC-011: Restart without reconfigure | Restart app during agent session |
| SC-012: Integration < 10 LOC | Count lines in integration guide above |
| SC-013: Zero app-specific proxy config | Proxy works with any app self-describing tools |
