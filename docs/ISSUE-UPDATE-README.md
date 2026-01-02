# GitHub Issue Updates - Windows-Only Migration

## Overview

This directory contains comprehensive documentation for updating all open GitHub issues to remove cross-platform requirements and update them to reflect WatchTower's Windows-only target (per issue #172).

## Status

**Due to environment limitations, GitHub issue updates cannot be performed automatically.** The GitHub MCP tools available do not provide write access to GitHub issues, and GitHub CLI (`gh`) is not authenticated in this environment.

## What Has Been Provided

Two comprehensive documentation files have been created to facilitate manual or authenticated updates:

### 1. ISSUE-UPDATES-WINDOWS-ONLY.md
**Purpose**: Initial tracking document with overview and partial update instructions

**Contains**:
- Update patterns and search/replace templates
- Standard comment template
- Detailed instructions for high-priority CI/CD issues (#162, #135, #128, #131)
- Detailed instructions for window/UI issues (#45, #119, #120)
- Progress tracking checklist
- Verification checklist
- Optional automation script template

### 2. COMPLETE-ISSUE-UPDATE-INSTRUCTIONS.md
**Purpose**: Complete, issue-by-issue update instructions for ALL 22 open issues

**Contains**:
- Detailed, copy-paste ready instructions for each issue
- Exact text to find and exact replacement text
- Instructions to delete entire sections where needed
- Standard update comment for all issues
- Verification script commands
- Final completion checklist

## How to Use These Documents

### Option 1: Manual Updates (Recommended)
1. Open each issue in GitHub web interface
2. Follow the instructions in `COMPLETE-ISSUE-UPDATE-INSTRUCTIONS.md`
3. For each issue:
   - Click "Edit" on the issue description
   - Follow the find/replace instructions
   - Save the updated description
   - Add the standard comment
4. Check off items in the completion checklist

### Option 2: Automated Updates (Requires Authentication)
1. Authenticate with GitHub CLI: `gh auth login`
2. For each issue, prepare an updated markdown file with the changes
3. Use `gh issue edit <number> --body "$(cat updated-<number>.md)"`
4. Add comments: `gh issue comment <number> --body "Updated to reflect Windows-only target per #172..."`

### Option 3: Use GitHub API
Write a script using GitHub REST API or GraphQL API with proper authentication to perform bulk updates.

## Issue Summary

### Total Issues to Update: 22
(Issue #44 is closed and can be skipped)

**High Priority CI/CD**: #162, #135, #128, #131
**Window/UI Issues**: #45, #55, #119, #120, #122, #136, #139, #141, #54, #113
**Documentation**: #130, #146
**Voice**: #38, #46, #47, #48, #49
**MCP/Architecture**: #50

## Verification

After all updates are complete, run these searches to verify no cross-platform references remain:

```bash
gh search issues --repo anokye-labs/watchtower "is:open cross-platform"
gh search issues --repo anokye-labs/watchtower "is:open macOS"
gh search issues --repo anokye-labs/watchtower "is:open osx-x64"
gh search issues --repo anokye-labs/watchtower "is:open linux-x64"
gh search issues --repo anokye-labs/watchtower "is:open \"all platforms\""
```

All searches should return 0 results.

## Next Steps

1. Review `COMPLETE-ISSUE-UPDATE-INSTRUCTIONS.md` for detailed instructions
2. Choose update method (manual, CLI, or API)
3. Perform updates systematically, checking off items in the completion checklist
4. Run verification searches
5. Close this PR once all updates are confirmed

## Questions?

Refer to the detailed instructions in the companion documents. Each issue has specific find/replace instructions that account for the exact text in each issue.
