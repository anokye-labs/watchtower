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

// Update individual fields
await client.updateSingleSelect(itemId, fieldId, optionId);
await client.updateNumber(itemId, fieldId, 5);
await client.updateText(itemId, fieldId, 'Some text');
await client.updateDate(itemId, fieldId, '2025-01-15');

// Batch update all fields
const fields = {
  status: 'afiase',
  priority: 'p1_abusuapanyin',
  complexity: 3,
  component: 'services',
  agent_type: 'copilot',
  dependencies: '#1, #2',
  last_activity: '2025-12-29',
  pr_link: 'https://github.com/...'
};

const config = {
  fields: {
    nsoromma: { id: 'PVTF_...', options: { afiase: 'PVTSSOO_...' } },
    tumi: { id: 'PVTF_...', options: { p1_abusuapanyin: 'PVTSSOO_...' } },
    mu: { id: 'PVTF_...' },
    fapem: { id: 'PVTF_...', options: { services: 'PVTSSOO_...' } },
    okyeame: { id: 'PVTF_...', options: { copilot: 'PVTSSOO_...' } },
    nkabom: { id: 'PVTF_...' },
    da_akyire: { id: 'PVTF_...' },
    pr_nkitahodi: { id: 'PVTF_...' }
  }
};

await client.updateAllFields(itemId, fields, config);

// Rate limiting
await delay(1000); // Wait 1 second between API calls
```

## Methods

### `getProjectItemId(issueNodeId)`
Returns the project item ID for an issue, or null if not in project.

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
Uses `Promise.allSettled()` to handle individual failures gracefully.
Throws an error if any field update fails, with details logged to console.

### `delay(ms)`
Promise-based delay for rate limiting.

## Error Handling

The `updateAllFields` method uses `Promise.allSettled()` to attempt all field updates even if some fail. If any updates fail, the method will:
1. Log details of each failure to console.error
2. Throw an error indicating how many updates failed

Example error output:
```
2 field update(s) failed:
  - Update 1: GraphQL error message
  - Update 3: Network error
Error: Failed to update 2 field(s)
```

## Limitations

- `getProjectItemId` only fetches the first 100 project items. For larger projects (>100 items), issues beyond this limit may not be found and will need to be added via `addIssueToProject`.

## Field Mapping

See `.github/project-config.yml` for field IDs and option IDs.

Akan field names:
- `nsoromma` - Status
- `tumi` - Priority
- `mu` - Complexity
- `okyeame` - Agent Type
- `fapem` - Component
- `nkabom` - Dependencies
- `da_akyire` - Last Activity
- `pr_nkitahodi` - PR Link
