<img src="https://r2cdn.perplexity.ai/pplx-full-logo-primary-dark%402x.png" style="height:64px;margin-right:32px"/>

# Federated Avalonia MCP Proxy Platform

## Ultra High-Level Specification


***

## 1. Vision \& Problem Statement

### Problem

Agents (Claude, GitHub Copilot, etc.) cannot reliably interact with Avalonia applications for iterative development and testing. Today's solutions require:

- Complex, app-specific MCP server setup
- Manual integration per application
- No standardized tool interface across different Avalonia apps
- Agents must context-switch between apps (multiple servers, multiple tool sets)
- Testing/development workflows are fragmented


### Vision

**Build a reusable, open-source MCP Proxy that aggregates embedded MCP handlers from multiple Avalonia applications, enabling agents to iteratively develop and test any Avalonia app through a single unified interface.**

The proxy is a **standalone, general-purpose tool** that any team can deploy to interact with any Avalonia applications in their environment. Watch-Tower is simply the **first client application** using this proxy.

**Result:** Agents can interact with Avalonia apps (any Avalonia apps) as naturally as they interact with APIs or filesystems—via unified, discoverable tools.

***

## 2. System Architecture (Conceptual)

```
┌─────────────────────────────────────────────┐
│ Agent Client (Claude, Copilot, etc.)        │
│ Sees: One MCP server, unified tool set      │
└────────────────────┬────────────────────────┘
                     │ MCP Protocol (stdio)
                     ▼
┌─────────────────────────────────────────────┐
│  Avalonia MCP Proxy (Standalone Project)    │
│  • Federates tools from all apps            │
│  • Routes tool calls to correct app         │
│  • Maintains connections to handlers        │
└────────────────────┬────────────────────────┘
         ┌───────────┼───────────────────┐
         ▼           ▼           ▼       ▼
     ┌────────┐   ┌────────┐   ┌────────┐ ┌────────┐
     │ Watch-  │  │  App 2  │  │  App 3  │ │ App N  │
     │ Tower   │  │         │  │        │ │        │
     │ Embedded   │ Embedded   │ Embedded│ │Embedded
     │ MCP     │  │ MCP     │  │ MCP    │ │MCP
     │ Handler │  │ Handler │  │ Handler │ │Handler
     └────────┘  └────────┘  └────────┘ └────────┘
```


***

## 3. Key Concepts

### 3.1 Embedded MCP Handler (Per App)

Each Avalonia application embeds an MCP handler that exposes tools for agent interaction:

**Core Standard Tools:**

- `ClickElement(x, y)` — interact via headless platform
- `TypeText(text)` — keyboard input
- `CaptureScreenshot()` — visual verification
- `GetElementTree()` — UI state inspection
- `FindElement(selector)` — locate UI elements
- `WaitForElement(selector, timeout)` — synchronization

**Custom Tools:**
Apps can define domain-specific tools for their own testing/development (e.g., `ResetAppState()`, `SetTestData(...)`)

**Key:** Handler is bundled with the app. No separate deployment.

### 3.2 MCP Proxy (Standalone Federation Layer)

Independent, reusable MCP server that:

1. **Discovers** running Avalonia apps with embedded handlers
2. **Aggregates** their tool catalogs
3. **Routes** tool calls to correct app
4. **Maintains** persistent connections to handlers
5. **Exposes** unified MCP interface to agents

Agent sees one connection. Proxy transparently routes to correct app.

**Key:** The proxy is NOT Watch-Tower-specific. Any team can deploy it to interact with their Avalonia apps.

### 3.3 Transport Abstraction

Apps connect to proxy via multiple possible transports:

- **TCP** (localhost:port) — local development
- **Named Pipes** (Windows-only) — inter-process, secure
- **HTTP/SSE** (Server-Sent Events) — for remote scenarios

Proxy handles transport selection transparently. Apps don't care how they connect.

***

## 4. Capabilities \& Requirements

### 4.1 Tool Discovery \& Routing

- **Agent asks proxy:** "What tools are available?"
- **Proxy responds:** Unified list from all connected apps (with app source tagged)
- **Agent calls tool:** "Execute `ClickElement` on `Watch-Tower`"
- **Proxy routes:** Sends to Watch-Tower's embedded handler, gets result, returns to agent


### 4.2 App Tagging \& Namespace Isolation

Each tool is scoped to its source app:

- `WatchTower:ClickElement`
- `WatchTower:TypeText`
- `WatchTower:GetElementTree`
- `OtherApp:ClickElement`
- `OtherApp:CustomBusinessLogic`

Agent knows which app each tool belongs to. No ambiguity.

### 4.3 Live App Discovery

- **Proxy monitors** for app connections/disconnections (real-time)
- **Apps register** when they start
- **Apps unregister** when they shut down
- **Proxy updates** tool list dynamically
- **Agent sees** current available tools immediately


### 4.4 Iterative Development Workflow

Agent can:

1. **Inspect** UI state (GetElementTree, screenshots)
2. **Interact** with UI (clicks, text input)
3. **Modify** code based on observed behavior
4. **Reload** app
5. **Re-inspect** to verify changes
6. **Repeat** → agent-driven development loop

Proxy enables the feedback loop.

### 4.5 Headless + GUI Support

- **Headless apps:** Use Avalonia headless platform (no display needed)
- **GUI apps:** Full desktop window interaction
- **Mixed scenarios:** Apps can run headless or GUI, proxy doesn't care
- **Screenshot capture:** Works in both modes (headless uses Skia renderer)

***

## 5. Operational Model

### 5.1 Startup Flow

1. **Proxy boots:** `dnx Avalonia.McpProxy --yes` (standalone, reusable tool)
2. **Proxy listens:** On configured transports (TCP, pipes, etc.)
3. **Apps start independently:** Each runs in their own process
4. **Apps register:** Each embedded handler connects to proxy (TCP/pipes)
5. **Proxy ready:** Reports aggregated tools to agent

**Key:** Apps and proxy are completely decoupled. Apps don't know about each other.

### 5.2 Runtime Interaction (Iterative Development)

**Scenario:** Agent is helping developer build Watch-Tower's UI

1. **Agent:** "Show me the current state of the main window"
2. **Proxy:** Calls `GetElementTree` on Watch-Tower handler
3. **Watch-Tower:** Returns UI tree (buttons, fields, etc.)
4. **Agent:** "I see a login button at (100, 50). Click it."
5. **Proxy:** Routes `ClickElement(100, 50)` to Watch-Tower
6. **Agent:** "Take a screenshot"
7. **Proxy:** Routes `CaptureScreenshot` to Watch-Tower, returns image
8. **Agent:** "I notice the button color is wrong. Let me modify the code..."
9. **Developer:** Edits XAML/C\#
10. **Developer:** Rebuilds and restarts Watch-Tower
11. **Proxy:** Detects reconnection, updates tool list
12. **Agent:** "Let me check the button color again..." → loops

***

## 6. Configuration \& Deployment

### 6.1 Proxy Configuration (`.mcpproxy.json`)

```yaml
Apps:
  - Name: WatchTower
    Endpoint: tcp://localhost:5000
    Description: Main Watch-Tower application
  
  - Name: AdminTool
    Endpoint: tcp://localhost:5001
    Description: Admin configuration tool
  
  - Name: DataService
    Endpoint: tcp://localhost:5002
    Description: Data processing service

Proxy:
  BindAddress: localhost:5100
  LogLevel: Information
  MaxConnections: 50
```

**Key:** App list is just for discovery. Apps connect when they're ready. No app-level profiles or capabilities defined here.

### 6.2 IDE Integration (VS Code `mcp.json`)

```json
{
  "servers": {
    "AvaloniaApps": {
      "type": "stdio",
      "command": "dnx",
      "args": ["Avalonia.McpProxy@1.0.0", "--stdio", "--yes"],
      "env": {
        "MCP_CONFIG": ".mcpproxy.json"
      }
    }
  }
}
```

**Result:** One line. Proxy discovers all configured apps and exposes their tools.

### 6.3 Watch-Tower as Client (Not Special)

Watch-Tower is an Avalonia app that:

- Embeds the MCP handler (like any other app)
- Runs in its own process
- Connects to proxy like any other app
- Is developed/tested via agent interaction through the proxy

**Key:** Watch-Tower is built WITH the proxy, not the other way around. The proxy is used to develop Watch-Tower.

***

## 7. Architecture Layers

### Layer 1: Core Library (`Avalonia.Mcp.Core`)

Shared by any Avalonia app that wants MCP capabilities:

- Embedded MCP handler base class
- Standard UI interaction tools (click, type, screenshot, etc.)
- DI extensions for easy integration
- Headless + GUI mode detection

**Used by:** Watch-Tower, any other Avalonia apps

### Layer 2: Proxy (`Avalonia.McpProxy`)

Standalone, general-purpose tool:

- Discovers and connects to app handlers
- Aggregates tool catalogs
- Routes tool calls
- Maintains live app status
- Exposes single MCP interface

**Used by:** Any team with Avalonia apps

### Layer 3: Application (Watch-Tower)

Example/primary application built with the core library:

- Uses embedded MCP handler for agent interaction
- Developed/tested via proxy
- Demonstrates iterative agent-driven development

***

## 8. Simplifications (What We're NOT Doing)

### NOT: Profile-Based Access Control

~~Proxy doesn't manage profiles or restrict tools.~~
Each app decides what tools to expose. Proxy just aggregates.

### NOT: Complex Multi-App Orchestration

~~Proxy doesn't coordinate state across apps.~~
Apps are independent. Agents can interact with multiple apps, but there's no built-in orchestration.

### NOT: Context Filtering

~~Proxy doesn't filter UI state visibility.~~
Apps return full state. If they want to hide internal details, they filter before returning. Proxy passes through as-is.

### NOT: Runtime Profile Switching

~~Proxy doesn't manage runtime mode changes.~~
Each app controls its own capabilities. If it wants to expose different tools in different modes, that's app-level logic.

***

## 9. Security Model (Simple)

**Security is handled at app level, not proxy level.**

- **App-level:** Each app decides what tools to expose
- **App-level:** Each app filters its own UI state before returning
- **App-level:** Each app can implement its own access control if needed
- **Proxy-level:** Proxy is transparent routing; no auth/authz logic

**Result:** Simple, decoupled, each app owns its own security boundary.

***

## 10. Observability

### 10.1 What Agents Do (Telemetry)

- Which tools agents use most frequently
- Which apps agents interact with most
- Tool success/failure rates
- Average execution time per tool
- Common error patterns


### 10.2 System Health

- App connection status (online/offline/recovering)
- Proxy uptime \& performance
- Tool execution latencies
- Connection count \& throughput


### 10.3 Development Workflow Metrics

- Time spent per app (which apps get most agent attention)
- Tool usage patterns (which UX patterns are tested most)
- Development velocity (commits per agent session)

***

## 11. Success Criteria

### 11.1 Functional

- ✅ Agent discovers all tools from 3+ apps via single MCP connection
- ✅ Tool execution routed correctly to target app
- ✅ Apps can connect/disconnect dynamically (no proxy restart needed)
- ✅ Headless + GUI modes both work
- ✅ Screenshots capture in both modes


### 11.2 Operational

- ✅ Proxy startup <2 seconds
- ✅ Tool execution <100ms p95 latency
- ✅ Screenshot capture <200ms
- ✅ Zero manual per-app MCP configuration needed (discovery automatic)
- ✅ Works in CI/CD (headless, deterministic)


### 11.3 Development Experience

- ✅ Agents can help develop/test Avalonia apps iteratively
- ✅ Agent can see app state, interact, see changes, repeat
- ✅ No special agent prompting needed (standard MCP pattern)
- ✅ Proxy is so generic any team can use it with their Avalonia apps

***

## 12. Rollout Strategy

### Phase 0: Foundation (Weeks 1-2)

- Research, spike validation, team alignment
- Validate .NET 10 MCP + `dnx` assumptions


### Phase 1: Core Library (Weeks 3-6)

- `Avalonia.Mcp.Core` library (embedded handler, standard tools)
- Headless + GUI support
- Basic tool set (click, type, screenshot, element tree, etc.)


### Phase 2: Proxy (Weeks 7-10)

- `Avalonia.McpProxy` standalone tool
- Multi-transport support (TCP, pipes, HTTP)
- Tool routing \& aggregation
- Live app discovery


### Phase 3: Watch-Tower Integration (Weeks 11-13)

- Embed core library in Watch-Tower
- Test iterative development via agent
- Demonstrate proxy usage pattern


### Phase 4: Polish \& Documentation (Weeks 14-17)

- Performance optimization
- Production hardening
- Comprehensive documentation
- Example scenarios

***

## 13. Project Structure

```
watch-tower-repo/
├── src/
│   ├── Avalonia.Mcp.Core/           # ← Reusable core library
│   │   ├── McpAgentTools.cs
│   │   ├── StandardTools.cs
│   │   └── Extensions/
│   │
│   ├── Avalonia.McpProxy/           # ← Standalone proxy (also in this repo)
│   │   ├── ProxyServer.cs
│   │   ├── AppRegistry.cs
│   │   └── Transports/
│   │
│   └── WatchTower/                  # ← Application that USES the core library + proxy
│       ├── App.xaml.cs
│       ├── MainWindow.xaml
│       └── WatchTowerMcpTools.cs    # Custom tools for Watch-Tower
│
├── docs/
│   ├── proxy-architecture.md
│   ├── integration-guide.md
│   └── development-workflow.md
│
└── samples/
    ├── SimpleCalculator/            # Example: simple app using core library
    └── TodoApp/                     # Example: another app using core library
```

**Key:**

- Core library is published to NuGet (others use it)
- Proxy is published as `dnx` tool to NuGet (others use it)
- Watch-Tower is example/primary app (built with core library, tested via proxy)

***

## 14. Vocabulary \& Terminology

| Term | Meaning |
| :-- | :-- |
| **Embedded MCP Handler** | Code running inside Avalonia app that exposes MCP tools |
| **Proxy** | Standalone MCP server that aggregates handlers from multiple apps |
| **Standard Tools** | Core UI interaction tools (click, type, screenshot) |
| **Custom Tools** | App-specific domain tools |
| **Tool Routing** | Proxy determining which app handler to invoke |
| **Transport** | Network protocol (TCP, pipes, SSE) between app and proxy |
| **Iterative Development** | Agent inspects → modifies code → reloads app → re-inspects → loops |


***

## 15. Non-Goals (Out of Scope)

- Visual-based element selection (use CSS/XPath selectors)
- Network traffic capture
- Performance profiling
- Video recording
- Multi-app state orchestration
- Profile-based access control (app-level concern, not proxy)
- Mobile/cross-platform (Avalonia desktop only)

***

## 16. What This Enables

### For Developers

- Agent helps build/test Avalonia apps interactively
- Real-time feedback loop: code → test → observe → iterate
- Agents inspect UI state and suggest improvements
- Custom tools for app-specific logic


### For Agents (AI)

- Single connection to interact with Avalonia apps (any Avalonia apps)
- Clear tool discovery (no guessing)
- Full UI interaction capability
- Iterative development support


### For Teams

- Reusable proxy (one deployment for all Avalonia apps)
- No per-app MCP server setup
- Plug-and-play integration (core library + embedded handler)
- Works headless or GUI


### For the Community

- Open-source, general-purpose proxy
- Any team can use it with their Avalonia apps
- Establishes pattern for AI-agent-driven Avalonia development

***

## Summary

**Build a reusable, open-source Avalonia MCP Proxy that enables agents to interact with any Avalonia applications through a single unified interface.**

The proxy is a **standalone tool** deployable by any team. Watch-Tower is the **first application** demonstrating its power—built and tested entirely through agent interaction via the proxy.

**Result:** Agents become natural partners in Avalonia application development, providing real-time feedback and suggestions as developers build.

