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
 */
export function parseDependencies(body) {
  if (!body) return 'None';
  
  // Match various dependency patterns
  const patterns = [
    /(?:depends on|blocked by|blocking)[:\s]+#(\d+)/gi,
    /depends on:\s*\n\s*-\s*#(\d+)/gi,
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
