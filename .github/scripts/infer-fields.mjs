#!/usr/bin/env node
/**
 * Field Inference Module for Nsumankwahene Migration
 * Infers project field values from existing issue data
 */

/**
 * Infer Priority from existing labels
 * @param {string[]} labels - Array of label names
 * @returns {string} Priority option key (p0_hene, p1_abusuapanyin, p2_obi, p3_akwadaa)
 * 
 * Test cases:
 * - inferPriority(['P0', 'bug']) → 'p0_hene'
 * - inferPriority(['P1', 'enhancement']) → 'p1_abusuapanyin'
 * - inferPriority(['P2']) → 'p2_obi'
 * - inferPriority(['bug']) → 'p3_akwadaa' (default)
 */
export function inferPriority(labels) {
  if (labels.includes('P0')) return 'p0_hene';
  if (labels.includes('P1')) return 'p1_abusuapanyin';
  if (labels.includes('P2')) return 'p2_obi';
  return 'p3_akwadaa';  // Default
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
 * - inferAgentType(['Tech Design Needed'], '', 8) → 'nnipa_hia'
 * - inferAgentType(['tech-design-needed'], '', 8) → 'nnipa_hia'
 * - inferAgentType(['nnipa-gyinae-hia'], '', 5) → 'nnipa_hia'
 * - inferAgentType(['testing'], '', 3) → 'biara'
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
    return 'nnipa_hia';
  }
  
  // Testing = any agent
  if (labels.includes('testing')) return 'biara';
  
  // Normalize body for robust, case-insensitive matching
  const normalizedBody = (body || '').toLowerCase();
  
  // Check body for explicit agent mentions (case-insensitive, tolerant of hyphen/space variations)
  if (normalizedBody.includes('copilot')) return 'copilot';
  if (normalizedBody.includes('claude-opus') ||
      normalizedBody.includes('claude opus') ||
      normalizedBody.includes('claude')) {
    return 'claude_opus';
  }
  if (normalizedBody.includes('task-maestro') ||
      normalizedBody.includes('task maestro')) {
    return 'task_maestro';
  }
  
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
  
  // Work line-by-line so that list items are only associated with
  // a dependency section ("Depends on:", "Blocked by:", "Blocking").
  const lines = body.split('\n');
  
  // Match dependency headers, e.g. "Depends on:", "Blocked by:", "Blocking:"
  // and capture any inline text after the header on the same line.
  const headerRegex = /^\s*(depends on|blocked by|blocking)\s*:?\s*(.*)$/i;
  
  // Match bullet lines following a dependency header, e.g. "- #70" or "- repo#70".
  const bulletRegex = /^\s*-\s+(.*)$/;
  
  // Shared issue reference pattern: "#123" or "owner/repo#123"
  const issuePattern = /(?:[\w-]+\/[\w-]+)?#(\d+)/g;
  
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    const headerMatch = headerRegex.exec(line);
    if (!headerMatch) {
      continue;
    }
    
    // 1) Extract any inline dependencies on the header line itself.
    const inlineText = headerMatch[2];
    if (inlineText) {
      issuePattern.lastIndex = 0;
      let match;
      while ((match = issuePattern.exec(inlineText)) !== null) {
        deps.add(`#${match[1]}`);
      }
    }
    
    // 2) Extract dependencies from immediately following bullet list items.
    let j = i + 1;
    while (j < lines.length && bulletRegex.test(lines[j])) {
      const bulletText = lines[j].replace(/^\s*-\s+/, '');
      issuePattern.lastIndex = 0;
      let bulletMatch;
      while ((bulletMatch = issuePattern.exec(bulletText)) !== null) {
        deps.add(`#${bulletMatch[1]}`);
      }
      j++;
    }
    
    // Skip over the bullet block we just processed.
    i = j - 1;
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
 *     priority: 'p1_abusuapanyin',
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
