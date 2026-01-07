# CI Integration Testing Plan

This document outlines the test plan for validating the CI integration with Nsumankwahene project automation.

## Prerequisites

- [ ] PR validation workflow (`pr-validation.yml`) must exist
- [ ] Test repository with GitHub Projects V2 configured
- [ ] Labels created: `needs-fix`, `ci-fail:1`, `ci-fail:2`, `ci-fail:3+`

## Test Scenarios

### Scenario 1: First CI Failure

**Setup:**
1. Create an issue (e.g., #TEST-001)
2. Create a PR that closes the issue
3. Introduce a failing test or formatting error

**Expected Behavior:**
- [ ] `needs-fix` label added to PR
- [ ] `needs-fix` label added to linked issue #TEST-001
- [ ] `ci-fail:1` label added to issue #TEST-001
- [ ] Comment posted on PR with failure details
- [ ] Issue status remains "In progress" (not blocked yet)

**Verification Commands:**
```bash
gh pr view <PR-NUMBER> --json labels
gh issue view TEST-001 --json labels
gh issue view TEST-001 --json projectItems
```

### Scenario 2: Second Consecutive CI Failure

**Setup:**
1. From Scenario 1, push another commit that still fails CI
2. Wait for check_suite to complete

**Expected Behavior:**
- [ ] `ci-fail:1` label removed from issue
- [ ] `ci-fail:2` label added to issue
- [ ] Issue status transitions to "Blocked"
- [ ] Comment posted on issue explaining status change
- [ ] `needs-fix` label remains on PR and issue

**Verification Commands:**
```bash
gh issue view TEST-001 --json labels
gh api graphql -f query='
  query($owner: String!, $repo: String!, $issueNumber: Int!) {
    repository(owner: $owner, name: $repo) {
      issue(number: $issueNumber) {
        projectItems(first: 1) {
          nodes {
            fieldValues(first: 10) {
              nodes {
                ... on ProjectV2ItemFieldSingleSelectValue {
                  field { name }
                  name
                }
              }
            }
          }
        }
      }
    }
  }
' -f owner='anokye-labs' -f repo='watchtower' -F issueNumber=<ISSUE_NUMBER>
```

**Note:** Replace `<ISSUE_NUMBER>` with the actual numeric issue number (e.g., `123`).

### Scenario 3: Third Consecutive CI Failure

**Setup:**
1. From Scenario 2, push another commit that still fails CI
2. Wait for check_suite to complete

**Expected Behavior:**
- [ ] `ci-fail:2` label removed from issue
- [ ] `ci-fail:3+` label added to issue
- [ ] Issue status remains "Blocked"
- [ ] `needs-fix` label remains on PR and issue

### Scenario 4: CI Success After Failures

**Setup:**
1. From Scenario 3 (or any failure state), push a commit that fixes the issues
2. Wait for check_suite to complete successfully

**Expected Behavior:**
- [ ] `needs-fix` label removed from PR
- [ ] `needs-fix` label removed from issue
- [ ] All `ci-fail:*` labels removed from issue
- [ ] Issue status transitions to "In review"
- [ ] `ɔkyeame:dwuma` label removed from issue

**Verification Commands:**
```bash
gh issue view TEST-001 --json labels
gh pr view <PR-NUMBER> --json labels
```

### Scenario 5: Draft PR Handling

**Setup:**
1. Create a draft PR with linked issue
2. Introduce failing CI

**Expected Behavior:**
- [ ] `needs-fix` label added to PR and issue
- [ ] `ci-fail:1` label added to issue
- [ ] Issue status does NOT transition to "In review" when CI passes (stays "In progress")

### Scenario 6: PR Without Linked Issues

**Setup:**
1. Create a PR without `Closes #X` in description
2. Introduce failing CI

**Expected Behavior:**
- [ ] `needs-fix` label added to PR only
- [ ] No project status changes
- [ ] Comment posted on PR with failure details

### Scenario 7: Workflow Filtering (When Enabled)

**Setup:**
1. Uncomment workflow filter in nsumankwahene-automation.yml
2. Trigger check_suite from non-pr-validation workflow

**Expected Behavior:**
- [ ] Workflow logs show "Check suite not from GitHub Actions, skipping" or similar
- [ ] No labels added
- [ ] No status changes

## Manual Testing Checklist

When dependencies #164, #165, #166 are complete:

1. [ ] Create test issue
2. [ ] Run Scenario 1 - First failure
3. [ ] Verify labels and status
4. [ ] Run Scenario 2 - Second failure (should block)
5. [ ] Verify "Blocked" status in GitHub Project
6. [ ] Run Scenario 4 - Success after failures
7. [ ] Verify all ci-fail labels cleared
8. [ ] Verify status returns to "In review"

## Rollback Plan

If issues are discovered:

1. Revert workflow changes:
   ```bash
   git revert <commit-sha>
   ```

2. Remove tracking labels from issues:
   ```bash
   gh label delete ci-fail:1
   gh label delete ci-fail:2
   gh label delete ci-fail:3+
   ```

3. Manually update any issues stuck in "Blocked" status

## Notes

- The `ci-fail:*` labels are purely for tracking and do not affect the issue workflow directly
- The "Blocked" status transition is the key behavior that helps prevent merge of failing PRs
- All automations are idempotent - running multiple times won't cause issues
- The workflow gracefully handles missing project data and labels

## Related Issues

- #162 - Parent issue for CI integration
- #164 - PR validation workflow (dependency)
- #165 - Coverage reporting (dependency)
- #166 - Formatting check (dependency)
