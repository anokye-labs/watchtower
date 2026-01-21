#!/usr/bin/env node
/**
 * Field Inference Module for Watchtower Project Migration
 * Infers project field values from existing issue data
 * 
 * Field values match the "Watchtower Iterative Development" GitHub Project
 * Project ID: PVT_kwDODbLOnM4BLchY
 */

/**
 * Infer Priority from existing labels
 * @param {string[]} labels - Array of label names
 * @returns {string} Priority option name (P0, P1, P2, P3)
 * 
 * Test cases:
 * - inferPriority(['P0', 'bug']) → 'P0'
 * - inferPriority(['P1', 'enhancement']) → 'P1'
 * - inferPriority(['P2']) → 'P2'
 * - inferPriority(['bug']) → 'P3' (default)
 */
export function inferPriority(labels) {
  if (labels.includes('P0')) return 'P0';
  if (labels.includes('P1')) return 'P1';
  if (labels.includes('P2')) return 'P2';
  return 'P3';  // Default
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
 * @returns {string} Component option name (Services, View Model, Views, Models, Testing, Infrastructure, Docs, Build)
 * 
 * Test cases:
 * - inferComponent(['services']) → 'Services'
 * - inferComponent(['voice']) → 'Services'
 * - inferComponent(['viewmodels']) → 'View Model'
 * - inferComponent(['mcp-proxy']) → 'Infrastructure'
 * - inferComponent(['architecture']) → 'Infrastructure'
 * - inferComponent(['testing']) → 'Testing'
 * - inferComponent(['ui']) → 'Views'
 * - inferComponent(['ux']) → 'Views'
 * - inferComponent(['docs']) → 'Docs'
 * - inferComponent(['documentation']) → 'Docs'
 * - inferComponent(['build']) → 'Build'
 * - inferComponent(['bug']) → 'Services' (default)
 */
export function inferComponent(labels) {
  // Service-related
  if (labels.includes('services') || labels.includes('voice')) return 'Services';
  
  // ViewModel-related
  if (labels.includes('viewmodels') || labels.includes('viewmodel')) return 'View Model';
  
  // Infrastructure-related
  if (labels.includes('mcp-proxy') || labels.includes('architecture') || 
      labels.includes('infrastructure')) return 'Infrastructure';
  
  // Testing
  if (labels.includes('testing')) return 'Testing';
  
  // UI-related
  if (labels.includes('ui') || labels.includes('ux')) return 'Views';
  
  // Models
  if (labels.includes('models') || labels.includes('model')) return 'Models';
  
  // Documentation
  if (labels.includes('docs') || labels.includes('documentation')) return 'Docs';
  
  // Build
  if (labels.includes('build') || labels.includes('ci') || labels.includes('cd')) return 'Build';
  
  // Default to Services (most common in WatchTower)
  return 'Services';
}

/**
 * Infer Agent Type
 * @param {string[]} labels - Array of label names
 * @param {string} body - Issue body text
 * @param {number} complexity - Inferred complexity score
 * @returns {string} Agent type option name (Copilot, Copilot + Thinking, Task-Maestro, Human Required, Any Agent)
 * 
 * Test cases:
 * - inferAgentType(['Tech Design Needed'], '', 8) → 'Human Required'
 * - inferAgentType(['tech-design-needed'], '', 8) → 'Human Required'
 * - inferAgentType(['requires:human-decision'], '', 5) → 'Human Required'
 * - inferAgentType(['testing'], '', 3) → 'Any Agent'
 * - inferAgentType([], 'Copilot should do this', 3) → 'Copilot'
 * - inferAgentType([], 'Claude-Opus preferred', 5) → 'Copilot + Thinking'
 * - inferAgentType([], 'Task-Maestro will handle', 5) → 'Task-Maestro'
 * - inferAgentType([], '', 2) → 'Copilot' (low complexity)
 * - inferAgentType([], '', 5) → 'Copilot + Thinking' (high complexity)
 */
export function inferAgentType(labels, body, complexity) {
  // Tech Design Needed or requires:human-decision = Human Required
  if (labels.includes('Tech Design Needed') || 
      labels.includes('tech-design-needed') ||
      labels.includes('requires:human-decision')) {
    return 'Human Required';
  }
  
  // Testing = any agent
  if (labels.includes('testing')) return 'Any Agent';
  
  // Normalize body for robust, case-insensitive matching
  const normalizedBody = (body || '').toLowerCase();
  
  // Check body for explicit agent mentions (case-insensitive, tolerant of hyphen/space variations)
  if (normalizedBody.includes('copilot') && !normalizedBody.includes('thinking')) return 'Copilot';
  if (normalizedBody.includes('claude-opus') ||
      normalizedBody.includes('claude opus') ||
      normalizedBody.includes('claude') ||
      normalizedBody.includes('thinking')) {
    return 'Copilot + Thinking';
  }
  if (normalizedBody.includes('task-maestro') ||
      normalizedBody.includes('task maestro')) {
    return 'Task-Maestro';
  }
  
  // Low complexity = Copilot eligible
  if (complexity <= 3) return 'Copilot';
  
  // Higher complexity = Copilot + Thinking (replaces Claude Opus)
  return 'Copilot + Thinking';
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
 * @returns {Object} Inferred field values matching GitHub Project field names
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
 *     status: 'Backlog',
 *     priority: 'P1',
 *     complexity: 1,
 *     component: 'Services',
 *     agent_type: 'Copilot',
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
    
    // Inferred fields (values match GitHub Project option names)
    status: 'Backlog',  // All start in Backlog
    priority: inferPriority(labels),
    complexity: complexity,
    component: inferComponent(labels),
    agent_type: inferAgentType(labels, body, complexity),
    dependencies: parseDependencies(body),
    last_activity: formatDate(issue.updated_at),
    pr_link: ''  // Empty initially
  };
}
