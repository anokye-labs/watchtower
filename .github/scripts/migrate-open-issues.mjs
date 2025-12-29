#!/usr/bin/env node
/**
 * Nsumankwahene Migration Script
 * Migrates all open issues to the new field system
 * 
 * Usage: 
 *   node migrate-open-issues.mjs              # Full migration
 *   node migrate-open-issues.mjs --dry-run    # Preview only
 *   node migrate-open-issues.mjs --issue 76   # Single issue test
 * 
 * Requires: GITHUB_TOKEN environment variable
 */

import { Octokit } from '@octokit/rest';
import { readFileSync } from 'fs';
import yaml from 'js-yaml';
import { inferAllFields } from './infer-fields.mjs';
import { ProjectGraphQLClient, delay } from './graphql-client.mjs';

// Parse command line args
const args = process.argv.slice(2);
const isDryRun = args.includes('--dry-run');
const singleIssue = args.includes('--issue') 
  ? parseInt(args[args.indexOf('--issue') + 1]) 
  : null;

// Validate environment
const token = process.env.GITHUB_TOKEN;
if (!token) {
  console.error('❌ Error: GITHUB_TOKEN environment variable required');
  process.exit(1);
}

// Load config
let config;
try {
  const configPath = new URL('./project-config.yml', import.meta.url);
  config = yaml.load(readFileSync(configPath, 'utf8'));
  console.log('✓ Loaded project-config.yml');
} catch (error) {
  console.error('❌ Error loading project-config.yml:', error.message);
  console.error('  Make sure field IDs are populated before running migration.');
  process.exit(1);
}

// Validate config has real IDs (not placeholders)
const hasPlaceholders = JSON.stringify(config).includes('REPLACE_WITH');
if (hasPlaceholders && !isDryRun) {
  console.error('❌ Error: project-config.yml contains placeholder IDs');
  console.error('  Please populate real field IDs from GraphQL before migration.');
  process.exit(1);
}

// Initialize clients
const octokit = new Octokit({ auth: token });
const graphql = new ProjectGraphQLClient(token, config.project?.id);

async function fetchOpenIssues() {
  console.log('\n📋 Fetching open issues...');
  
  const issues = await octokit.paginate(octokit.rest.issues.listForRepo, {
    owner: config.project?.owner || 'anokye-labs',
    repo: config.project?.repo || 'watchtower',
    state: 'open',
    per_page: 100
  });
  
  // Filter out PRs
  const actualIssues = issues.filter(i => !i.pull_request);
  console.log(`  Found ${actualIssues.length} open issues (excluding PRs)`);
  
  return actualIssues;
}

async function migrateIssue(issue) {
  console.log(`\n🔄 Migrating #${issue.number}: ${issue.title}`);
  
  // Infer field values
  const fields = inferAllFields(issue);
  
  console.log(`  Priority: ${fields.priority}`);
  console.log(`  Complexity: ${fields.complexity}`);
  console.log(`  Component: ${fields.component}`);
  console.log(`  Agent Type: ${fields.agent_type}`);
  console.log(`  Dependencies: ${fields.dependencies}`);
  console.log(`  Last Activity: ${fields.last_activity}`);
  
  if (isDryRun) {
    console.log('  [DRY RUN] Would update project fields');
    return { ...fields, migrated: false, dryRun: true };
  }
  
  try {
    // Get or create project item
    let itemId = await graphql.getProjectItemId(issue.node_id);
    if (!itemId) {
      console.log('  Adding issue to project...');
      itemId = await graphql.addIssueToProject(issue.node_id);
    }
    
    // Update all fields
    await graphql.updateAllFields(itemId, fields, config);
    console.log('  ✓ Fields updated successfully');
    
    // Rate limit delay
    await delay(500);
    
    return { ...fields, migrated: true, itemId };
  } catch (error) {
    console.error(`  ❌ Error: ${error.message}`);
    return { ...fields, migrated: false, error: error.message };
  }
}

async function main() {
  console.log('═══════════════════════════════════════════════════════════');
  console.log('  Nsumankwahene Migration Script');
  console.log('  Migrating open issues to new field system');
  console.log('═══════════════════════════════════════════════════════════');
  
  if (isDryRun) {
    console.log('\n⚠️  DRY RUN MODE - No changes will be made');
  }
  
  if (singleIssue) {
    console.log(`\n🎯 Single issue mode: #${singleIssue}`);
  }
  
  // Fetch issues
  let issues = await fetchOpenIssues();
  
  // Filter to single issue if specified
  if (singleIssue) {
    issues = issues.filter(i => i.number === singleIssue);
    if (issues.length === 0) {
      console.error(`❌ Issue #${singleIssue} not found or not open`);
      process.exit(1);
    }
  }
  
  // Migrate each issue
  const results = [];
  for (const issue of issues) {
    const result = await migrateIssue(issue);
    results.push(result);
  }
  
  // Print summary
  console.log('\n═══════════════════════════════════════════════════════════');
  console.log('  MIGRATION SUMMARY');
  console.log('═══════════════════════════════════════════════════════════');
  
  const migrated = results.filter(r => r.migrated);
  const failed = results.filter(r => !r.migrated && !r.dryRun);
  const dryRun = results.filter(r => r.dryRun);
  
  console.log(`\n📊 Results:`);
  console.log(`  Total issues: ${results.length}`);
  console.log(`  Successfully migrated: ${migrated.length}`);
  console.log(`  Failed: ${failed.length}`);
  if (isDryRun) console.log(`  Dry run previews: ${dryRun.length}`);
  
  console.log(`\n📈 By Priority:`);
  console.log(`  P0 (Critical): ${results.filter(r => r.priority === 'p0_hene').length}`);
  console.log(`  P1 (High): ${results.filter(r => r.priority === 'p1_abusuapanyin').length}`);
  console.log(`  P2 (Medium): ${results.filter(r => r.priority === 'p2_obi').length}`);
  console.log(`  P3 (Low): ${results.filter(r => r.priority === 'p3_akwadaa').length}`);
  
  console.log(`\n📂 By Component:`);
  const components = ['services', 'infrastructure', 'testing', 'views', 'docs', 'viewmodels', 'models', 'build'];
  for (const comp of components) {
    const count = results.filter(r => r.component === comp).length;
    if (count > 0) console.log(`  ${comp}: ${count}`);
  }
  
  console.log(`\n🤖 By Agent Type:`);
  const agentTypes = ['copilot', 'claude_opus', 'nnipa_hia', 'biara', 'task_maestro'];
  for (const agent of agentTypes) {
    const count = results.filter(r => r.agent_type === agent).length;
    if (count > 0) console.log(`  ${agent}: ${count}`);
  }
  
  if (failed.length > 0) {
    console.log(`\n❌ Failed issues:`);
    for (const f of failed) {
      console.log(`  #${f.number}: ${f.error}`);
    }
    process.exit(1);
  }
  
  console.log('\n✅ Migration complete!');
}

main().catch(error => {
  console.error('\n❌ Fatal error:', error);
  process.exit(1);
});
