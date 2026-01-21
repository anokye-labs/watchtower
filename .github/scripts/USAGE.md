# GraphQL Client Usage Guide

## Installation

```bash
cd .github/scripts
npm install
```

## Basic Usage

```javascript
import { ProjectGraphQLClient, delay } from './graphql-client.mjs';

// Initialize client
const client = new ProjectGraphQLClient(
  process.env.GITHUB_TOKEN,
  'PVT_kwDONc6RBc4A0MJL' // Project ID from project-config.yml
);

// Get or add issue to project
const issueNodeId = 'I_kwDONc6RBc4...' // Issue node_id
let itemId = await client.getProjectItemId(issueNodeId);

if (!itemId) {
  itemId = await client.addIssueToProject(issueNodeId);
  console.log('Added issue to project');
}

// Define config first (typically loaded from project-config.yml)
const config = {
  fields: {
    status: { id: 'PVTF_...', options: { backlog: 'PVTSSOO_...' } },
    priority: { id: 'PVTF_...', options: { p1: 'PVTSSOO_...' } },
    complexity: { id: 'PVTF_...' },
    component: { id: 'PVTF_...', options: { services: 'PVTSSOO_...' } },
    agent_type: { id: 'PVTF_...', options: { copilot: 'PVTSSOO_...' } },
    dependencies: { id: 'PVTF_...' },
    last_activity: { id: 'PVTF_...' },
    pr_link: { id: 'PVTF_...' }
  }
};

// Update individual fields (using values from config)
const fieldId = config.fields.status.id;
const optionId = config.fields.status.options.backlog;
await client.updateSingleSelect(itemId, fieldId, optionId);
await client.updateNumber(itemId, config.fields.complexity.id, 5);
await client.updateText(itemId, config.fields.dependencies.id, 'Some text');
await client.updateDate(itemId, config.fields.last_activity.id, '2025-01-15');

// Batch update all fields
const fields = {
  status: 'backlog',
  priority: 'p1',
  complexity: 3,
  component: 'services',
  agent_type: 'copilot',
  dependencies: '#1, #2',
  last_activity: '2025-12-29',
  pr_link: 'https://github.com/...'
};

await client.updateAllFields(itemId, fields, config);
```

## Methods

### `getProjectItemId(issueNodeId)`
Returns the project item ID for an issue, or null if not in project.

**Pagination Support**: This method automatically paginates through all project items using cursor-based pagination. It will iterate through pages of 100 items each until the issue is found or all items have been checked.

### `addIssueToProject(issueNodeId)`
Adds an issue to the project and returns the project item ID.

### `updateSingleSelect(itemId, fieldId, optionId)`
Updates a single-select field (Status, Priority, Component, Agent Type).

### `updateNumber(itemId, fieldId, value)`
Updates a number field (Complexity).

### `updateText(itemId, fieldId, text)`
Updates a text field (Dependencies, PR Link).

### `updateDate(itemId, fieldId, date)`
Updates a date field (Last Activity). Date must be ISO format (YYYY-MM-DD).

### `updateAllFields(itemId, fields, config)`
Batch updates all fields for an issue using the field values and config.
Updates are executed sequentially with a 100ms delay between each to avoid rate limits.
Uses `Promise.allSettled()` to attempt all updates even if some fail.
Throws an error if any field update fails, with details logged to console.

### `delay(ms)`
Promise-based delay for rate limiting. Used internally by `updateAllFields`.

## Error Handling

The `updateAllFields` method attempts all field updates sequentially. If any updates fail, the method will:
1. Log details of each failure to console.error
2. Throw an error indicating how many updates failed

Example error output:
```
2 field update(s) failed:
  - Update 1: GraphQL error message
  - Update 3: Network error
Error: Failed to update 2 field(s)
```

## Validation

The module performs validation on inputs:
- Constructor validates token and projectId are non-empty strings
- Complexity field must be >= 1
- Date fields must be in ISO format (YYYY-MM-DD)
- Text fields must be non-empty after trimming
- Empty strings are filtered out to avoid unnecessary API calls

## Testing

Run the test suite with:

```bash
npm test
```

Tests cover:
- Pagination through multiple pages (>100 items)
- Finding items on first and subsequent pages
- Handling projects with exactly 100 items
- Empty projects
- Items not found after checking all pages
- Early termination when item is found

## Field Mapping

See `.github/project-config.yml` for field IDs and option IDs.

Field names:
- `status` - Status
- `priority` - Priority
- `complexity` - Complexity
- `agent_type` - Agent Type
- `component` - Component
- `dependencies` - Dependencies
- `last_activity` - Last Activity
- `pr_link` - PR Link
