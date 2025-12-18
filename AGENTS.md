# WatchTower - AI Agent Instructions

## FAL.AI
NOTE: the FAL.AI Key is in the local .env file when creating scripts, create them to load it from there.


## Project Overview
- Avalonia UI-based application with strict MVVM architecture and DI on .NET 10.
- Cross-platform first (win-x64, osx-x64, linux-x64) with self-contained single-file publish.
- Open source only (MIT/Apache 2.0).

## Architecture & Structure
- MVVM enforcement: ViewModels contain logic; Views only bindings/presentation; code-behind limited to initialization.
- Service layer: business logic in Services/, injected into ViewModels via DI; ViewModels orchestrate services.
- Directory highlights: WatchTower/ (Models, ViewModels, Views, Services, App.axaml, Program.cs, appsettings.json); .specify/ (constitution, templates, scripts); .github/agents/ (Spec-Kit agents); specs/NNN-feature-name/ (spec, plan, tasks, checklists).

## Core Principles
1. MVVM Architecture First.
2. Open Source Only.
3. Cross-Platform Native.
4. Testability Required (Avalonia.Headless, no browser storage APIs).
5. Design System Compliance (Avalonia CSS vars; dark primary, light supported).

## Critical Workflows
- General: Prefer built-in tasks/tools over CLI; justify any direct tool invocation; use runSubagent for code changes.
- Spec-Kit flow: create-new-feature → speckit.clarify → setup-plan → speckit.tasks → speckit.analyze → speckit.implement. Always run check-prerequisites.ps1 -Json for paths/validation.

## Build & Run
- Development build: dotnet build.
- Run: dotnet run --project WatchTower/WatchTower.csproj.
- Publish (per RID): dotnet publish -c Release -r win-x64|osx-x64|linux-x64 --self-contained true.

## Debugging
- F5 attaches debugger; breakpoints in all C# files; XAML hot reload in Debug.

## dotnet watch Workflow (preferred)
- Start via VS Code watch task or dotnet watch run --project WatchTower/WatchTower.csproj --non-interactive.
- Never kill WatchTower/dotnet processes; watch handles restarts. If file locks block builds, ask user to close app.
- Hot reload applies most C# edits; rude edits auto-restart (DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true). Manual restart: Ctrl+R in watch terminal.
- Environment: DOTNET_ENVIRONMENT=Development; DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true.
- Supported hot reload edits: modify method bodies; add methods/fields/ctors/properties/events/indexers; add nested/top-level types; async/iterator changes; lambdas/LINQ; generics; dynamic; attributes; using directives; parameter renames/type tweaks; return type changes; delete/rename members (except fields); namespace edits; partials; source-generated edits.
- Unsupported (triggers restart): interface changes; abstract/virtual/override additions; destructor additions; type parameter/base/delegate changes; type deletions; catch/try-finally active changes; making abstract method concrete; embedded interop edits; lambda signature or captured-variable changes.
- Ask for manual shutdown only when modifying csproj structure (not packages), persistent build errors, crashed watch, or missing DOTNET_WATCH_RESTART_ON_RUDE_EDIT.

## Project-Specific Conventions
- MVVM example (logic in ViewModel) and anti-pattern (no logic in code-behind); services registered in App.axaml.cs via DI.
- Configuration: appsettings.json Logging:LogLevel minimal|normal|verbose → Warning|Information|Debug; LoggingService loads config.
- Red flags: code-behind logic; static service locators; platform-specific hacks without guards; hardcoded config; ViewModels requiring UI types; manually created tasks/specs.

## Testing
- Minimum 80% coverage for ViewModels and Services; write tests before implementation; ViewModels testable without UI dependencies; xUnit/NUnit recommended.

## Tool Usage (MCP first)
- Prefer .NET MCP tools: list_errors, find_all_references, find_symbols, get_symbol_definition, add_member/update_member, search_package_context, gcdump_* , code_refactoring, rename_symbol, get_solution_context, source generator readers, symbol_semantic_search.
- Strategy: favor semantic tools over text search; reach for code_refactoring for multi-file transforms; verify solution path exists before passing to tools.
- Confidence: tool outputs are authoritative; avoid redundant validation. Fallback to built-ins (semantic_search, list_code_usages, file_search, replace_string_in_file, multi_replace_string_in_file, get_search_view_results, list_dir) only when MCP tools unavailable.

## C# Code Style & Quality
- Follow .NET Runtime guidelines; Allman braces; 4-space indent; `_camelCase` private/internal fields, `s_` static, `t_` thread static; avoid this.; explicit visibility; sorted usings (System first); avoid extra blank lines/whitespace; var only with explicit RHS type; language keywords over BCL types; PascalCase constants/methods; use nameof; non-ASCII via \uXXXX; single-line if only when all branches single-line.
- Tooling: enforced by .editorconfig; dotnet format (or --verify-no-changes) to align style.
- Package management: use dotnet add/remove/list; never hand-edit csproj for packages.
- Solution management: dotnet sln list/add/remove.

## When Working on Features
- Check constitution (.specify/memory/constitution.md), spec, plan, tasks in order; respect task dependencies and [P] markers; keep MVVM separation; ensure cross-platform parity; stay on latest stable Avalonia (currently 11.3.9, suggest upgrade when newer stable ships).

**IMPORTANT:** The message "Terminal will be reused by tasks, press any key to close it." is informational, not a prompt.

---
Last Updated: 2025-11-24 | Constitution Version: 1.1.0