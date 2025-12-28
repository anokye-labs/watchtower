# WatchTower Tools

This directory contains utility scripts and tools for repository management and development workflows.

## Label Management

### create-labels.sh

Creates semantic labels with Akan names for the Nsumankwahene Flow System (agent-driven development).

**Prerequisites:**
- `gh` CLI installed ([installation guide](https://cli.github.com/manual/installation))
- Authenticated with GitHub: `gh auth login`
- Repository admin or maintainer permissions

**Usage:**

```bash
# From repository root
./tools/create-labels.sh
```

**What it creates:**

The script creates 8 agent signal labels:

| Label | Description | Color |
|-------|-------------|-------|
| `ɔkyeame:siesie` | Agent ready - prepared for autonomous work | Green |
| `ɔkyeame:dwuma` | Agent in progress - actively being worked | Blue |
| `nnipa-gyinae-hia` | Requires human decision | Red-orange |
| `stale` | No activity >5 days | Yellow |
| `needs-fix` | PR checks failed | Light yellow |
| `asiw:external` | Blocked by third-party dependency | Red |
| `nsakrae:api` | Breaking API change | Orange |
| `nhwɛsoɔ-hia` | Requires testing - Complexity ≥5 | Purple |

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
- Akan name pronunciations
- Automation behavior

## MCP Server

See [mcp-server/](./mcp-server/) for Model Context Protocol server implementation.
