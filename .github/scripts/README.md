# Nsumankwahene Migration Scripts

This directory contains scripts for migrating open issues to the Nsumankwahene (Spiritual Guardian) continuous flow system.

## Prerequisites

1. **Node.js** (version 18 or higher recommended)
2. **GitHub Personal Access Token** with the following permissions:
   - `repo` (full repository access)
   - `project` (full project access)

## Setup

1. Navigate to the scripts directory:
   ```bash
   cd .github/scripts
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Set your GitHub token as an environment variable:
   ```bash
   export GITHUB_TOKEN=your_github_token_here
   ```

## Usage

### Dry Run (Preview Only)

Preview what the migration would do without making any changes:

```bash
npm run dry-run
```

### Single Issue Test

Test the migration on a single issue before running the full migration:

```bash
node migrate-open-issues.mjs --issue 90
```

### Full Migration

Run the complete migration of all open issues:

```bash
npm run migrate
```

## What the Migration Does

The migration script:

1. **Fetches all open issues** from the repository (excluding pull requests)
2. **Infers field values** from existing issue data:
   - **Priority (Tumi)**: Inferred from P0/P1/P2 labels (defaults to P3)
   - **Complexity (Mu)**: Calculated from "Tech Design Needed" label and body length (1-13 scale)
   - **Component (Fapem)**: Inferred from labels (services, infrastructure, testing, views, docs, etc.)
   - **Agent Type (Ɔkyeame)**: Based on complexity, labels, and body content
   - **Dependencies (Nkabom)**: Parsed from "Depends on" or "Blocked by" patterns in issue body
   - **Last Activity (Da-Akyire)**: Set from issue's updated_at date
   - **Status (Nsoromma)**: All issues start in "Afiase" (Backlog)
3. **Adds issues to the project** if not already present
4. **Updates all custom fields** for each issue
5. **Provides a summary report** showing counts by priority, component, and agent type

## Configuration

The script uses `project-config.yml` which is a copy of the root `.github/project-config.yml`. This file maps to the Akan field names in the GitHub Project:

**Note**: This is a local copy for the migration script. If the root config changes, you may need to update this copy or use the root config directly by modifying the script's config path.

- **nsoromma** - Status (Nsoromma)
- **tumi** - Priority (Tumi)  
- **mu** - Complexity (Mu)
- **okyeame** - Agent Type (Ɔkyeame)
- **fapem** - Component (Fapem)
- **nkabom** - Dependencies (Nkabom)
- **da_akyire** - Last Activity (Da-Akyire)
- **pr_nkitahodi** - PR Link (PR Nkitahodi)

## Field Inference Logic

### Priority (Tumi)
- `P0` label → `p0_hene` (Critical)
- `P1` label → `p1_abusuapanyin` (High)
- `P2` label → `p2_obi` (Medium)
- No label → `p3_akwadaa` (Low) - default

### Complexity (Mu)
- Has "Tech Design Needed" label → 8
- Body length > 3000 chars → 5
- Body length > 1500 chars → 3
- Body length > 500 chars → 2
- Otherwise → 1

### Component (Fapem)
Maps labels to components:
- `services`, `voice` → services
- `mcp-proxy`, `architecture`, `infrastructure` → infrastructure
- `testing` → testing
- `ui`, `ux` → views
- `docs`, `documentation` → docs
- No labels → unclassified
- Labels exist but don't match above → services

### Agent Type (Ɔkyeame)
- Has "Tech Design Needed" or "nnipa-gyinae-hia" label → `nnipa_hia` (Human Required)
- Has "testing" label → `biara` (Any Agent)
- Body mentions "Copilot" → `copilot`
- Body mentions "Claude" → `claude_opus`
- Body mentions "Task-Maestro" → `task_maestro`
- Complexity ≤ 3 → `copilot`
- Otherwise → `claude_opus`

### Dependencies (Nkabom)
Searches issue body for patterns:
- "Depends on: #123"
- "Blocked by: #456"
- "Blocking #789"

Returns comma-separated list or "None" if no dependencies found.

## Rate Limiting

The script applies a 500ms delay only after failed issue update attempts to respect GitHub API rate limits. Successful updates have no delay between them, relying on GitHub's rate limit tolerance for normal operations.

## Troubleshooting

### Error: GITHUB_TOKEN environment variable required

Make sure you've exported your GitHub token:
```bash
export GITHUB_TOKEN=your_token_here
```

### Error: project-config.yml contains placeholder IDs

The `project-config.yml` must have real field IDs from your GitHub Project. The file should not contain `REPLACE_WITH` placeholders.

### Error: Issue not found or not open

The issue number you specified with `--issue` doesn't exist or is closed.

## Files

- **`migrate-open-issues.mjs`** - Main migration script
- **`infer-fields.mjs`** - Issue field inference logic
- **`graphql-client.mjs`** - GitHub GraphQL API client
- **`project-config.yml`** - Project field configuration
- **`package.json`** - npm package definition
- **`.gitignore`** - Excludes node_modules from git

## Related Issues

- #90 - Create main migration script (this issue)
- #91 - Create issue-to-field inference logic module
- #92 - Create GraphQL mutation module for project field updates
- #76 - Parent issue: Migration script and migrate all 32 open issues
