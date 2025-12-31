# Nsumankwahene Migration Execution Checklist

## 🎯 Goal
Complete #69 - Nsumankwahene Flow System by executing migration and validation.

## ✅ Already Complete (75%)
- [x] Custom fields created
- [x] Field IDs documented
- [x] Labels created
- [x] Automation workflows deployed
- [x] Views created
- [x] Issue templates created
- [x] Agent configuration created
- [x] Migration tooling built
- [x] Documentation written (nsumankwahene-workflow.md)

## 🚀 Remaining Tasks (25%)

### Step 1: Execute Migration (#76)
**Estimated Time: 15-30 minutes**

1. **Set GitHub Token** (if not already set):
   ```powershell
   $env:GITHUB_TOKEN = "your_token_here"
   ```

2. **Run Dry-Run** (preview changes):
   ```bash
   cd .github/scripts
   npm run dry-run
   ```
   Review output to ensure inference looks correct.

3. **Execute Migration**:
   ```bash
   npm run migrate
   ```
   This will:
   - Add all 31 open issues to project
   - Populate Status, Priority, Complexity, Agent Type, Component, Dependencies, Last Activity
   - Generate summary report

4. **Verify in UI**:
   - Open [Project Board](https://github.com/orgs/anokye-labs/projects/2)
   - Check "Adwuma Nhyehyɛe" (Work Queue) view
   - Verify all issues have fields populated
   - Spot-check a few issues for accuracy

### Step 2: Validate End-to-End Cycle (#81)
**Estimated Time: 1-2 hours**

Follow the validation checklist in issue #81:

1. **Create Test Issue**:
   ```bash
   gh issue create --title "Test: Nsumankwahene Validation" \
     --body "Testing automation cycle" \
     --label "P3,docs" \
     --assignee @me
   ```

2. **Set to Ready**:
   - In project UI, set Status = "Ready"
   - Verify appears in Work Queue view

3. **Create PR**:
   ```bash
   git checkout -b test/nsumankwahene-validation
   echo "# Test" >> test-file.md
   git add test-file.md
   git commit -m "test: nsumankwahene validation"
   git push origin test/nsumankwahene-validation
   gh pr create --title "Test: Validate Nsumankwahene" \
     --body "Fixes #<test-issue-number>" \
     --draft
   ```

4. **Verify Automation**:
   - Check Actions tab for workflow run
   - Verify Status changed to "In progress"
   - Verify PR Link populated
   - Verify `ɔkyeame:dwuma` label added

5. **Mark Ready for Review**:
   ```bash
   gh pr ready
   ```
   - Verify Status changed to "In review"

6. **Merge PR**:
   ```bash
   gh pr merge --squash
   ```
   - Verify Status changed to "Done"
   - Verify issue closed
   - Verify `ɔkyeame:dwuma` label removed

7. **Test Stale Detection** (optional - can wait for weekly cron):
   - Manually trigger workflow or wait for weekly run
   - Verify issues >5 days in "In progress" get set to "Blocked"

8. **Cleanup**:
   ```bash
   git checkout main
   git pull
   git branch -D test/nsumankwahene-validation
   ```

### Step 3: Final Verification
**Estimated Time: 15 minutes**

1. **Views Check**:
   - Adwuma Nhyehyɛe (Work Queue) - Has Ready items
   - Asiw Amanneɛ (Blocked Items) - Empty or shows known blockers
   - Hwehwɛ Ahyɛnsoɔ (Review Tracking) - Shows PRs in review
   - Nkɔsoɔ Akontaabu (Throughput) - Shows completed items
   - Nkabom Mepɔ (Dependency Map) - Shows dependency chains

2. **Documentation Check**:
   - [x] docs/nsumankwahene-workflow.md exists
   - [x] README.md links to project board
   - [x] AGENTS.md references Nsumankwahene

3. **Update Issue #69**:
   - Mark all checkboxes complete
   - Add comment summarizing migration results
   - Close issue

## 📊 Success Criteria

All items from #69 must be true:
- [x] All 8 custom fields created with English names + descriptions
- [x] Field IDs documented in `.github/project-config.yml`
- [x] 5 views created and rendering correctly
- [x] GitHub Actions workflow deployed and triggering on PR events
- [ ] All open issues migrated with Status, Priority, Component, Agent Type populated
- [ ] At least 1 full cycle (Backlog → Done) completed through automation
- [x] Documentation complete and linked

## 🔧 Troubleshooting

### Migration Fails
- Check `GITHUB_TOKEN` has correct permissions
- Verify field IDs in `project-config.yml` match project
- Check API rate limits: `gh api rate_limit`

### Automation Not Triggering
- Check workflow file syntax: `.github/workflows/nsumankwahene-automation.yml`
- Verify PR body contains `Closes #X` or `Fixes #X`
- Check Actions tab for errors

### Fields Not Updating
- Verify field IDs in workflow match `project-config.yml`
- Check workflow logs for GraphQL errors
- Ensure field types match (SingleSelect, Number, Text, Date)

## 📚 Reference

- Issue #69: https://github.com/anokye-labs/watchtower/issues/69
- Issue #76: https://github.com/anokye-labs/watchtower/issues/76
- Issue #81: https://github.com/anokye-labs/watchtower/issues/81
- Project Board: https://github.com/orgs/anokye-labs/projects/2
- Workflow Docs: docs/nsumankwahene-workflow.md

---

**Current Status**: ⏳ Ready for migration execution  
**Next Action**: Set GITHUB_TOKEN and run `npm run dry-run`  
**Estimated Completion**: ~2 hours total
