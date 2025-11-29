# Feature Specification: Simplified Watchtower UX

**Feature Branch**: `003-simplified-ux`  
**Created**: 2025-11-29  
**Status**: Draft  
**Source Document**: specs/vision.md  
**Goal**: Design a barebones, self-contained UX for Watchtower that replaces the complex dock library approach with a simpler fixed-panel layout

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Active Agent Sessions (Priority: P1)

As a developer, I want to see all my active AI agent sessions at a glance so I can understand what each agent is doing without switching between multiple terminal windows.

**Why this priority**: This is the core value proposition of Watchtower—unified visibility across the agentverse. Without this, users have no reason to use the application.

**Independent Test**: Can be fully tested by launching the application with mock agent data and verifying the session list displays agent names, statuses, and current activity summaries.

**Acceptance Scenarios**:

1. **Given** the application is running, **When** the user opens Watchtower, **Then** they see a list of configured agents with their current status (active, idle, disconnected)
2. **Given** multiple agents are running, **When** an agent changes status, **Then** the status indicator updates in real-time without user intervention
3. **Given** the session list is visible, **When** the user clicks on an agent, **Then** that agent's conversation becomes the active view

---

### User Story 2 - Read Agent Conversation (Priority: P1)

As a developer, I want to read the full conversation with a selected agent so I can understand its reasoning and track decisions being made.

**Why this priority**: Reading agent output is fundamental to the "human-in-the-loop" principle. Users must see what agents are thinking to maintain meaningful oversight.

**Independent Test**: Can be fully tested by selecting an agent from the list and verifying the conversation panel displays messages with proper formatting, timestamps, and visual distinction between user and agent messages.

**Acceptance Scenarios**:

1. **Given** an agent is selected, **When** the conversation view loads, **Then** all messages are displayed chronologically with clear visual distinction between user prompts and agent responses
2. **Given** an agent sends a new message, **When** the message arrives, **Then** it appears in the conversation view and auto-scrolls to show the latest content
3. **Given** a long conversation exists, **When** the user scrolls up, **Then** they can read older messages and the view stops auto-scrolling until they scroll to the bottom again

---

### User Story 3 - Send Message to Agent (Priority: P1)

As a developer, I want to send messages to a selected agent using a command input so I can direct the agent's work without leaving Watchtower.

**Why this priority**: Two-way communication is essential for agent coordination. Without the ability to send messages, Watchtower is only a viewer, not a control center.

**Independent Test**: Can be fully tested by typing a message in the input area and verifying it is sent to the selected agent and appears in the conversation.

**Acceptance Scenarios**:

1. **Given** an agent is selected and connected, **When** the user types a message and presses Enter, **Then** the message is sent to the agent and appears in the conversation view
2. **Given** the input field is focused, **When** the user uses keyboard shortcuts (e.g., Ctrl+Enter for multi-line), **Then** the expected behavior occurs
3. **Given** no agent is selected, **When** the user attempts to send a message, **Then** the system shows a clear indication that an agent must be selected first

---

### User Story 4 - Switch Between Agents (Priority: P2)

As a developer, I want to quickly switch between different agent conversations using keyboard navigation so I can monitor multiple agents efficiently.

**Why this priority**: Power users need fast navigation. Keyboard-native design is a core principle from the vision document.

**Independent Test**: Can be fully tested by using keyboard shortcuts to navigate the agent list and verifying focus changes appropriately.

**Acceptance Scenarios**:

1. **Given** multiple agents are configured, **When** the user presses the designated navigation keys (e.g., Up/Down arrows in agent list), **Then** the selection moves between agents
2. **Given** an agent is selected via keyboard, **When** the user presses Enter or a designated key, **Then** that agent's conversation becomes active
3. **Given** the command input has focus, **When** the user presses a shortcut to switch agents, **Then** focus returns to the agent list for navigation

---

### User Story 5 - Persist Window Layout (Priority: P3)

As a developer, I want the application to remember my window size and position so I don't have to re-arrange it every time I start Watchtower.

**Why this priority**: Quality-of-life feature that improves user experience over time but is not essential for core functionality.

**Independent Test**: Can be fully tested by resizing/moving the window, closing the application, reopening it, and verifying the previous size and position are restored.

**Acceptance Scenarios**:

1. **Given** the user has resized the window, **When** they close and reopen the application, **Then** the window opens with the same size
2. **Given** the user has moved the window to a specific position, **When** they close and reopen the application, **Then** the window opens at the same position
3. **Given** the saved position is now off-screen (e.g., external monitor disconnected), **When** the application opens, **Then** the window appears in a visible default position

---

### Edge Cases

- What happens when an agent disconnects unexpectedly during a conversation?
  - The status indicator updates to show disconnection and the conversation remains viewable
- What happens when the user has no agents configured?
  - The application displays a helpful empty state with guidance on how to configure agents
- What happens when a message fails to send?
  - The message shows an error indicator with option to retry
- What happens when the conversation history is extremely long (10,000+ messages)?
  - The application loads messages incrementally (virtualized list) to maintain performance

## Requirements *(mandatory)*

### Functional Requirements

#### Layout & Structure
- **FR-001**: Application MUST display a fixed three-panel layout: agent list (left), conversation view (center), and command input (bottom)
- **FR-002**: Panels MUST NOT be draggable, dockable, or floatable—the layout is fixed and self-contained
- **FR-003**: Application MUST provide clear visual boundaries between panels

#### Agent List Panel
- **FR-004**: Agent list MUST display all configured agents with their name and status indicator
- **FR-005**: Status indicators MUST use distinct visual cues (color, icon) for: active, idle, and disconnected states
- **FR-006**: Agent list MUST highlight the currently selected agent
- **FR-007**: Agent list MUST support single-click selection to switch the active conversation

#### Conversation Panel
- **FR-008**: Conversation panel MUST display messages in chronological order
- **FR-009**: User messages and agent messages MUST be visually distinct (alignment, color, or style)
- **FR-010**: Conversation panel MUST auto-scroll to new messages when user is at the bottom
- **FR-011**: Conversation panel MUST preserve scroll position when user has scrolled up to read history
- **FR-012**: Messages MUST display timestamps in a user-friendly format

#### Command Input
- **FR-013**: Command input MUST be a text entry area that accepts user messages
- **FR-014**: Command input MUST send the message when user presses Enter
- **FR-015**: Command input MUST support basic text editing (copy, paste, undo)
- **FR-016**: Command input MUST clear after successful message submission

#### Keyboard Navigation
- **FR-017**: Application MUST support keyboard shortcuts to navigate between agents
- **FR-018**: Application MUST support keyboard shortcut to focus the command input
- **FR-019**: Application MUST support keyboard shortcut to access a command palette (future extensibility)

#### Window Management
- **FR-020**: Application MUST persist window size and position between sessions
- **FR-021**: Application MUST detect and handle invalid saved positions (off-screen recovery)

### Key Entities

- **Agent**: Represents a connected AI coding agent. Key attributes: unique identifier, display name, connection status, current activity description
- **Message**: A single communication unit in a conversation. Key attributes: content, sender type (user or agent), timestamp, delivery status
- **Session**: An active conversation with an agent. Key attributes: associated agent, ordered list of messages, connection state

## Assumptions

- The application will initially support viewing and interacting with agents that have been pre-configured (agent configuration management is out of scope for this UX spec)
- Protocol adapters for specific agents (CLI, ACP, MCP) will be implemented separately; this spec focuses on the UI/UX layer
- The application will run as a desktop application on Windows, macOS, and Linux with identical UX
- Initial implementation will use mock data to validate the UX before integrating real agent adapters

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can identify which agents are active and their current status within 3 seconds of opening the application
- **SC-002**: Users can switch between agent conversations in under 1 second using either mouse or keyboard
- **SC-003**: Users can read the full conversation history for any agent without noticeable scrolling lag (even with 1000+ messages)
- **SC-004**: Users can send a message to an agent in under 5 seconds from application launch (select agent → type → send)
- **SC-005**: Window layout persists correctly 100% of the time across normal application restarts
- **SC-006**: 90% of first-time users can identify how to send a message to an agent without external documentation (intuitive design)
