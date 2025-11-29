# Feature Specification: Federated Avalonia MCP Proxy Platform

**Feature Branch**: `002-federated-mcp-proxy`  
**Created**: 2025-11-28  
**Status**: Draft  
**Source Document**: [Federated Avalonia MCP Proxy Platform.md](../Federated%20Avalonia%20MCP%20Proxy%20Platform.md)  
**Goal**: Build a reusable, open-source MCP Proxy that aggregates embedded MCP handlers from multiple Avalonia applications, enabling agents to iteratively develop and test any Avalonia app through a single unified interface.

## User Scenarios & Testing *(mandatory)*

### User Story 0 - Embedded MCP Library Integration with WatchTower (Priority: P0)

As a developer building the federated MCP proxy platform, I want to first create the embedded MCP library and integrate it directly with WatchTower, so that I can validate the core MCP tool functionality before building the proxy federation layer.

**Why this priority**: This is the foundational building block. Before we can federate multiple applications through a proxy, we must first prove that a single Avalonia application can expose MCP tools reliably. WatchTower serves as the first test case to validate the embedded handler design, standard tools implementation, and headless/GUI compatibility.

**Independent Test**: Can be fully tested by embedding the MCP library in WatchTower, running WatchTower with the embedded handler, and having an agent connect directly to WatchTower's MCP interface to invoke tools like `CaptureScreenshot` and `GetElementTree`. Delivers immediate value by validating the core MCP integration pattern before adding proxy complexity.

**Acceptance Scenarios**:

1. **Given** the embedded MCP library exists as a reusable package, **When** a developer adds it to WatchTower's dependencies and initializes it in the application startup, **Then** WatchTower exposes standard MCP tools (`ClickElement`, `TypeText`, `CaptureScreenshot`, `GetElementTree`, `FindElement`, `WaitForElement`).

2. **Given** WatchTower is running with the embedded MCP handler in GUI mode, **When** an agent connects directly to WatchTower's MCP interface and calls `CaptureScreenshot`, **Then** the agent receives a valid screenshot of the current window.

3. **Given** WatchTower is running with the embedded MCP handler in headless mode, **When** an agent calls `GetElementTree`, **Then** the agent receives a complete hierarchical representation of all UI elements.

4. **Given** WatchTower is running with the embedded MCP handler, **When** an agent calls `ClickElement` at a valid coordinate, **Then** the click is processed and the UI responds appropriately.

5. **Given** the embedded MCP library is integrated with WatchTower, **When** a developer defines a custom tool (e.g., `ResetAppState`), **Then** the custom tool appears in the MCP tool catalog alongside the standard tools.

---

### User Story 1 - Single Connection to Multiple Apps (Priority: P1)

As an AI agent (Claude, GitHub Copilot, etc.), I want to connect to a single MCP server that provides access to all running Avalonia applications, so that I can interact with any Avalonia app without managing multiple server connections or context-switching between different tool sets.

**Why this priority**: This is the core value proposition of the platform. Without federation, agents must manage separate MCP connections for each app, creating complexity and friction. This single-connection model is essential for all other functionality.

**Independent Test**: Can be fully tested by starting the proxy, connecting one Avalonia app with an embedded handler, and verifying an agent can discover and invoke the app's tools through the proxy. Delivers immediate value by simplifying agent configuration.

**Acceptance Scenarios**:

1. **Given** the MCP proxy is running and Watch-Tower is connected with its embedded handler, **When** an agent connects to the proxy and requests available tools, **Then** the agent receives a unified list of tools prefixed with "WatchTower:" (e.g., `WatchTower:ClickElement`, `WatchTower:CaptureScreenshot`).

2. **Given** multiple Avalonia apps (Watch-Tower, AdminTool, DataService) are connected to the proxy, **When** an agent lists available tools, **Then** the agent sees tools from all apps organized by namespace (e.g., `WatchTower:*`, `AdminTool:*`, `DataService:*`) without ambiguity.

3. **Given** an agent has discovered tools from the proxy, **When** the agent calls `WatchTower:ClickElement(100, 50)`, **Then** the proxy routes the call to Watch-Tower's handler, executes the click, and returns the result to the agent.

---

### User Story 2 - UI Inspection and Screenshot Capture (Priority: P1)

As an AI agent helping a developer build an Avalonia app, I want to inspect the current UI state and capture screenshots, so that I can understand the application's visual layout and provide informed suggestions for improvements.

**Why this priority**: Visual verification and UI state inspection are fundamental to agent-driven development. Agents cannot provide useful feedback without seeing what the app looks like.

**Independent Test**: Can be fully tested by having an agent request a UI element tree and screenshot from a running app. Delivers value by enabling agents to "see" the application state.

**Acceptance Scenarios**:

1. **Given** Watch-Tower is running and connected to the proxy, **When** an agent calls `WatchTower:GetElementTree`, **Then** the agent receives a hierarchical representation of all UI elements including their types, positions, properties, and accessibility information.

2. **Given** Watch-Tower is running in GUI mode, **When** an agent calls `WatchTower:CaptureScreenshot`, **Then** the agent receives a visual capture of the current window state within 200ms.

3. **Given** Watch-Tower is running in headless mode (no display), **When** an agent calls `WatchTower:CaptureScreenshot`, **Then** the agent still receives a valid visual capture rendered via the headless platform.

---

### User Story 3 - UI Interaction (Priority: P1)

As an AI agent, I want to interact with Avalonia applications by clicking elements, typing text, and triggering actions, so that I can test the application's behavior and validate changes made by developers.

**Why this priority**: Interaction capability is essential for the iterative development workflow. Without the ability to click, type, and navigate, agents cannot verify that code changes produce the expected behavior.

**Independent Test**: Can be fully tested by having an agent locate a button, click it, and verify the resulting state change. Delivers value by enabling automated interaction testing.

**Acceptance Scenarios**:

1. **Given** Watch-Tower displays a login form, **When** an agent calls `WatchTower:TypeText("username@example.com")` on the email field, **Then** the text appears in the input field.

2. **Given** Watch-Tower displays a "Submit" button at coordinates (200, 150), **When** an agent calls `WatchTower:ClickElement(200, 150)`, **Then** the button is activated and the form submission process begins.

3. **Given** Watch-Tower has a search field, **When** an agent calls `WatchTower:FindElement("SearchBox")`, **Then** the agent receives the element's position and properties for subsequent interaction.

4. **Given** an agent needs to wait for a loading indicator to disappear, **When** the agent calls `WatchTower:WaitForElement("LoadingSpinner", timeout: 5000)` with visibility=false, **Then** the call returns when the element is no longer visible or times out.

---

### User Story 4 - Dynamic App Discovery (Priority: P2)

As an AI agent, I want to see applications connect and disconnect in real-time, so that I always have an accurate view of available tools without restarting the proxy or refreshing manually.

**Why this priority**: Dynamic discovery enables smooth iterative development workflows where developers restart their apps frequently. Without it, agents would work with stale tool lists.

**Independent Test**: Can be fully tested by starting an app while the proxy is running and verifying the agent sees new tools without reconnecting. Delivers value by supporting rapid iteration.

**Acceptance Scenarios**:

1. **Given** the proxy is running with no apps connected, **When** Watch-Tower starts and connects its embedded handler, **Then** the proxy immediately updates its tool catalog and agents see Watch-Tower's tools.

2. **Given** Watch-Tower is connected and an agent is using its tools, **When** Watch-Tower shuts down, **Then** the proxy removes Watch-Tower's tools from the catalog and agents receive appropriate errors if they try to call those tools.

3. **Given** a developer rebuilds and restarts Watch-Tower during a development session, **When** the new instance connects, **Then** the agent's tool list updates to reflect any new or changed tools without agent intervention.

---

### User Story 5 - Iterative Development Workflow (Priority: P2)

As a developer working with an AI agent, I want the agent to inspect my app, suggest code changes, and verify those changes after I rebuild, so that I can leverage AI assistance throughout my development cycle.

**Why this priority**: This is the end-to-end workflow that ties all capabilities together. While individual tools provide value independently, the full loop demonstrates the platform's transformative potential.

**Independent Test**: Can be fully tested by having an agent identify a UI issue, suggest a fix, then verify the fix after app restart. Delivers value by enabling agent-assisted development.

**Acceptance Scenarios**:

1. **Given** an agent has inspected Watch-Tower and identified a button with incorrect styling, **When** the developer modifies the code and rebuilds, **Then** the agent can capture a new screenshot and confirm the styling is corrected.

2. **Given** Watch-Tower crashes during testing, **When** the developer fixes the bug and restarts, **Then** the agent can resume interaction without any manual reconfiguration.

3. **Given** an agent is helping test a new feature, **When** the feature is complete, **Then** the agent can document all the interactions it performed and their results for future reference.

---

### User Story 6 - Custom Application Tools (Priority: P3)

As an Avalonia application developer, I want to expose domain-specific tools beyond the standard UI interactions, so that agents can perform application-specific operations like resetting state or loading test data.

**Why this priority**: Custom tools extend the platform beyond basic UI testing into domain-specific automation. While valuable, basic UI tools must work first.

**Independent Test**: Can be fully tested by implementing a custom `ResetAppState` tool in Watch-Tower and having an agent invoke it. Delivers value by enabling domain-specific automation.

**Acceptance Scenarios**:

1. **Given** Watch-Tower exposes a custom `WatchTower:ResetAppState` tool, **When** an agent calls this tool, **Then** the application returns to its initial state for consistent test starting points.

2. **Given** Watch-Tower exposes `WatchTower:SetTestData(scenarioName)`, **When** an agent calls this with "HighVolumeAlerts", **Then** the application loads a predefined dataset for testing that scenario.

3. **Given** an agent queries available tools, **When** Watch-Tower has both standard and custom tools, **Then** the agent sees all tools with appropriate descriptions and parameter information.

---

### User Story 7 - Headless CI/CD Operation (Priority: P3)

As a DevOps engineer, I want to run Avalonia apps and the MCP proxy in headless mode without a display, so that agent-driven tests can execute in continuous integration pipelines.

**Why this priority**: CI/CD integration is important for production-ready tooling but requires core functionality to work first.

**Independent Test**: Can be fully tested by running the proxy and an Avalonia app in headless mode on a CI server and verifying screenshot capture works. Delivers value by enabling automated testing pipelines.

**Acceptance Scenarios**:

1. **Given** a CI/CD pipeline with no display environment, **When** the proxy and Watch-Tower start in headless mode, **Then** all tool calls succeed including screenshot capture (rendered via headless platform).

2. **Given** a headless test run completes, **When** results are collected, **Then** screenshots and element trees are available for test reporting and debugging.

---

### Edge Cases

- What happens when an agent calls a tool for an app that just disconnected? The proxy returns an error indicating the app is no longer available, with guidance to check if the app is running.

- How does the system handle two apps with the same name? The proxy rejects duplicate registrations and requires unique app identifiers. Applications should use stable, unique identifiers (e.g., `MyApp-Development`, `MyApp-v2.1.0`) rather than generic names. When a duplicate is detected, the proxy returns an error with guidance to use a unique identifier.

- What happens when a tool call times out? The proxy returns a timeout error after a configurable period (default: 30 seconds) without blocking other operations.

- How does the system handle rapid app connect/disconnect cycles? The proxy implements debouncing to prevent tool catalog thrashing during app restarts.

- What happens if the embedded handler crashes but the app continues running? The proxy detects the broken connection and marks the app as unavailable. The app's heartbeat mechanism should detect this and attempt reconnection.

- What happens when system resources are exhausted (too many apps or concurrent tool calls)? The proxy implements graceful degradation: new connections receive backpressure signals, tool calls queue with visible wait times, and the system logs warnings about resource constraints without hard failures.

## Requirements *(mandatory)*

### Functional Requirements

**Core Library (Embedded Handler)**

- **FR-001**: System MUST provide a reusable library that Avalonia applications can embed to expose MCP tools.
- **FR-002**: Embedded handlers MUST expose standard UI interaction tools: `ClickElement`, `TypeText`, `CaptureScreenshot`, `GetElementTree`, `FindElement`, and `WaitForElement`.
- **FR-003**: Embedded handlers MUST support custom tool definitions for application-specific operations.
- **FR-004**: Embedded handlers MUST work in both GUI mode (with display) and headless mode (without display).
- **FR-005**: Screenshot capture MUST work in headless mode using the headless rendering platform.

**Proxy Server**

- **FR-006**: Proxy MUST aggregate tools from all connected applications into a single unified interface.
- **FR-007**: Proxy MUST namespace all tools with their source application identifier (e.g., `AppName:ToolName`).
- **FR-008**: Proxy MUST route tool calls to the correct application handler based on the namespace prefix.
- **FR-009**: Proxy MUST detect and report application connections and disconnections in real-time.
- **FR-010**: Proxy MUST update the tool catalog dynamically when applications connect or disconnect.
- **FR-011**: Proxy MUST expose a single MCP interface (stdio) to agent clients, separate from the app-to-proxy transport layer (FR-012 to FR-014).

**Transport Layer**

- **FR-012**: System MUST support TCP transport for local development scenarios.
- **FR-013**: System MUST support Named Pipes transport for Windows inter-process communication.
- **FR-014**: System MUST support HTTP/SSE transport for remote scenarios.
- **FR-015**: Transport selection MUST be transparent to both applications and agents.

**Discovery & Configuration**

- **FR-016**: Proxy MUST read application endpoints from a configuration file.
- **FR-017**: Applications MUST register with the proxy automatically when they start.
- **FR-018**: Applications MUST unregister from the proxy when they shut down gracefully.

**Error Handling**

- **FR-019**: Proxy MUST return meaningful error messages when tool calls fail.
- **FR-020**: Proxy MUST handle application crashes gracefully without affecting other connected applications.
- **FR-021**: Proxy MUST implement configurable timeouts for tool calls (default: 30 seconds).

**Security**

- **FR-022**: Proxy and applications MUST authenticate using a shared secret token stored in configuration files.
- **FR-023**: Agent connections MUST provide the shared secret during initial handshake.
- **FR-024**: Application registrations MUST include the shared secret for validation.
- **FR-025**: Failed authentication attempts MUST be logged and rejected with appropriate error messages.

**Observability**

- **FR-026**: Proxy MUST implement structured logging with JSON-formatted output including timestamps, log levels, and contextual information.
- **FR-027**: Log verbosity MUST be configurable via appsettings.json with levels: Debug, Information, Warning, Error.
- **FR-028**: Proxy MUST log connection events (app connect/disconnect), authentication attempts, tool call routing, and errors.
- **FR-029**: Each log entry for tool calls MUST include: timestamp, application identifier, tool name, execution duration, and result status.
- **FR-030**: Embedded handlers MUST support the same structured logging configuration for consistency across the platform.

**Element Identification**

- **FR-031**: Tools that locate UI elements (FindElement, WaitForElement) MUST support hierarchical path-based identification using accessibility tree paths.
- **FR-032**: Hierarchical paths MUST use format: "ParentType[index]/ChildType[index]" (e.g., "MainWindow/Panel[0]/Button[2]").
- **FR-033**: GetElementTree MUST return sufficient metadata (element type, index within parent, accessibility properties) to construct valid hierarchical paths.
- **FR-034**: Coordinate-based interaction tools (ClickElement, TypeText) MUST accept X/Y pixel positions for cases where hierarchical paths are impractical.

### Key Entities

- **MCP Proxy**: The standalone server that federates tools from multiple applications. Maintains a registry of connected apps, their tool catalogs, and active connections. Routes tool calls to appropriate handlers.

- **Embedded MCP Handler**: Code module integrated into each Avalonia application that exposes MCP tools. Connects to the proxy on app startup, implements standard UI tools, and allows custom tool registration.

- **Tool Catalog**: The aggregated list of all available tools across connected applications. Each tool entry includes namespace, name, description, and parameter schema.

- **Application Registration**: The record of a connected application including its identifier, endpoint, connection status, and exposed tools.

- **Transport Connection**: The communication channel between an application's embedded handler and the proxy. Abstracts TCP, Named Pipes, or HTTP/SSE protocols.

## Success Criteria *(mandatory)*

### Measurable Outcomes

**Functional**

- **SC-001**: Agents can discover tools from 3 or more connected applications through a single MCP connection.
- **SC-002**: Tool calls are routed correctly to the target application 100% of the time based on namespace prefix.
- **SC-003**: Applications can connect and disconnect dynamically without proxy restart.
- **SC-004**: Both headless and GUI mode applications can capture screenshots and return UI element trees.

**Operational**

- **SC-005**: Proxy starts and becomes ready for connections in under 2 seconds.
- **SC-006**: 95th percentile tool execution latency is under 100ms (excluding screenshot capture).
- **SC-007**: Screenshot capture completes in under 200ms.
- **SC-008**: Applications register with the proxy within 1 second of startup.
- **SC-009**: System works in CI/CD environments without displays (fully headless).

**Development Experience**

- **SC-010**: Agents can complete a full inspect-interact-verify cycle without manual intervention.
- **SC-011**: Developers can restart their applications during an agent session without reconfiguring the agent.
- **SC-012**: New Avalonia applications can integrate the embedded handler with less than 10 lines of code.
- **SC-013**: The proxy requires zero application-specific configuration (apps self-describe their tools).

## Assumptions

- .NET 10 provides stable support for the required MCP protocol implementation.
- Avalonia's headless platform supports all required screenshot and UI inspection operations.
- Applications run on the same machine as the proxy in typical development scenarios.
- Agent clients (Claude, GitHub Copilot) support standard MCP protocol via stdio.
- TCP localhost connections provide sufficient performance for local development (sub-millisecond latency).
- Named Pipes are available on Windows for secure inter-process communication.

## Clarifications

### Session 2025-11-28

- Q: The spec describes a proxy that aggregates tools from multiple applications, allowing agents to control any connected app. However, there's no security model defined for scenarios where sensitive operations are exposed. What security model should be used? → A: Shared secret token - proxy and apps share a configuration-file-based secret; agents authenticate once per session
- Q: The spec includes performance targets and error handling but doesn't specify what observability features the proxy should provide for debugging connection issues, performance problems, or tool execution failures in production. What logging/observability approach should be used? → A: Structured logging with configurable verbosity - JSON-formatted logs with levels (Debug/Info/Warning/Error), configurable via appsettings.json, optimized for localhost development scenarios
- Q: The spec describes federating tools from multiple applications but doesn't specify limits on how many apps can connect simultaneously or how many concurrent tool calls the proxy should handle. What scalability limits should be enforced? → A: Unlimited - no explicit limits, rely on system resources and implement graceful degradation
- Q: The spec mentions tools like FindElement and WaitForElement that locate UI elements, but doesn't specify what identification strategy should be used (e.g., element names, IDs, visual coordinates, accessibility properties). What element identification approach should be used? → A: Full accessibility tree path - use hierarchical paths like "MainWindow/Panel[0]/Button[2]" for precise targeting

## Non-Goals

The following are explicitly out of scope for this feature:

- Visual-based element selection (CSS/XPath selectors are used instead)
- Network traffic capture or monitoring
- Performance profiling of applications
- Video recording of interactions
- Multi-app state orchestration (apps remain independent)
- Profile-based access control (security is app-level responsibility)
- Mobile or non-desktop platform support
