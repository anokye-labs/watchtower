# Watchtower: Vision Document

## The World Is Changing

We are witnessing the emergence of the **agentverse**—a constellation of AI coding agents, each with distinct capabilities, personalities, and interfaces. Claude Code reasons deeply about architecture. Aider excels at surgical file edits. OpenCode integrates with your favorite models. Copilot lives in your editor. Codex runs autonomously in the background.

This is not a temporary state. This is the new reality of software development.

The question is not "which agent should I use?" The question is "how do I harness them all?"

---

## The Problem

### Fragmented Control

Today, managing multiple agents means:
- Six terminal windows, each running a different agent
- Mental overhead tracking who's doing what
- Copy-pasting context between sessions
- No visibility into agent reasoning until it's done
- No coordination—agents work in isolation
- No governance—each session operates independently

You're not orchestrating. You're juggling.

### The Missing Layer

We have:
- ✅ Powerful individual agents (Claude Code, Aider, Copilot, etc.)
- ✅ Standard protocols emerging (MCP, Agent Client Protocol)
- ✅ Model diversity (Claude, GPT, Gemini, local models)

We're missing:
- ❌ **Unified visibility** across all agent activity
- ❌ **Coordination primitives** for multi-agent workflows
- ❌ **Constitutional governance** ensuring consistency
- ❌ **Human-in-the-loop interfaces** for meaningful oversight

Watchtower is this missing layer.

---

## The Vision

### Watchtower: Your Personal Agent Control Center

Watchtower is a **conversational GUI for coordinating AI coding agents**—your single interface for orchestrating Claude Code, Aider, OpenCode, GitHub Copilot, or whatever mix of agents works for you.

It doesn't replace your agents. It doesn't force uniformity. It's simply the **command center** through which you harness your personal agentverse.

```
┌─────────────────────────────────────────────────────────────────────┐
│  Watchtower                                              ─ □ ×    │
├──────────┬──────────────────────────────────────────┬───────────────┤
│          │                                          │               │
│ AGENTS   │  ACTIVE SESSION: Claude Code             │  ARTIFACTS    │
│          │  ════════════════════════════════════    │               │
│ ● Claude │  [Thinking] Analyzing the authentication │  📄 auth.ts   │
│   Code   │  flow. I see three concerns:             │  📄 api.ts    │
│          │                                          │  📄 tests.ts  │
│ ● Aider  │  1. The JWT validation happens too late  │               │
│   (idle) │     in the middleware chain...           │  CONTEXT      │
│          │                                          │  ─────────    │
│ ○ Copilot│  2. Token refresh logic is scattered     │  • PR #142    │
│   (off)  │     across multiple modules...           │  • Issue #89  │
│          │                                          │  • Spec v2.1  │
│ ● OpenCode│ 3. No rate limiting on auth endpoints   │               │
│   (busy) │                                          │               │
├──────────┴──────────────────────────────────────────┴───────────────┤
│  > @claude Let's start with concern #1. Show me the middleware.     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Core Principles

### 1. Protocol-Native, Not Proprietary

Watchtower speaks the languages agents already understand:

| Protocol | Use Case |
|----------|----------|
| **Agent Client Protocol (ACP)** | Native agent integration |
| **Model Context Protocol (MCP)** | Tool and context sharing |
| **VS Code Remote** | Extension-based agents |
| **CLI Wrapping** | Everything else |

No vendor lock-in. No forced migration. Use what works.

### 2. Human-in-the-Loop by Design

Agents are powerful but not infallible. Watchtower ensures humans remain in control:

- **Visibility**: See what every agent is thinking, doing, changing
- **Intervention**: Pause, redirect, or cancel any operation
- **Approval Gates**: Require human sign-off at critical points
- **Audit Trails**: Every decision logged, every action traceable

You're not debugging after the fact. You're steering in real-time.

### 3. Constitutional Governance

Define your principles once. Apply them everywhere.

```yaml
# constitution.yaml
principles:
  testing:
    required: true
    coverage_threshold: 80%

  security:
    require_review: ["auth/*", "crypto/*", "payments/*"]
    blocked_patterns: ["eval(", "exec(", "innerHTML"]

  handoffs:
    architecture_to_implementation:
      require_spec: true
      approval: human

  code_review:
    auto_approve_threshold: low_risk
    require_human: [high_risk, security_sensitive]
```

Every agent session operates within these guardrails. Coordination without micromanagement.

### 4. Living Context

Agents don't work in isolation. They share understanding:

- **Decision Logs**: Why did the architecture agent choose this approach?
- **Artifact Versioning**: Track every file change, every iteration
- **Context Flow**: Automatically share relevant context between agents
- **Collective Memory**: Learn from patterns across sessions

The tenth agent to touch this codebase benefits from what the first nine learned.

---

## How You Use It

### Scenario 1: Monitoring Everything

Open Watchtower and see your agent ecosystem at a glance:

```
ACTIVE SESSIONS
═══════════════
Claude Code    │ Designing payment API       │ ████████░░ 78%
Aider          │ Refactoring auth module     │ ██████░░░░ 55%  
OpenCode       │ Writing integration tests   │ ███░░░░░░░ 32%

RECENT DECISIONS
════════════════
[Claude] Chose event sourcing over CRUD for payment history (see reasoning →)
[Aider]  Split auth.ts into auth-core.ts and auth-middleware.ts
[OpenCode] Using factory pattern for test fixtures
```

Each session is a live conversation thread. See the agent's reasoning. Watch decisions unfold. Jump in when needed.

### Scenario 2: One-Shot Commands

Not everything needs orchestration. Quick tasks get quick treatment:

```
> @aider add error handling to the payment webhook
[Aider] Analyzing payment webhook handler...
[Aider] Adding try-catch with specific error types...
[Aider] ✓ Modified: src/webhooks/payment.ts (+23 lines)

> @claude review this change for security issues
[Claude] Reviewing payment.ts changes...
[Claude] ✓ No security issues found. Error messages don't leak sensitive info.
```

Watchtower calls the CLI, passes your prompt, captures output. Simple things stay simple.

### Scenario 3: Multi-Agent Workflows

Complex features need coordination. Define the workflow:

```yaml
# workflow: new-feature.yaml
stages:
  - name: Architecture
    agent: claude-code
    output: design-doc
    approval: human

  - name: Implementation  
    agent: aider
    input: design-doc
    output: code-changes

  - name: Testing
    agent: opencode
    input: code-changes
    output: test-suite

  - name: Security Review
    agent: claude-code
    input: [code-changes, test-suite]
    approval: human
```

Watchtower orchestrates the handoffs. You see the full pipeline. Intervene at any stage.

### Scenario 4: Context Injection

Your agents need to know things. Feed them context:

```
> /context add PR #142 "Authentication Refactor"
[Watchtower] Added PR context. Available to all agents.

> /context add @file:docs/auth-spec.md
[Watchtower] Added file context.

> @claude given this context, what's missing from the implementation?
[Claude] Comparing spec to PR... I see three gaps:
1. Spec requires token rotation; PR doesn't implement it
2. Missing audit logging for failed auth attempts
3. Rate limiting specified but not present
```

Context flows to agents automatically based on your rules.

---

## The Interface

### Design Philosophy

Watchtower's interface follows these principles:

**1. Calm Confidence**
No flashing alerts. No anxiety-inducing dashboards. Clean typography, subtle status indicators, information density that respects your attention.

**2. Progressive Disclosure**
See summaries by default. Drill into details when needed. Never overwhelmed, never under-informed.

**3. Conversation-First**
Agents are conversational. The interface reflects this. Rich text rendering of agent reasoning. Natural language input. The GUI enhances the conversation; it doesn't replace it.

**4. Keyboard-Native**
Power users live on the keyboard. Full command palette. Vim-style navigation. Quick switching between sessions. Mouse optional.

### Panel Architecture

Watchtower uses a VSCode-style docking layout:

| Panel | Purpose |
|-------|---------|
| **Agent List** | All agents, their status, quick actions |
| **Session View** | Active conversation with selected agent |
| **Artifacts** | Files, docs, and outputs from current session |
| **Context** | Shared knowledge available to agents |
| **Workflows** | Multi-agent pipelines and their status |
| **Constitution** | Active governance rules and their application |

Drag panels. Dock them. Float them. Make it yours.

### Command Interface

Every action is a command:

```
@<agent> <message>       # Send to specific agent
/context add <item>      # Add shared context  
/workflow run <name>     # Execute multi-agent workflow
/approve <decision>      # Human approval gate
/pause <agent>           # Pause agent execution
/constitution edit       # Modify governance rules
/history <agent>         # View agent decision history
```

Natural language when you want it. Structured commands when you need precision.

---

## Technical Foundation

### Built on Avalonia

Watchtower is built with Avalonia UI for true cross-platform capability:

- **Windows, macOS, Linux** — identical experience
- **High performance** — Skia rendering, sub-second startup
- **MVVM architecture** — clean, testable, extensible
- **Docking system** — VSCode-style panel management
- **Headless testing** — AI-assisted test generation

### Protocol Adapters

Modular adapter system for agent integration:

```
┌─────────────────────────────────────────┐
│           Watchtower Core              │
├─────────────────────────────────────────┤
│  ┌─────────┐ ┌─────────┐ ┌─────────┐   │
│  │   ACP   │ │   MCP   │ │  CLI    │   │
│  │ Adapter │ │ Adapter │ │ Adapter │   │
│  └────┬────┘ └────┬────┘ └────┬────┘   │
└───────┼──────────┼──────────┼──────────┘
        │          │          │
   ┌────▼────┐ ┌───▼───┐ ┌───▼────┐
   │ Claude  │ │  MCP  │ │ Aider  │
   │  Code   │ │Servers│ │  CLI   │
   └─────────┘ └───────┘ └────────┘
```

Add new agents by implementing the adapter interface. No core changes required.

### State Architecture

```
Watchtower State
├── Sessions[]           # Active agent conversations
│   ├── agent_id
│   ├── messages[]
│   ├── artifacts[]
│   └── decisions[]
├── Context              # Shared knowledge base
│   ├── files[]
│   ├── documents[]
│   └── decisions[]
├── Constitution         # Governance rules
│   ├── principles
│   ├── gates
│   └── handoff_rules
├── Workflows[]          # Multi-agent pipelines
│   ├── stages[]
│   ├── current_stage
│   └── approvals[]
└── Layout               # UI state (persisted)
```

All state changes are events. Full audit trail. Time-travel debugging.

---

## The Future

### Near Term (v1.0)

- Basic agent monitoring and one-shot commands
- CLI adapter for Claude Code, Aider, OpenCode
- Session management with conversation history
- Layout persistence
- Simple constitutional rules

### Medium Term (v2.0)

- Multi-agent workflows with handoffs
- MCP integration for shared context
- Decision logging and audit trails
- Approval gates and human-in-the-loop controls
- Artifact versioning

### Long Term (v3.0+)

- Voice control interface
- Agent capability discovery
- Automatic agent selection based on task
- Learning from user patterns
- API for external integrations
- Collaborative multi-user sessions

### The Bigger Picture

Watchtower is the **interface to your agentverse**—but the agentverse is still emerging.

As agents become more capable, the need for coordination grows. As protocols mature, integration becomes easier. As trust develops, autonomy increases.

Watchtower adapts:

- **Today**: Active monitoring, frequent intervention
- **Tomorrow**: Approval gates at key decisions
- **Eventually**: Ambient oversight, exception-based alerts

The interface evolves. The principle endures: **humans remain in control**.

---

## Why Now

### The Agents Are Here

Claude Code, Aider, Copilot, Cursor, OpenCode, Codex—the tools exist. They're powerful. They're proliferating. The question isn't whether to use AI agents; it's how to use them effectively.

### The Protocols Are Emerging

MCP standardizes tool integration. ACP defines agent communication. VS Code Remote enables extension hosting. The building blocks for interoperability are falling into place.

### The Need Is Acute

Developers are already juggling multiple agents. The pain is real. The workarounds are fragile. The time for a proper solution is now.

### The Technology Is Ready

Avalonia enables beautiful, performant cross-platform UIs. .NET provides robust backend capabilities. The technical foundation is solid.

---

## What Watchtower Is Not

**Not an agent**: Watchtower coordinates agents; it doesn't replace them. Your agents do the work. Watchtower provides the interface.

**Not a model router**: This isn't about picking the cheapest model for each query. Use whatever agents and models you prefer. Watchtower is agnostic.

**Not a workflow engine**: While Watchtower supports multi-agent workflows, it's not trying to be Apache Airflow. The focus is conversational coordination, not enterprise orchestration.

**Not a replacement for VS Code**: Watchtower manages agent conversations, not file editing. Use it alongside your editor, not instead of it.

---

## The Invitation

Watchtower is built on a simple belief: **the future of software development is multi-agent, and humans need interfaces to remain in control**.

If you share this belief—if you're juggling agents and wishing for better—Watchtower is for you.

The agentverse is vast. Your agents are capable. What's been missing is the control center.

Welcome to Watchtower.

---

## Appendix: Design Inspirations

### Interface
- VS Code's docking and panel system
- Zed's performance and typography focus
- Linear's calm, confident aesthetic
- Notion's progressive disclosure

### Coordination
- Kubernetes control plane architecture
- Event sourcing for audit trails
- Unix philosophy: do one thing well, compose via pipes

### Governance
- GitHub's CODEOWNERS for approval routing
- Kubernetes admission controllers for policy enforcement
- Constitutional AI principles for agent behavior

---

*Watchtower: See everything. Coordinate anything. Control what matters.*
