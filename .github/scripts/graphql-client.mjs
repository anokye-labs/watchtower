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
    if (typeof token !== 'string' || token.trim() === '') {
      throw new Error('GitHub token is required and must be a non-empty string.');
    }
    if (typeof projectId !== 'string' || projectId.trim() === '') {
      throw new Error('projectId is required and must be a non-empty string.');
    }
    this.octokit = new Octokit({ auth: token });
    this.projectId = projectId;
  }
  
  /**
   * Get project item ID for an issue
   * @param {string} issueNodeId - The node_id of the issue
   * @returns {Promise<string|null>} Project item ID or null if not in project
   * @note This method fetches the first 100 items. For larger projects, the issue may not be found.
   */
  async getProjectItemId(issueNodeId) {
    const query = `
      query GetProjectItem($projectId: ID!) {
        node(id: $projectId) {
          ... on ProjectV2 {
            items(first: 100) {
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
    
    const result = await this.octokit.graphql(query, {
      projectId: this.projectId
    });
    
    const items = result.node?.items?.nodes || [];
    const item = items.find(i => i.content?.id === issueNodeId);
    return item?.id || null;
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
    
    const itemId = result?.addProjectV2ItemById?.item?.id;
    if (!itemId) {
      throw new Error('Failed to add issue to project: missing item id in GraphQL response.');
    }
    return itemId;
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
    
    // Status (single-select) - nsoromma
    if (config.fields.nsoromma?.options?.[fields.status] && fields.status) {
      updates.push(this.updateSingleSelect(
        itemId,
        config.fields.nsoromma.id,
        config.fields.nsoromma.options[fields.status]
      ));
    }
    
    // Priority (single-select) - tumi
    if (config.fields.tumi?.options?.[fields.priority] && fields.priority) {
      updates.push(this.updateSingleSelect(
        itemId,
        config.fields.tumi.id,
        config.fields.tumi.options[fields.priority]
      ));
    }
    
    // Complexity (number) - mu
    if (
      config.fields.mu?.id &&
      typeof fields.complexity === 'number' &&
      !isNaN(fields.complexity) &&
      fields.complexity >= 1
    ) {
      updates.push(this.updateNumber(
        itemId,
        config.fields.mu.id,
        fields.complexity
      ));
    }
    
    // Component (single-select) - fapem
    if (config.fields.fapem?.options?.[fields.component] && fields.component) {
      updates.push(this.updateSingleSelect(
        itemId,
        config.fields.fapem.id,
        config.fields.fapem.options[fields.component]
      ));
    }
    
    // Agent Type (single-select) - okyeame
    if (config.fields.okyeame?.options?.[fields.agent_type] && fields.agent_type) {
      updates.push(this.updateSingleSelect(
        itemId,
        config.fields.okyeame.id,
        config.fields.okyeame.options[fields.agent_type]
      ));
    }
    
    // Dependencies (text) - nkabom
    if (config.fields.nkabom?.id && fields.dependencies && String(fields.dependencies).trim()) {
      updates.push(this.updateText(
        itemId,
        config.fields.nkabom.id,
        fields.dependencies
      ));
    }
    
    // Last Activity (date) - da_akyire
    if (config.fields.da_akyire?.id && fields.last_activity) {
      const lastActivity = String(fields.last_activity).trim();
      const isoDateRegex = /^\d{4}-\d{2}-\d{2}$/;
      if (!isoDateRegex.test(lastActivity) || isNaN(Date.parse(lastActivity))) {
        throw new Error(`Invalid last_activity date format: "${fields.last_activity}". Expected ISO format YYYY-MM-DD.`);
      }
      updates.push(this.updateDate(
        itemId,
        config.fields.da_akyire.id,
        lastActivity
      ));
    }
    
    // PR Link (text) - pr_nkitahodi
    if (config.fields.pr_nkitahodi?.id && fields.pr_link && String(fields.pr_link).trim()) {
      updates.push(this.updateText(
        itemId,
        config.fields.pr_nkitahodi.id,
        fields.pr_link
      ));
    }
    
    // Execute all updates with rate limiting between batches
    // Use try-catch to handle individual failures gracefully
    // Add a small delay between updates to avoid rate limits
    const RATE_LIMIT_DELAY = 100; // milliseconds between updates
    const results = [];
    
    for (let i = 0; i < updates.length; i++) {
      try {
        await updates[i];
        results.push({ status: 'fulfilled' });
      } catch (error) {
        results.push({ status: 'rejected', reason: error });
      }
      
      // Add delay between updates (but not after the last one)
      if (i < updates.length - 1) {
        await delay(RATE_LIMIT_DELAY);
      }
    }
    
    // Check for any failures
    const failures = results.filter(r => r.status === 'rejected');
    if (failures.length > 0) {
      console.error(`${failures.length} field update(s) failed:`);
      failures.forEach((f, i) => {
        console.error(`  - Update ${i + 1}: ${f.reason?.message || f.reason}`);
      });
      throw new Error(`Failed to update ${failures.length} field(s)`);
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
