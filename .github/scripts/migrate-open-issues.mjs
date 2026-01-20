#!/usr/bin/env node
/**
 * Agent Flow Migration Script
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

// Validate --issue argument
const issueIndex = args.indexOf('--issue');
let singleIssue = null;
if (issueIndex !== -1) {
  const issueArg = args[issueIndex + 1];
  
  if (!issueArg || issueArg.startsWith('--')) {
    console.error('❌ Error: --issue option requires a positive integer argument');
    console.error('   Usage: node migrate-open-issues.mjs --issue 76');
    process.exit(1);
  }
  
  const parsedIssue = Number(issueArg);
  if (!Number.isInteger(parsedIssue) || parsedIssue <= 0) {
    console.error(`❌ Error: Invalid issue number "${issueArg}". Expected a positive integer.`);
    console.error('   Usage: node migrate-open-issues.mjs --issue 76');
    process.exit(1);
  }
  
  singleIssue = parsedIssue;
}

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

// Validate project configuration fields
if (!config.project?.owner || !config.project?.repo || !config.project?.id) {
  console.error('❌ Error: Missing project configuration in project-config.yml');
  console.error('  Please set "project.owner", "project.repo", and "project.id" in the config file.');
  process.exit(1);
}

// Initialize clients
const octokit = new Octokit({ auth: token });
const graphql = new ProjectGraphQLClient(token, config.project.id);

async function fetchOpenIssues() {
  console.log('\n📋 Fetching open issues...');
  
  const issues = await octokit.paginate(octokit.rest.issues.listForRepo, {
    owner: config.project.owner,
    repo: config.project.repo,
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
    
    return { ...fields, migrated: true, itemId };
  } catch (error) {
    console.error(`  ❌ Error: ${error.message}`);
    // Apply backoff delay after failed update to reduce API pressure
    await delay(500);
    return { ...fields, migrated: false, error: error.message };
  }
}

async function main() {
  console.log('═══════════════════════════════════════════════════════════');
  console.log('  Agent Flow Migration Script');
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
  
  // Migrate each issue with small delay for rate limiting
  const results = [];
  for (const issue of issues) {
    const result = await migrateIssue(issue);
    results.push(result);
    // Small delay between all migrations to prevent rate limiting
    if (results.length < issues.length) {
      await delay(200);
    }
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
  console.log(`  P0 (Critical): ${results.filter(r => r.priority === 'P0').length}`);
  console.log(`  P1 (High): ${results.filter(r => r.priority === 'P1').length}`);
  console.log(`  P2 (Medium): ${results.filter(r => r.priority === 'P2').length}`);
  console.log(`  P3 (Low): ${results.filter(r => r.priority === 'P3').length}`);
  
  console.log(`\n📂 By Component:`);
  const components = ['Services', 'Infrastructure', 'Testing', 'Views', 'Docs', 'View Model', 'Models', 'Build', 'unclassified'];
  for (const comp of components) {
    const count = results.filter(r => r.component === comp).length;
    if (count > 0) console.log(`  ${comp}: ${count}`);
  }
  
  console.log(`\n🤖 By Agent Type:`);
  const agentTypes = ['Copilot', 'Copilot + Thinking', 'Human Required', 'Any Agent', 'Task-Maestro'];
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
