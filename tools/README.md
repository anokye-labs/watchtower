# WatchTower Tools

This directory contains utility scripts and tools for repository management and development workflows.

## Label Management

### create-labels.sh

Creates semantic labels for the Agent Flow System (agent-driven development).

**Prerequisites:**
- `gh` CLI installed ([installation guide](https://cli.github.com/manual/installation))
- Authenticated with GitHub: `gh auth login`
- Repository admin or maintainer permissions

**Usage:**

```bash
# Preview what would be created (dry-run)
./tools/create-labels.sh --dry-run

# Actually create the labels
./tools/create-labels.sh
```

**What it creates:**

The script creates 8 agent signal labels:

| Label | Description | Color |
|-------|-------------|-------|
| `agent:ready` | Agent ready - prepared for autonomous work | Green |
| `agent:in-progress` | Agent in progress - actively being worked | Blue |
| `requires:human-decision` | Requires human decision | Red-orange |
| `stale` | No activity >5 days | Yellow |
| `needs-fix` | PR checks failed | Light yellow |
| `blocked:external` | Blocked by third-party dependency | Red |
| `breaking:api-change` | Breaking API change | Orange |
| `requires:testing` | Requires testing - Complexity ≥5 | Purple |

**Features:**
- Idempotent: Safe to run multiple times
- Color-coded output with success/warning/error indicators
- Automatic detection of existing labels
- Summary report at completion

**Documentation:**

See [docs/label-system.md](../docs/label-system.md) for complete label system documentation, including:
- Label semantics and usage guidelines
- Existing priority and component labels
- Color coding conventions
- Automation behavior

## MCP Server

See [mcp-server/](./mcp-server/) for Model Context Protocol server implementation.
