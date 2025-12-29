#!/usr/bin/env node
/**
 * Field Inference Module for Nsumankwahene Migration
 * Infers project field values from existing issue data
 */

/**
 * Infer Priority from existing labels
 * @param {string[]} labels - Array of label names
 * @returns {string} Priority option key (p0_critical, p1_high, p2_medium, p3_low)
 * 
 * Test cases:
 * - inferPriority(['P0', 'bug']) → 'p0_critical'
 * - inferPriority(['P1', 'enhancement']) → 'p1_high'
 * - inferPriority(['P2']) → 'p2_medium'
 * - inferPriority(['bug']) → 'p3_low' (default)
 */
export function inferPriority(labels) {
  if (labels.includes('P0')) return 'p0_critical';
  if (labels.includes('P1')) return 'p1_high';
  if (labels.includes('P2')) return 'p2_medium';
  return 'p3_low';  // Default
}

/**
 * Infer Complexity (1-13 Fibonacci scale)
 * @param {string[]} labels - Array of label names  
 * @param {string} body - Issue body text
 * @returns {number} Complexity score
 * 
 * Test cases:
 * - inferComplexity(['Tech Design Needed'], '') → 8
 * - inferComplexity(['tech-design-needed'], '') → 8
 * - inferComplexity([], 'x'.repeat(3500)) → 5 (long body)
 * - inferComplexity([], 'x'.repeat(1600)) → 3 (medium body)
 * - inferComplexity([], 'x'.repeat(600)) → 2 (short body)
 * - inferComplexity([], 'short') → 1 (very short)
 */
export function inferComplexity(labels, body) {
  // Tech Design Needed = high complexity
  if (labels.includes('Tech Design Needed') || labels.includes('tech-design-needed')) {
    return 8;
  }
  
  // Long body = more complex
  const bodyLength = body?.length || 0;
  if (bodyLength > 3000) return 5;
  if (bodyLength > 1500) return 3;
  if (bodyLength > 500) return 2;
  return 1;
}

/**
 * Infer Component from labels
 * @param {string[]} labels - Array of label names
 * @returns {string} Component option key
 * 
 * Test cases:
 * - inferComponent(['services']) → 'services'
 * - inferComponent(['voice']) → 'services'
 * - inferComponent(['mcp-proxy']) → 'infrastructure'
 * - inferComponent(['architecture']) → 'infrastructure'
 * - inferComponent(['testing']) → 'testing'
 * - inferComponent(['ui']) → 'views'
 * - inferComponent(['ux']) → 'views'
 * - inferComponent(['docs']) → 'docs'
 * - inferComponent(['documentation']) → 'docs'
 * - inferComponent(['bug']) → 'services' (default)
 */
export function inferComponent(labels) {
  // Service-related
  if (labels.includes('services') || labels.includes('voice')) return 'services';
  
  // Infrastructure-related
  if (labels.includes('mcp-proxy') || labels.includes('architecture') || 
      labels.includes('infrastructure')) return 'infrastructure';
  
  // Testing
  if (labels.includes('testing')) return 'testing';
  
  // UI-related
  if (labels.includes('ui') || labels.includes('ux')) return 'views';
  
  // Documentation
  if (labels.includes('docs') || labels.includes('documentation')) return 'docs';
  
  // Default to services (most common in WatchTower)
  return 'services';
}

/**
 * Infer Agent Type
 * @param {string[]} labels - Array of label names
 * @param {string} body - Issue body text
 * @param {number} complexity - Inferred complexity score
 * @returns {string} Agent type option key
 * 
 * Test cases:
 * - inferAgentType(['Tech Design Needed'], '', 8) → 'human_required'
 * - inferAgentType(['tech-design-needed'], '', 8) → 'human_required'
 * - inferAgentType(['nnipa-gyinae-hia'], '', 5) → 'human_required'
 * - inferAgentType(['testing'], '', 3) → 'any_agent'
 * - inferAgentType([], 'Copilot should do this', 3) → 'copilot'
 * - inferAgentType([], 'Claude-Opus preferred', 5) → 'claude_opus'
 * - inferAgentType([], 'Task-Maestro will handle', 5) → 'task_maestro'
 * - inferAgentType([], '', 2) → 'copilot' (low complexity)
 * - inferAgentType([], '', 5) → 'claude_opus' (high complexity)
 */
export function inferAgentType(labels, body, complexity) {
  // Tech Design Needed or nnipa-gyinae-hia = Human Required
  if (labels.includes('Tech Design Needed') || 
      labels.includes('tech-design-needed') ||
      labels.includes('nnipa-gyinae-hia')) {
    return 'human_required';
  }
  
  // Testing = any agent
  if (labels.includes('testing')) return 'any_agent';
  
  // Check body for explicit agent mentions
  if (body?.includes('Copilot')) return 'copilot';
  if (body?.includes('Claude-Opus') || body?.includes('Claude')) return 'claude_opus';
  if (body?.includes('Task-Maestro')) return 'task_maestro';
  
  // Low complexity = Copilot eligible
  if (complexity <= 3) return 'copilot';
  
  // Higher complexity = Claude
  return 'claude_opus';
}

/**
 * Parse Dependencies from issue body
 * Looks for "Depends on: #X" or "Blocked by: #X" patterns
 * @param {string} body - Issue body text
 * @returns {string} Comma-separated issue references or "None"
 * 
 * Test cases:
 * - parseDependencies('Depends on: #70') → '#70'
 * - parseDependencies('Blocked by: #70') → '#70'
 * - parseDependencies('Depends on: anokye-labs/watchtower#70') → '#70'
 * - parseDependencies('Depends on:\n- #70\n- #71') → '#70, #71'
 * - parseDependencies('Some text') → 'None'
 * - parseDependencies(null) → 'None'
 */
export function parseDependencies(body) {
  if (!body) return 'None';
  
  const deps = new Set();
  
  // Pattern 1: "Depends on: #70" or "Depends on: repo#70" (captures first occurrence)
  const mainPattern = /(?:depends on|blocked by|blocking)[:\s]+([^\n]+)/gi;
  let mainMatch;
  while ((mainMatch = mainPattern.exec(body)) !== null) {
    const dependencyText = mainMatch[1];
    // Extract all #numbers from this line
    const issuePattern = /(?:[\w-]+\/[\w-]+)?#(\d+)/g;
    let issueMatch;
    while ((issueMatch = issuePattern.exec(dependencyText)) !== null) {
      deps.add(`#${issueMatch[1]}`);
    }
  }
  
  // Pattern 2: List format "- #70" or "- repo#70"
  const listPattern = /^\s*-\s*(?:[\w-]+\/[\w-]+)?#(\d+)/gm;
  let listMatch;
  while ((listMatch = listPattern.exec(body)) !== null) {
    deps.add(`#${listMatch[1]}`);
  }
  
  return deps.size > 0 ? Array.from(deps).join(', ') : 'None';
}

/**
 * Format date for Last Activity field
 * @param {string} dateString - ISO date string
 * @returns {string} YYYY-MM-DD format
 * 
 * Test cases:
 * - formatDate('2025-12-29T03:29:30.159Z') → '2025-12-29'
 * - formatDate('2025-01-15T12:00:00Z') → '2025-01-15'
 * - formatDate(null) → current date in YYYY-MM-DD format
 */
export function formatDate(dateString) {
  return dateString ? dateString.split('T')[0] : new Date().toISOString().split('T')[0];
}

/**
 * Infer all fields for a single issue
 * @param {Object} issue - GitHub issue object
 * @returns {Object} Inferred field values
 * 
 * Test case:
 * - inferAllFields({
 *     number: 89,
 *     title: 'Create inference module',
 *     labels: [{name: 'P1'}, {name: 'services'}],
 *     body: 'Implementation task',
 *     updated_at: '2025-12-29T03:29:30.159Z'
 *   }) → {
 *     number: 89,
 *     title: 'Create inference module',
 *     status: 'backlog',
 *     priority: 'p1_high',
 *     complexity: 1,
 *     component: 'services',
 *     agent_type: 'copilot',
 *     dependencies: 'None',
 *     last_activity: '2025-12-29',
 *     pr_link: ''
 *   }
 */
export function inferAllFields(issue) {
  const labels = issue.labels?.map(l => l.name) || [];
  const body = issue.body || '';
  const complexity = inferComplexity(labels, body);
  
  return {
    number: issue.number,
    title: issue.title,
    
    // Inferred fields
    status: 'backlog',  // All start in Backlog
    priority: inferPriority(labels),
    complexity: complexity,
    component: inferComponent(labels),
    agent_type: inferAgentType(labels, body, complexity),
    dependencies: parseDependencies(body),
    last_activity: formatDate(issue.updated_at),
    pr_link: ''  // Empty initially
  };
}
