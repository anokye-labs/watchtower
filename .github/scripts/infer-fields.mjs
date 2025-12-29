#!/usr/bin/env node
/**
 * Field Inference Module for Nsumankwahene Migration
 * Infers project field values from existing issue data
 */

/**
 * Infer Priority from existing labels
 * @param {string[]} labels - Array of label names
 * @returns {string} Priority option key (p0_hene, p1_abusuapanyin, p2_obi, p3_akwadaa)
 */
export function inferPriority(labels) {
  const safeLabels = Array.isArray(labels) ? labels : [];
  if (safeLabels.includes('P0')) return 'p0_hene';
  if (safeLabels.includes('P1')) return 'p1_abusuapanyin';
  if (safeLabels.includes('P2')) return 'p2_obi';
  return 'p3_akwadaa';  // Default
}

/**
 * Infer Complexity (1-13 Fibonacci scale)
 * @param {string[]} labels - Array of label names  
 * @param {string} body - Issue body text
 * @returns {number} Complexity score
 */
export function inferComplexity(labels, body) {
  const safeLabels = Array.isArray(labels) ? labels : [];
  
  // Tech Design Needed = high complexity
  if (safeLabels.includes('Tech Design Needed') || safeLabels.includes('tech-design-needed')) {
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
 */
export function inferComponent(labels) {
  const safeLabels = Array.isArray(labels) ? labels : [];
  
  // Service-related
  if (safeLabels.includes('services') || safeLabels.includes('voice')) return 'services';
  
  // Infrastructure-related
  if (safeLabels.includes('mcp-proxy') || safeLabels.includes('architecture') || 
      safeLabels.includes('infrastructure')) return 'infrastructure';
  
  // Testing
  if (safeLabels.includes('testing')) return 'testing';
  
  // UI-related
  if (safeLabels.includes('ui') || safeLabels.includes('ux')) return 'views';
  
  // Documentation
  if (safeLabels.includes('docs') || safeLabels.includes('documentation')) return 'docs';
  
  // If there are no labels at all, cannot confidently infer a component
  if (safeLabels.length === 0) return 'unclassified';
  
  // Default to services when labels exist but do not match known components
  // (most common category in WatchTower)
  return 'services';
}

/**
 * Infer Agent Type
 * @param {string[]} labels - Array of label names
 * @param {string} body - Issue body text
 * @param {number} complexity - Inferred complexity score
 * @returns {string} Agent type option key
 */
export function inferAgentType(labels, body, complexity) {
  const safeLabels = Array.isArray(labels) ? labels : [];
  
  // Tech Design Needed or nnipa-gyinae-hia = Human Required
  if (safeLabels.includes('Tech Design Needed') || 
      safeLabels.includes('tech-design-needed') ||
      safeLabels.includes('nnipa-gyinae-hia')) {
    return 'nnipa_hia';
  }
  
  // Testing = any agent
  if (safeLabels.includes('testing')) return 'biara';
  
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
 * Looks for "Depends on: #X", "Blocked by: #X", or "Blocking #X" patterns
 * @param {string} body - Issue body text
 * @returns {string} Comma-separated issue references or "None"
 */
export function parseDependencies(body) {
  if (!body) return 'None';
  
  // Match various dependency patterns (inline and list formats)
  // Handles: "Depends on: #123", "Blocking #456", "Depends on:\n- #789"
  const patterns = [
    /(?:depends on|blocked by|blocking)[:\s]*(?:\n\s*-\s*)?#(\d+)/gi,
  ];
  
  const deps = new Set();
  for (const pattern of patterns) {
    let match;
    while ((match = pattern.exec(body)) !== null) {
      deps.add(`#${match[1]}`);
    }
  }
  
  return deps.size > 0 ? Array.from(deps).join(', ') : 'None';
}

/**
 * Format date for Last Activity field
 * Falls back to current date if dateString is missing, which preserves existing
 * behavior but may make issues appear more recently active than they actually were.
 * In practice, GitHub API always provides updated_at, so this is a defensive fallback.
 * @param {string} dateString - ISO date string
 * @returns {string} YYYY-MM-DD format
 */
export function formatDate(dateString) {
  return dateString ? dateString.split('T')[0] : new Date().toISOString().split('T')[0];
}

/**
 * Infer all fields for a single issue
 * @param {Object} issue - GitHub issue object
 * @returns {Object} Inferred field values
 */
export function inferAllFields(issue) {
  const labels = issue.labels?.map(l => l.name) || [];
  const body = issue.body || '';
  const complexity = inferComplexity(labels, body);
  
  return {
    number: issue.number,
    title: issue.title,
    
    // Inferred fields
    status: 'afiase',  // All start in Backlog (Afiase)
    priority: inferPriority(labels),
    complexity: complexity,
    component: inferComponent(labels),
    agent_type: inferAgentType(labels, body, complexity),
    dependencies: parseDependencies(body),
    last_activity: formatDate(issue.updated_at),
    pr_link: ''  // Empty initially
  };
}
