# WatchTower Label System

Part of the Nsumankwahene Flow System for agent-driven development.

## Overview

WatchTower uses a semantic label system with Akan names to track issue lifecycle, agent signals, and workflow state. This system enables both human and autonomous agent coordination.

## Label Categories

### Agent Signal Labels

These labels indicate agent readiness and workflow state:

| Label | English | Description | Color | Usage |
|-------|---------|-------------|-------|-------|
| `ɔkyeame:siesie` | agent:ready | Prepared for autonomous work, dependencies verified | ![#0E8A16](https://via.placeholder.com/15/0E8A16/000000?text=+) `#0E8A16` (green) | Applied when issue is fully specified and ready for agent pickup |
| `ɔkyeame:dwuma` | agent:in-progress | Actively being worked by agent | ![#1D76DB](https://via.placeholder.com/15/1D76DB/000000?text=+) `#1D76DB` (blue) | Agent applies when starting work on an issue |
| `nnipa-gyinae-hia` | requires:human-decision | Agent cannot proceed without human input | ![#D93F0B](https://via.placeholder.com/15/D93F0B/000000?text=+) `#D93F0B` (red-orange) | Agent applies when blocked and needs human decision |
| `stale` | stale | No activity >5 days | ![#FBCA04](https://via.placeholder.com/15/FBCA04/000000?text=+) `#FBCA04` (yellow) | Automation applies when issue inactive for 5+ days |
| `needs-fix` | needs-fix | PR checks failed | ![#E4E669](https://via.placeholder.com/15/E4E669/000000?text=+) `#E4E669` (light yellow) | Automation applies when CI/PR checks fail |
| `ci-fail:1` | ci:first-failure | First consecutive CI failure | ![#FEF2C0](https://via.placeholder.com/15/FEF2C0/000000?text=+) `#FEF2C0` (pale yellow) | Automation applies on first CI failure |
| `ci-fail:2` | ci:second-failure | Second consecutive CI failure, triggers Blocked | ![#FBCA04](https://via.placeholder.com/15/FBCA04/000000?text=+) `#FBCA04` (yellow) | Automation applies on second consecutive CI failure |
| `ci-fail:3+` | ci:multiple-failures | Three or more consecutive CI failures | ![#D93F0B](https://via.placeholder.com/15/D93F0B/000000?text=+) `#D93F0B` (red-orange) | Automation applies on third+ consecutive CI failure |
| `asiw:external` | blocked:external | Waiting on third-party dependency | ![#B60205](https://via.placeholder.com/15/B60205/000000?text=+) `#B60205` (red) | Applied when blocked by external dependency |
| `nsakrae:api` | breaking:api-change | Major version bump required | ![#D93F0B](https://via.placeholder.com/15/D93F0B/000000?text=+) `#D93F0B` (red-orange) | Applied when changes break public API |
| `nhwɛsoɔ-hia` | requires:testing | Must include unit tests (Complexity ≥5) | ![#5319E7](https://via.placeholder.com/15/5319E7/000000?text=+) `#5319E7` (purple) | Applied to complex issues requiring test coverage |

### Priority Labels (Tumi Field)

Existing labels that map to priority:

- `P0` - Critical: System down or major security issue
- `P1` - High: Major feature or bug affecting many users
- `P2` - Medium: Standard feature or bug
- `P3` - Low: Nice-to-have or minor issue

### Component Labels (Fapem Field)

Existing labels that map to components:

- `services` - Service layer changes
- `viewmodels` - ViewModel changes
- `testing` - Test infrastructure
- `mcp-proxy` - MCP proxy service
- `voice` - Voice integration
- `ui` - User interface
- `ux` - User experience
- `tech-debt` - Technical debt
- `cross-platform` - Cross-platform compatibility
- `architecture` - Architectural changes

### Workflow Labels

- `Tech Design Needed` - Requires technical design (maps to Ɔkyeame = Nnipa-Hia)

## Creating Labels

### Using the Script

The repository includes a script to create all agent signal labels:

```bash
# Make sure gh CLI is installed and authenticated
gh auth status

# Preview what would be created (recommended first step)
./tools/create-labels.sh --dry-run

# Create the labels
./tools/create-labels.sh
```

### Manual Creation

To create labels manually using the gh CLI:

```bash
# Create agent signal labels
gh label create "ɔkyeame:siesie" --description "Agent ready - prepared for autonomous work" --color "0E8A16"
gh label create "ɔkyeame:dwuma" --description "Agent in progress - actively being worked" --color "1D76DB"
gh label create "nnipa-gyinae-hia" --description "Requires human decision - agent cannot proceed" --color "D93F0B"
gh label create "stale" --description "No activity >5 days" --color "FBCA04"
gh label create "needs-fix" --description "PR checks failed" --color "E4E669"
gh label create "asiw:external" --description "Blocked by third-party dependency" --color "B60205"
gh label create "nsakrae:api" --description "Breaking API change - major version bump" --color "D93F0B"
gh label create "nhwɛsoɔ-hia" --description "Requires testing - Complexity ≥5" --color "5319E7"

# Create CI failure tracking labels
gh label create "ci-fail:1" --description "First consecutive CI failure" --color "FEF2C0"
gh label create "ci-fail:2" --description "Second consecutive CI failure - triggers Blocked status" --color "FBCA04"
gh label create "ci-fail:3+" --description "Three or more consecutive CI failures" --color "D93F0B"
```

## Color Coding

The label colors follow a semantic system:

- **Green** (`#0E8A16`): Ready state - work can proceed
- **Blue** (`#1D76DB`): Active work in progress
- **Red** (`#B60205`): Critical blocking issue
- **Red-Orange** (`#D93F0B`): Requires attention/decision
  - Used for both `nnipa-gyinae-hia` and `nsakrae:api` as both indicate critical situations requiring human attention
- **Yellow** (`#FBCA04`, `#E4E669`): Warning or needs action
- **Purple** (`#5319E7`): Quality requirements

**Note**: The red-orange color (`#D93F0B`) is intentionally shared between `nnipa-gyinae-hia` (human decision required) and `nsakrae:api` (breaking change) as both represent critical situations that require careful human consideration and cannot proceed automatically.

## Akan Names

The labels use Akan (Twi) language names to reflect the project's cultural foundation:

- `ɔkyeame` (oh-CHAY-ah-may) - spokesperson, messenger, agent
- `siesie` (see-eh-see-eh) - to prepare, to arrange
- `dwuma` (joo-mah) - work
- `nnipa-gyinae-hia` (nn-pah gee-nay hee-ah) - human decision needed
- `asiw` (ah-see-oo) - blockage
- `nsakrae` (nn-sah-krah-eh) - breaking, fundamental change
- `nhwɛsoɔ-hia` (nn-whay-so hee-ah) - examination/testing needed

## Automation

Some labels are automatically applied by GitHub Actions or agents:

- `stale` - Applied by automation when issue has no activity for 5+ days
- `needs-fix` - Applied by CI when PR checks fail, removed when checks pass
- `ci-fail:1`, `ci-fail:2`, `ci-fail:3+` - Automatically managed by CI integration to track consecutive failures
- `ɔkyeame:dwuma` - Applied by agent when starting work, removed when CI passes or PR merged
- `nnipa-gyinae-hia` - Applied by agent when blocked

**Note on CI Failure Labels:** The `ci-fail:*` labels are internal tracking labels that should never be manually added or removed. They are automatically:
- Added/incremented when CI fails consecutively
- Completely removed when CI passes
- Used to trigger automatic status transitions (e.g., to "Blocked" after 2 consecutive failures)

## Related Documentation

- [Nsumankwahene Flow System](#69) - Parent issue for agent workflow
- [GitHub Labels Documentation](https://docs.github.com/en/issues/using-labels-and-milestones-to-track-work/managing-labels)
