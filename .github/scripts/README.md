# Agent Flow Migration Scripts

This directory contains scripts for migrating open issues to the Agent Flow continuous flow system.

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
   - **Priority**: Inferred from P0/P1/P2 labels (defaults to P3)
   - **Complexity**: Calculated from "Tech Design Needed" label and body length (1-13 scale)
   - **Component**: Inferred from labels (services, infrastructure, testing, views, docs, etc.)
   - **Agent Type**: Based on complexity, labels, and body content
   - **Dependencies**: Parsed from "Depends on" or "Blocked by" patterns in issue body
   - **Last Activity**: Set from issue's updated_at date
   - **Status**: All issues start in "Backlog"
3. **Adds issues to the project** if not already present
4. **Updates all custom fields** for each issue
5. **Provides a summary report** showing counts by priority, component, and agent type

## Configuration

The script uses `project-config.yml` which is a copy of the root `.github/project-config.yml`. This file maps to the field names in the GitHub Project:

**Note**: This is a local copy for the migration script. If the root config changes, you may need to update this copy or use the root config directly by modifying the script's config path.

- **status** - Status
- **priority** - Priority
- **complexity** - Complexity
- **agent_type** - Agent Type
- **component** - Component
- **dependencies** - Dependencies
- **last_activity** - Last Activity
- **pr_link** - PR Link

## Field Inference Logic

### Priority
- `P0` label → P0 (Critical)
- `P1` label → P1 (High)
- `P2` label → P2 (Medium)
- No label → P3 (Low) - default

### Complexity
- Has "Tech Design Needed" label → 8
- Body length > 3000 chars → 5
- Body length > 1500 chars → 3
- Body length > 500 chars → 2
- Otherwise → 1

### Component
Maps labels to components:
- `services`, `voice` → services
- `mcp-proxy`, `architecture`, `infrastructure` → infrastructure
- `testing` → testing
- `ui`, `ux` → views
- `docs`, `documentation` → docs
- No labels → unclassified
- Labels exist but don't match above → services

### Agent Type
- Has "Tech Design Needed" or "requires:human-decision" label → Human Required
- Has "testing" label → Any Agent
- Body mentions "Copilot" → Copilot
- Body mentions "Claude" → Copilot + Thinking
- Body mentions "Task-Maestro" → Task-Maestro
- Complexity ≤ 3 → Copilot
- Otherwise → Copilot + Thinking

### Dependencies
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
- **`graphql-client.mjs`** - GitHub GraphQL API client with automatic pagination support
- **`graphql-client.test.mjs`** - Test suite for GraphQL client pagination
- **`project-config.yml`** - Project field configuration
- **`package.json`** - npm package definition
- **`.gitignore`** - Excludes node_modules from git

## Testing

Run the test suite:

```bash
npm test
```

The test suite validates:
- Pagination through projects with >100 items
- Finding items across multiple pages
- Handling edge cases (empty projects, exactly 100 items, etc.)

## GraphQL Client Features

The `graphql-client.mjs` module provides:
- **Automatic pagination**: `getProjectItemId` automatically iterates through all project items using cursor-based pagination (100 items per page)
- **Backward compatibility**: Existing code works without modifications
- **Comprehensive error handling**: Detailed error messages for failed operations

## Related Issues

- #90 - Create main migration script (this issue)
- #91 - Create issue-to-field inference logic module
- #92 - Create GraphQL mutation module for project field updates
- #76 - Parent issue: Migration script and migrate all 32 open issues

## Validation Test
This line validates Agent Flow automation.
