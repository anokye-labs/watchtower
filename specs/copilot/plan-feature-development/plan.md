# Implementation Plan: Federated Avalonia MCP Proxy Platform

**Branch**: `002-federated-mcp-proxy` | **Date**: 2025-11-29 | **Spec**: [spec.md](../002-federated-mcp-proxy/spec.md)
**Input**: Feature specification from `/specs/002-federated-mcp-proxy/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Build a reusable, open-source MCP Proxy that aggregates embedded MCP handlers from multiple Avalonia applications, enabling agents to iteratively develop and test any Avalonia app through a single unified interface. The system consists of two main components: an **Embedded MCP Library** that Avalonia apps integrate for exposing UI tools, and an **MCP Proxy Server** that federates tools from multiple apps into a single MCP endpoint.

## Technical Context

**Language/Version**: C# / .NET 10 (Avalonia 11.3.9)  
**Primary Dependencies**: Avalonia UI, Microsoft.Extensions.* (DI, Logging, Configuration), System.Text.Json, System.IO.Pipelines  
**Storage**: JSON configuration files (appsettings.json) for proxy endpoints and shared secrets  
**Testing**: xUnit for unit/integration tests, Avalonia.Headless for UI testing  
**Target Platform**: Cross-platform desktop (Windows x64, macOS x64, Linux x64)  
**Project Type**: Multi-project solution (embedded library + proxy server + WatchTower integration)  
**Performance Goals**: Proxy ready in <2s, tool execution <100ms p95 (excluding screenshots), screenshot capture <200ms, app registration <1s  
**Constraints**: No database dependency, localhost-first development, headless mode support for CI/CD  
**Scale/Scope**: 3+ concurrent apps, unlimited tool calls with graceful degradation, single-developer to team workflows

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The project constitution (`.specify/memory/constitution.md`) is currently a template. Applying **WatchTower-specific MVVM principles** from AGENTS.md:

| Principle | Status | Notes |
|-----------|--------|-------|
| **MVVM Architecture** | ✅ Pass | Embedded handler will be a service injected into ViewModels; no UI logic in Views |
| **Service Layer** | ✅ Pass | MCP tools exposed via service interfaces; ViewModels orchestrate services |
| **Test-First Development** | ⚠️ Requires enforcement | Tests written before implementation per constitution |
| **Dependency Injection** | ✅ Pass | All dependencies injected via DI container |
| **Cross-Platform Native** | ✅ Pass | Windows/macOS/Linux equal support via Avalonia |
| **Latest Avalonia** | ✅ Pass | Using Avalonia 11.3.9 |

**Gate Result**: PASS - No constitution violations detected

## Project Structure

### Documentation (this feature)

```text
specs/002-federated-mcp-proxy/
├── spec.md              # Feature specification (existing)
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   └── mcp-tools.json   # MCP tool definitions schema
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
# Multi-project solution structure
WatchTower/                           # Existing Avalonia application
├── Views/
├── ViewModels/
├── Services/
│   ├── LoggingService.cs             # Existing
│   └── McpEmbeddedHandler/           # New: MCP handler integration
│       └── WatchTowerMcpHandler.cs   # App-specific MCP tool registration
└── Models/

Avalonia.Mcp/                         # New: Reusable embedded MCP library
├── Services/
│   ├── IMcpEmbeddedService.cs        # Core service interface
│   ├── McpEmbeddedService.cs         # Embedded handler implementation
│   ├── IToolRegistry.cs              # Tool registration interface
│   └── ToolRegistry.cs               # Standard + custom tool management
├── Tools/
│   ├── IUiTool.cs                    # Base UI tool interface
│   ├── ClickElementTool.cs           # FR-002: Click interaction
│   ├── TypeTextTool.cs               # FR-002: Text input
│   ├── CaptureScreenshotTool.cs      # FR-002: Screenshot capture
│   ├── GetElementTreeTool.cs         # FR-002: UI inspection
│   ├── FindElementTool.cs            # FR-002: Element location
│   └── WaitForElementTool.cs         # FR-002: Element wait
├── Transport/
│   ├── ITransportConnection.cs       # FR-015: Transport abstraction
│   ├── TcpTransportConnection.cs     # FR-012: TCP transport
│   ├── NamedPipeTransportConnection.cs # FR-013: Named pipes (Windows)
│   └── HttpSseTransportConnection.cs # FR-014: HTTP/SSE transport
├── Protocol/
│   ├── McpMessage.cs                 # MCP protocol messages
│   ├── ToolDefinition.cs             # Tool schema definitions
│   └── ToolResult.cs                 # Tool execution results
└── Models/
    ├── ElementInfo.cs                # UI element representation
    └── ElementPath.cs                # FR-031-033: Hierarchical path

Avalonia.Mcp.Proxy/                   # New: MCP Proxy Server
├── Services/
│   ├── IMcpProxyService.cs           # Proxy service interface
│   ├── McpProxyService.cs            # FR-006-010: Tool aggregation & routing
│   ├── IApplicationRegistry.cs       # App registration interface
│   ├── ApplicationRegistry.cs        # FR-017-018: App lifecycle via process discovery
│   ├── IToolCatalog.cs               # Aggregated tools interface
│   └── ToolCatalog.cs                # FR-007: Namespaced tool catalog
├── Discovery/
│   ├── IProcessScanner.cs            # Process environment variable scanner
│   └── ProcessScanner.cs             # Discovers apps via AVALONIA_MCP_ENDPOINT env var
├── Transport/
│   ├── StdioMcpServer.cs             # FR-011: Agent-facing stdio interface
│   └── AppConnectionManager.cs       # App connection management
├── Security/
│   ├── IAuthenticationService.cs     # FR-022-025: Authentication
│   └── SharedSecretAuthService.cs    # Token-based auth
├── Configuration/
│   └── ProxyConfiguration.cs         # FR-016: Config file loading
└── Program.cs                        # Proxy entry point

Avalonia.Mcp.Tests/                   # Test projects
├── Unit/
│   ├── Embedded/
│   └── Proxy/
├── Integration/
│   ├── EmbeddedToProxyTests.cs
│   └── EndToEndToolTests.cs
└── Contract/
    └── McpProtocolTests.cs
```

**Structure Decision**: Multi-project solution following .NET conventions. The embedded library (`Avalonia.Mcp`) is a reusable NuGet-style package that any Avalonia app can reference. The proxy (`Avalonia.Mcp.Proxy`) is a standalone console application that discovers running apps by scanning process environment variables. WatchTower serves as the first integration target per User Story 0.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| 3 new projects | Federation requires separation of concerns | Single project would couple proxy logic with app logic, preventing reuse |
| Transport abstraction | FR-012-015 require multiple protocols | Single protocol insufficient for diverse deployment scenarios (localhost TCP vs Windows IPC vs remote HTTP) |

---

## Post-Design Constitution Check

*Re-evaluated after Phase 1 design completion.*

| Principle | Status | Design Validation |
|-----------|--------|-------------------|
| **MVVM Architecture** | ✅ Pass | `IMcpEmbeddedService` is a service injected into app lifecycle; tools operate on accessibility tree without touching Views |
| **Service Layer** | ✅ Pass | All MCP functionality in `Services/` directories; clear interfaces (`IMcpEmbeddedService`, `IMcpProxyService`, `IToolRegistry`) |
| **Test-First Development** | ✅ Designed for TDD | `WatchTower.Mcp.Tests/` structure with Unit, Integration, Contract folders enables TDD workflow |
| **Dependency Injection** | ✅ Pass | `services.AddMcpEmbeddedHandler()` extension method follows .NET DI patterns |
| **Cross-Platform Native** | ✅ Pass | Transport abstraction supports platform-specific optimizations (Named Pipes on Windows); Headless mode for all platforms |
| **Latest Avalonia** | ✅ Pass | Design uses Avalonia 11.3.9 `AutomationPeer` API and `RenderTargetBitmap` |

**Post-Design Gate Result**: PASS - Design complies with all architectural principles

---

## Generated Artifacts

| Artifact | Location | Description |
|----------|----------|-------------|
| plan.md | `specs/copilot/plan-feature-development/plan.md` | This implementation plan |
| research.md | `specs/copilot/plan-feature-development/research.md` | Technical research and decisions |
| data-model.md | `specs/copilot/plan-feature-development/data-model.md` | Entity definitions and relationships |
| mcp-tools.json | `specs/copilot/plan-feature-development/contracts/mcp-tools.json` | API schema for MCP tools |
| quickstart.md | `specs/copilot/plan-feature-development/quickstart.md` | Integration guide |
| copilot-instructions.md | `.github/agents/copilot-instructions.md` | Updated agent context |

---

## Next Steps

1. **Tasks Generation**: Run `/speckit.tasks` to generate `tasks.md` with implementation tasks
2. **Implementation**: Use `speckit.implement` agent to execute tasks in dependency order
3. **Validation**: Run checklists from `specs/002-federated-mcp-proxy/checklists/` to verify implementation
