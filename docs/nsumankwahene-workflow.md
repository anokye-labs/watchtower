# Nsumankwahene Workflow (Spiritual Guardian Flow)

*English: Agent-Optimized Continuous Flow Project Management*

## Overview

The Nsumankwahene system replaces time-based sprint iterations with a state-driven continuous flow optimized for AI agent collaboration. Named after the Akan concept of a spiritual guardian, it provides structure and protection for the development process.

### Why Continuous Flow?

AI agents work asynchronously, in parallel, and with different context needs than human developers. Traditional sprints create artificial time boundaries that conflict with agent capabilities:

- **Agents don't "sprint"** - they work when invoked
- **Context is expensive** - agents need clear state visibility
- **Dependencies matter more than time** - blockers affect agents immediately

### Design Philosophy

This system honors WatchTower's "Ancestral Futurism" design language by using Akan terminology with English descriptions. This is not merely decorative—it reinforces the cultural foundation of the project while maintaining accessibility.

---

## Field Glossary

### Status
*Akan: Nsoromma ("Star progression through the night sky")*

| Value | When to Use |
|-------|-------------|
| Backlog | Item is captured but not ready to start |
| Ready | All dependencies met, ready for agent work |
| In progress | Actively being worked (PR open) |
| In review | PR opened, checks passing, awaiting merge |
| Blocked | Cannot proceed, needs human intervention |
| Done | Merged and complete |
| Abandoned | Will not be implemented |

### Priority
*Akan: Tumi ("Power/Authority level")*

| Value | Response Time |
|-------|---------------|
| P0 | Critical - drop other work, immediate attention |
| P1 | High - this week |
| P2 | Medium - this month |
| P3 | Low - when convenient |

### Complexity
*Akan: Mu ("Depth/Difficulty")*

Uses Fibonacci scale (1, 2, 3, 5, 8, 13):
- **1-2**: Simple, Copilot-eligible
- **3-5**: Moderate, may need guidance
- **8-13**: Complex, requires Tech Design Needed

### Agent Type
*Akan: Ɔkyeame ("Linguist/Interpreter")*

| Value | When to Assign |
|-------|----------------|
| Copilot | Simple, well-defined tasks (Complexity ≤ 3) |
| Copilot + Thinking | Tasks requiring extended reasoning |
| Task-Maestro | Issue creation and planning |
| Human Required | Requires human decision |
| Any Agent | Any agent can work on this |

### Component
*Akan: Fapem ("Foundation section")*

Matches codebase structure:
- Services
- View Model
- Views
- Models
- Testing
- Infrastructure
- Docs
- Build

### Dependencies
*Akan: Nkabom ("Unity/Connection")*

Format: `#42, anokye-labs/watchtower#53` or `None`

All listed issues must be in Done before work can begin.

### Last Activity
*Akan: Da-Akyire ("Most recent day")*

Auto-updated on issue/PR activity. Used for stale detection.

### PR Link
*Akan: PR Nkitahodi ("Pull request connection")*

Auto-populated when PR references issue.

---

## Status State Machine

```
                    ┌─────────────────────────────────────────────────┐
                    │                                                 │
                    ▼                                                 │
Backlog ──────► Ready ──────► In progress ──────► In review ──────► Done       │
                              │                   │                           │
                              │                   │                           │
                              ▼                   │                           │
                            Blocked ◄─────────────┘                           │
                              │                                               │
                              │                                               │
                              ▼                                               │
                           Abandoned ───────────────────────────────────────────┘
```

### Transitions

| From | To | Trigger |
|------|----|---------|
| Backlog | Ready | Dependencies resolved, refinement complete |
| Ready | In progress | PR opened referencing issue |
| In progress | In review | Checks pass AND PR ready for review |
| In review | Done | PR merged |
| Any | Blocked | Blocker encountered, 5+ days stale |
| Blocked | Ready | Blocker resolved |
| Any | Abandoned | Decision to not implement |

---

## Agent Decision Rules

### Work Selection

```
IF query "Adwuma Nhyehyɛe" (Work Queue) view:
  FOR each item ordered by Priority DESC, Complexity ASC:
    IF Status = "Ready" 
       AND Dependencies = "None" or all deps in Done
       AND Agent Type matches my type:
      → Start this task
```

### Before Starting Work

1. ✅ Verify Dependencies - all dependencies in Done
2. ✅ Check for `nnipa-gyinae-hia` label - if present, STOP
3. ✅ Check for `Tech Design Needed` - if present, verify design approved
4. ✅ Read acceptance criteria carefully

### When Encountering Blockers

1. Set Status to Blocked
2. Update Dependencies with blocker issue numbers
3. Add comment explaining the blocker
4. Assign to @hoopsomuah

### On Completion

1. Ensure all acceptance criteria met
2. Run `dotnet build` and `dotnet test`
3. Create PR with `Closes #issue-number` in body
4. If Complexity >= 5, request review

---

## Label Reference

### Agent Signals
| Label | Meaning |
|-------|---------|
| `ɔkyeame:siesie` | Ready for agent work |
| `ɔkyeame:dwuma` | Agent actively working |
| `nnipa-gyinae-hia` | Requires human decision |
| `stale` | No activity >5 days |
| `needs-fix` | PR checks failed |

### Requirement Signals
| Label | Meaning |
|-------|---------|
| `nhwɛsoɔ-hia` | Must include tests |
| `nsakrae:api` | Breaking API change |
| `asiw:external` | Blocked by third-party |
| `Tech Design Needed` | Requires architecture review |

---

## Troubleshooting

### Item stuck in "In progress"

1. Check Last Activity - if >5 days, automation should have set Blocked
2. Verify Dependencies are resolved
3. Check if agent is actively working (look for recent commits)
4. Manually set to Blocked if stuck

### Automation not triggering

1. Verify `.github/workflows/nsumankwahene-automation.yml` exists
2. Check workflow runs in Actions tab
3. Ensure PR body contains `Closes #X` or `Fixes #X`
4. Verify field IDs in `.github/project-config.yml` are current

### Item missing from views

1. Verify item is added to project
2. Check field values match view filters
3. Manually populate any missing fields

---

## GraphQL Query Cookbook

### Get Work Queue

```graphql
query GetWorkQueue {
  organization(login: "anokye-labs") {
    projectV2(number: 2) {
      items(first: 20) {
        nodes {
          content { ... on Issue { number title } }
          fieldValues(first: 10) {
            nodes {
              ... on ProjectV2ItemFieldSingleSelectValue {
                field { name }
                name
              }
            }
          }
        }
      }
    }
  }
}
```

### Transition Status

```graphql
mutation TransitionToReview($projectId: ID!, $itemId: ID!, $fieldId: ID!, $optionId: ID!) {
  updateProjectV2ItemFieldValue(input: {
    projectId: $projectId
    itemId: $itemId
    fieldId: $fieldId
    value: { singleSelectOptionId: $optionId }
  }) {
    projectV2Item { id }
  }
}
```

### Example: Set Status to "In review"

Using values from `.github/project-config.yml`:

```graphql
mutation {
  updateProjectV2ItemFieldValue(input: {
    projectId: "PVT_kwDODbLOnM4BLchY"
    itemId: "<item-id>"
    fieldId: "PVTSSF_lADODbLOnM4BLchYzg7BHPM"
    value: { singleSelectOptionId: "aba860b9" }
  }) {
    projectV2Item { id }
  }
}
```

---

## Related Documentation

- [AGENTS.md](../AGENTS.md) - Agent development guidelines
- [Design Language](../concept-art/design-language.md) - Akan cultural design system
- [.github/ɔkyeame-config.yml](../.github/ɔkyeame-config.yml) - Agent configuration
- [.github/project-config.yml](../.github/project-config.yml) - Field IDs and project configuration

---

## Appendix: Field Mapping Reference

Quick reference for developers and agents working with the GitHub API:

| English Field | Akan Term | Field ID | Type |
|---------------|-----------|----------|------|
| Status | Nsoromma | PVTSSF_lADODbLOnM4BLchYzg7BHPM | SingleSelect |
| Priority | Tumi | PVTSSF_lADODbLOnM4BLchYzg7BHUU | SingleSelect |
| Complexity | Mu | PVTF_lADODbLOnM4BLchYzg7BHUc | Number |
| Agent Type | Ɔkyeame | PVTSSF_lADODbLOnM4BLchYzg7DI1s | SingleSelect |
| Component | Fapem | PVTSSF_lADODbLOnM4BLchYzg7DI6Y | SingleSelect |
| Dependencies | Nkabom | PVTF_lADODbLOnM4BLchYzg7DI88 | Text |
| Last Activity | Da-Akyire | PVTF_lADODbLOnM4BLchYzg7DJR8 | Date |
| PR Link | PR Nkitahodi | PVTF_lADODbLOnM4BLchYzg7DJHo | Text |

---

*Last Updated: 2025-12-29*
