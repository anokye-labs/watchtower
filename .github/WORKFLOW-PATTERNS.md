# GitHub Actions Workflow Patterns

This document describes standard patterns and best practices for GitHub Actions workflows in the WatchTower project.

## Multi-Job Workflows and .NET SDK Setup

### The Pattern

**IMPORTANT**: Each job in a multi-job workflow runs in a completely separate environment (separate runner, separate filesystem, separate installed tools).

When using .NET CLI tools in any job, you **must** explicitly set up the .NET SDK using `actions/setup-dotnet`, even if another job in the same workflow already set it up.

### Why This Matters

GitHub Actions jobs are independent by design:
- Each job runs on a fresh runner instance
- Jobs do not share environment state
- Pre-installed tools on runners may not match project requirements
- SDK versions can vary between runner images

### Standard Setup Block

```yaml
- name: Setup .NET 10 SDK
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '10.0.x'
    dotnet-quality: 'preview'
```

**When to include this:**
- Before running `dotnet` commands (build, test, restore, publish, etc.)
- Before installing .NET global tools (`dotnet tool install -g ...`)
- Before running tools that depend on .NET runtime
- In **every job** that uses .NET, not just the first one

### Performance Considerations

The `setup-dotnet` action is optimized with caching:
- SDK downloads are cached by GitHub Actions
- Subsequent setup steps in different jobs are fast
- The overhead is minimal (typically 5-10 seconds)
- The reliability gain far outweighs the small time cost

### Example: Correct Multi-Job Pattern

```yaml
jobs:
  build-and-test:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      
      # ✅ Setup .NET SDK for this job
      - name: Setup .NET 10 SDK
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
          dotnet-quality: 'preview'
      
      - name: Build
        run: dotnet build
      
      - name: Test
        run: dotnet test

  coverage-report:
    needs: build-and-test
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      
      # ✅ Setup .NET SDK for this job too!
      - name: Setup .NET 10 SDK
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
          dotnet-quality: 'preview'
      
      - name: Install ReportGenerator
        run: dotnet tool install -g dotnet-reportgenerator-globaltool
      
      - name: Generate Report
        run: reportgenerator ...
```

### Example: Incorrect Pattern (Don't Do This)

```yaml
jobs:
  build-and-test:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET 10 SDK
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
          dotnet-quality: 'preview'
      
      - name: Build and Test
        run: dotnet build && dotnet test

  coverage-report:
    needs: build-and-test
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      
      # ❌ Missing setup-dotnet!
      # This will use whatever .NET version is pre-installed on the runner,
      # which may not be .NET 10 and may not be stable across runner updates.
      - name: Install ReportGenerator
        run: dotnet tool install -g dotnet-reportgenerator-globaltool
```

## Job Structure Best Practices

### When to Separate Jobs

Separate jobs when:
- Jobs have logically distinct purposes (build vs. report)
- Jobs can potentially run in parallel
- Failure in one job shouldn't prevent others from running
- You want better visibility in GitHub UI for different stages
- Jobs have significantly different resource requirements

### When to Combine Jobs

Combine jobs when:
- Steps are tightly coupled and must run sequentially
- Sharing state between steps is complex via artifacts
- The combined job is still easy to understand and maintain
- There's significant overhead in job setup

### Current WatchTower Workflows

#### pr-validation.yml
- **Structure**: Two jobs (build-and-test → coverage-report)
- **Rationale**: Separation provides better failure isolation and UI clarity
- **Pattern**: Both jobs include `setup-dotnet` ✅

#### test-automation.yml
- **Structure**: Single job
- **Rationale**: Simpler workflow for continuous testing
- **Pattern**: Includes `setup-dotnet` before installing global tools ✅

## Maintenance Checklist

When creating or modifying workflows:

- [ ] Does each job that uses `dotnet` have a `setup-dotnet` step?
- [ ] Are all SDK version specifications consistent across jobs?
- [ ] Is the dotnet-quality specified if using preview/rc versions?
- [ ] Are global tool installations placed after SDK setup?
- [ ] Is the job structure (combined vs. separated) appropriate?
- [ ] Are timeout values reasonable for the job's complexity?
- [ ] Are artifacts properly uploaded/downloaded between dependent jobs?

## References

- [GitHub Actions: Using jobs](https://docs.github.com/en/actions/using-jobs)
- [actions/setup-dotnet documentation](https://github.com/actions/setup-dotnet)
- [WatchTower CI Integration Documentation](./CI-INTEGRATION-IMPLEMENTATION.md)

---

**Last Updated**: 2026-01-08  
**Applies To**: All GitHub Actions workflows in `.github/workflows/`
