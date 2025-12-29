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
      query GetProjectItem($projectId: ID!, $issueId: ID!) {
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
      projectId: this.projectId,
      issueId: issueNodeId
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
    
    // Status (single-select)
    if (config.fields.status?.options?.[fields.status]) {
      updates.push(this.updateSingleSelect(
        itemId,
        config.fields.status.id,
        config.fields.status.options[fields.status]
      ));
    }
    
    // Priority (single-select)
    if (config.fields.priority?.options?.[fields.priority]) {
      updates.push(this.updateSingleSelect(
        itemId,
        config.fields.priority.id,
        config.fields.priority.options[fields.priority]
      ));
    }
    
    // Complexity (number)
    if (config.fields.complexity?.id) {
      updates.push(this.updateNumber(
        itemId,
        config.fields.complexity.id,
        fields.complexity
      ));
    }
    
    // Component (single-select)
    if (config.fields.component?.options?.[fields.component]) {
      updates.push(this.updateSingleSelect(
        itemId,
        config.fields.component.id,
        config.fields.component.options[fields.component]
      ));
    }
    
    // Agent Type (single-select)
    if (config.fields.agent_type?.options?.[fields.agent_type]) {
      updates.push(this.updateSingleSelect(
        itemId,
        config.fields.agent_type.id,
        config.fields.agent_type.options[fields.agent_type]
      ));
    }
    
    // Dependencies (text)
    if (config.fields.dependencies?.id) {
      updates.push(this.updateText(
        itemId,
        config.fields.dependencies.id,
        fields.dependencies
      ));
    }
    
    // Last Activity (date)
    if (config.fields.last_activity?.id) {
      updates.push(this.updateDate(
        itemId,
        config.fields.last_activity.id,
        fields.last_activity
      ));
    }
    
    // PR Link (text) - skip if empty
    if (config.fields.pr_link?.id && fields.pr_link) {
      updates.push(this.updateText(
        itemId,
        config.fields.pr_link.id,
        fields.pr_link
      ));
    }
    
    // Execute all updates in parallel (within rate limits)
    await Promise.all(updates);
  }
}

/**
 * Rate limiter to avoid GitHub API limits
 * @param {number} ms - Milliseconds to wait
 */
export function delay(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}
