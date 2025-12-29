#!/usr/bin/env node
/**
 * GraphQL Client for GitHub Projects V2
 * Handles all GraphQL mutations for project field updates
 */

import { Octokit } from '@octokit/rest';

/**
 * GraphQL client for GitHub Projects V2
 */
export class ProjectGraphQLClient {
  constructor(token, projectId) {
    this.octokit = new Octokit({ auth: token });
    this.projectId = projectId;
  }
  
  /**
   * Get project item ID for an issue
   * @param {string} issueNodeId - The node_id of the issue
   * @returns {Promise<string|null>} Project item ID or null if not in project
   */
  async getProjectItemId(issueNodeId) {
    const query = `
      query GetProjectItem($projectId: ID!, $cursor: String) {
        node(id: $projectId) {
          ... on ProjectV2 {
            items(first: 100, after: $cursor) {
              pageInfo {
                hasNextPage
                endCursor
              }
              nodes {
                id
                content {
                  ... on Issue {
                    id
                  }
                }
              }
            }
          }
        }
      }
    `;
    
    let hasNextPage = true;
    let cursor = null;
    
    while (hasNextPage) {
      const result = await this.octokit.graphql(query, {
        projectId: this.projectId,
        cursor
      });
      
      const items = result.node?.items?.nodes || [];
      const item = items.find(i => i.content?.id === issueNodeId);
      if (item) {
        return item.id;
      }
      
      hasNextPage = result.node?.items?.pageInfo?.hasNextPage || false;
      cursor = result.node?.items?.pageInfo?.endCursor;
    }
    
    return null;
  }
  
  /**
   * Add issue to project if not already present
   * @param {string} issueNodeId - The node_id of the issue
   * @returns {Promise<string>} Project item ID
   */
  async addIssueToProject(issueNodeId) {
    const mutation = `
      mutation AddToProject($projectId: ID!, $contentId: ID!) {
        addProjectV2ItemById(input: {
          projectId: $projectId
          contentId: $contentId
        }) {
          item {
            id
          }
        }
      }
    `;
    
    const result = await this.octokit.graphql(mutation, {
      projectId: this.projectId,
      contentId: issueNodeId
    });
    
    return result.addProjectV2ItemById.item.id;
  }
  
  /**
   * Update a single-select field
   * @param {string} itemId - Project item ID
   * @param {string} fieldId - Field ID
   * @param {string} optionId - Option ID to select
   */
  async updateSingleSelect(itemId, fieldId, optionId) {
    const mutation = `
      mutation UpdateSingleSelect($projectId: ID!, $itemId: ID!, $fieldId: ID!, $optionId: String!) {
        updateProjectV2ItemFieldValue(input: {
          projectId: $projectId
          itemId: $itemId
          fieldId: $fieldId
          value: { singleSelectOptionId: $optionId }
        }) {
          projectV2Item { id }
        }
      }
    `;
    
    await this.octokit.graphql(mutation, {
      projectId: this.projectId,
      itemId,
      fieldId,
      optionId
    });
  }
  
  /**
   * Update a number field
   * @param {string} itemId - Project item ID
   * @param {string} fieldId - Field ID
   * @param {number} value - Number value
   */
  async updateNumber(itemId, fieldId, value) {
    const mutation = `
      mutation UpdateNumber($projectId: ID!, $itemId: ID!, $fieldId: ID!, $value: Float!) {
        updateProjectV2ItemFieldValue(input: {
          projectId: $projectId
          itemId: $itemId
          fieldId: $fieldId
          value: { number: $value }
        }) {
          projectV2Item { id }
        }
      }
    `;
    
    await this.octokit.graphql(mutation, {
      projectId: this.projectId,
      itemId,
      fieldId,
      value
    });
  }
  
  /**
   * Update a text field
   * @param {string} itemId - Project item ID
   * @param {string} fieldId - Field ID
   * @param {string} text - Text value
   */
  async updateText(itemId, fieldId, text) {
    const mutation = `
      mutation UpdateText($projectId: ID!, $itemId: ID!, $fieldId: ID!, $text: String!) {
        updateProjectV2ItemFieldValue(input: {
          projectId: $projectId
          itemId: $itemId
          fieldId: $fieldId
          value: { text: $text }
        }) {
          projectV2Item { id }
        }
      }
    `;
    
    await this.octokit.graphql(mutation, {
      projectId: this.projectId,
      itemId,
      fieldId,
      text
    });
  }
  
  /**
   * Update a date field
   * @param {string} itemId - Project item ID
   * @param {string} fieldId - Field ID
   * @param {string} date - ISO date string (YYYY-MM-DD)
   */
  async updateDate(itemId, fieldId, date) {
    const mutation = `
      mutation UpdateDate($projectId: ID!, $itemId: ID!, $fieldId: ID!, $date: Date!) {
        updateProjectV2ItemFieldValue(input: {
          projectId: $projectId
          itemId: $itemId
          fieldId: $fieldId
          value: { date: $date }
        }) {
          projectV2Item { id }
        }
      }
    `;
    
    await this.octokit.graphql(mutation, {
      projectId: this.projectId,
      itemId,
      fieldId,
      date
    });
  }
  
  /**
   * Update all fields for an issue using config
   * @param {string} itemId - Project item ID
   * @param {Object} fields - Inferred field values
   * @param {Object} config - Project config with field IDs
   */
  async updateAllFields(itemId, fields, config) {
    const updates = [];
    
    const logFieldSkip = (fieldName, reason) => {
      console.warn(`  ⚠️  Skipping ${fieldName}: ${reason}`);
    };
    
    // Nsoromma (Status) - single-select
    const nsorommaField = config.fields.nsoromma;
    if (nsorommaField?.options?.[fields.status]) {
      updates.push({
        name: 'Status',
        promise: this.updateSingleSelect(
          itemId,
          nsorommaField.id,
          nsorommaField.options[fields.status]
        )
      });
    } else if (fields.status) {
      if (!nsorommaField?.options) {
        logFieldSkip('Status', 'config missing or no options defined');
      } else {
        logFieldSkip('Status', `no option found for value "${fields.status}"`);
      }
    }
    
    // Tumi (Priority) - single-select
    const tumiField = config.fields.tumi;
    if (tumiField?.options?.[fields.priority]) {
      updates.push({
        name: 'Priority',
        promise: this.updateSingleSelect(
          itemId,
          tumiField.id,
          tumiField.options[fields.priority]
        )
      });
    } else if (fields.priority) {
      if (!tumiField?.options) {
        logFieldSkip('Priority', 'config missing or no options defined');
      } else {
        logFieldSkip('Priority', `no option found for value "${fields.priority}"`);
      }
    }
    
    // Mu (Complexity) - number
    if (config.fields.mu?.id) {
      updates.push({
        name: 'Complexity',
        promise: this.updateNumber(
          itemId,
          config.fields.mu.id,
          fields.complexity
        )
      });
    } else if (fields.complexity != null) {
      logFieldSkip('Complexity', 'field ID not configured');
    }
    
    // Fapem (Component) - single-select
    const fapemField = config.fields.fapem;
    if (fapemField?.options?.[fields.component]) {
      updates.push({
        name: 'Component',
        promise: this.updateSingleSelect(
          itemId,
          fapemField.id,
          fapemField.options[fields.component]
        )
      });
    } else if (fields.component) {
      if (!fapemField?.options) {
        logFieldSkip('Component', 'config missing or no options defined');
      } else {
        logFieldSkip('Component', `no option found for value "${fields.component}"`);
      }
    }
    
    // Ɔkyeame (Agent Type) - single-select
    const okyeameField = config.fields.okyeame;
    if (okyeameField?.options?.[fields.agent_type]) {
      updates.push({
        name: 'Agent Type',
        promise: this.updateSingleSelect(
          itemId,
          okyeameField.id,
          okyeameField.options[fields.agent_type]
        )
      });
    } else if (fields.agent_type) {
      if (!okyeameField?.options) {
        logFieldSkip('Agent Type', 'config missing or no options defined');
      } else {
        logFieldSkip('Agent Type', `no option found for value "${fields.agent_type}"`);
      }
    }
    
    // Nkabom (Dependencies) - text
    if (config.fields.nkabom?.id) {
      updates.push({
        name: 'Dependencies',
        promise: this.updateText(
          itemId,
          config.fields.nkabom.id,
          fields.dependencies
        )
      });
    } else if (fields.dependencies && fields.dependencies !== 'None') {
      logFieldSkip('Dependencies', 'field ID not configured');
    }
    
    // Da-Akyire (Last Activity) - date
    if (config.fields.da_akyire?.id) {
      updates.push({
        name: 'Last Activity',
        promise: this.updateDate(
          itemId,
          config.fields.da_akyire.id,
          fields.last_activity
        )
      });
    } else if (fields.last_activity) {
      logFieldSkip('Last Activity', 'field ID not configured');
    }
    
    // PR Nkitahodi (PR Link) - text - skip if empty
    if (config.fields.pr_nkitahodi?.id && fields.pr_link) {
      updates.push({
        name: 'PR Link',
        promise: this.updateText(
          itemId,
          config.fields.pr_nkitahodi.id,
          fields.pr_link
        )
      });
    }
    
    // Check if we have any updates to perform
    if (updates.length === 0) {
      console.warn(`  ⚠️  No field updates queued for item ${itemId}. Check project config and inferred values.`);
      return;
    }
    
    // Execute all updates with Promise.allSettled to allow partial success
    const results = await Promise.allSettled(updates.map(u => u.promise));
    
    // Report any failures
    const failures = results
      .map((result, index) => ({ result, name: updates[index].name }))
      .filter(({ result }) => result.status === 'rejected');
    
    if (failures.length > 0) {
      const errors = failures.map(({ name, result }) => `${name}: ${result.reason.message}`).join('\n  - ');
      throw new Error(`Failed to update ${failures.length} field(s):\n  - ${errors}`);
    }
  }
}

/**
 * Rate limiter to avoid GitHub API limits
 * @param {number} ms - Milliseconds to wait
 */
export function delay(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}
