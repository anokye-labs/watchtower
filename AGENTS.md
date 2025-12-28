# WatchTower - AI Agent Instructions

This document provides guidelines for AI agents and developers working on the WatchTower codebase. For detailed architecture information, see [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md). For codebase-specific terminology, see [docs/GLOSSARY.md](docs/GLOSSARY.md).

## FAL.AI

NOTE: the FAL.AI Key is in the local .env file when creating scripts, create them to load it from there.

## Project Overview

WatchTower is an Avalonia UI-based application with strict MVVM architecture and dependency injection on .NET 10. It is cross-platform first, targeting win-x64, osx-x64, and linux-x64 with self-contained single-file publish. The project uses only open source dependencies (MIT/Apache 2.0).

### Key Capabilities

The application provides Adaptive Card rendering with a custom "Ancestral Futurism" theme, gamepad-first navigation through SDL2 integration, full-duplex voice capabilities with offline and online modes, and an animated shell window with 5x5 grid frame slicing for resolution-independent scaling.

## Architecture & Structure

### MVVM Enforcement

ViewModels contain all presentation logic. Views contain only bindings and presentation with code-behind limited to initialization. Services encapsulate business logic and are injected into ViewModels via DI.

### Directory Structure

The main application code is in `WatchTower/` containing Models, ViewModels, Views, Services, App.axaml, Program.cs, and appsettings.json. Technical documentation is in `docs/` including ARCHITECTURE.md, GLOSSARY.md, and feature-specific guides. Design inspiration and cultural references are in `concept-art/`.

### Layer Responsibilities

```
View Layer (UI)              ViewModel Layer (Logic)       Service Layer (Business Logic)
--------------------         ----------------------        -----------------------------
MainWindow.axaml         ->  MainWindowViewModel       ->  IGameControllerService
ShellWindow.axaml        ->  ShellWindowViewModel      ->  IAdaptiveCardService
SplashWindow.axaml       ->  SplashWindowViewModel     ->  IVoiceOrchestrationService
                                                          StartupOrchestrator
                                                          FrameSliceService
```

## Core Principles

1. **MVVM Architecture First** - All logic in ViewModels and Services, Views are presentation-only.
2. **Open Source Only** - Use only MIT or Apache 2.0 licensed dependencies.
3. **Cross-Platform Native** - No platform-specific code, works identically on Windows/macOS/Linux.
4. **Testability Required** - ViewModels testable without UI dependencies using Avalonia.Headless.
5. **Design System Compliance** - Use Avalonia CSS vars; dark theme primary, light supported.

## Critical Workflows

### General Guidelines

Prefer built-in tasks/tools over CLI. Justify any direct tool invocation. Use runSubagent for code changes. Review relevant documentation in docs/ before starting work.

### Before Making Changes

Read the [Architecture documentation](docs/ARCHITECTURE.md) to understand system design. Check the [Glossary](docs/GLOSSARY.md) for unfamiliar terms. Review existing code patterns in similar files.

## Build & Run

### Development Build

```bash
dotnet build
```

### Run Application

```bash
dotnet run --project WatchTower/WatchTower.csproj
```

### Publish (per platform)

```bash
dotnet publish -c Release -r win-x64 --self-contained true
dotnet publish -c Release -r osx-x64 --self-contained true
dotnet publish -c Release -r linux-x64 --self-contained true
```

## Debugging

Press F5 in VS Code to attach the debugger. Set breakpoints in all C# files. XAML hot reload is available in Debug mode.

## dotnet watch Workflow (preferred)

### Starting Watch Mode

Start via VS Code watch task or run:

```bash
dotnet watch run --project WatchTower/WatchTower.csproj --non-interactive
```

### Watch Mode Behavior

Never kill WatchTower/dotnet processes; watch handles restarts. If file locks block builds, ask user to close app. Hot reload applies most C# edits; rude edits auto-restart (DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true). Manual restart: Ctrl+R in watch terminal.

### Environment Variables

```
DOTNET_ENVIRONMENT=Development
DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true
```

### Supported Hot Reload Edits

The following changes can be hot reloaded: modify method bodies; add methods/fields/ctors/properties/events/indexers; add nested/top-level types; async/iterator changes; lambdas/LINQ; generics; dynamic; attributes; using directives; parameter renames/type tweaks; return type changes; delete/rename members (except fields); namespace edits; partials; source-generated edits.

### Unsupported Edits (triggers restart)

The following changes trigger a restart: interface changes; abstract/virtual/override additions; destructor additions; type parameter/base/delegate changes; type deletions; catch/try-finally active changes; making abstract method concrete; embedded interop edits; lambda signature or captured-variable changes.

### When to Request Manual Shutdown

Ask for manual shutdown only when modifying csproj structure (not packages), persistent build errors, crashed watch, or missing DOTNET_WATCH_RESTART_ON_RUDE_EDIT.

## Project-Specific Conventions

### MVVM Patterns

Logic belongs in ViewModels, not code-behind. Services are registered in App.axaml.cs via DI. ViewModels receive dependencies via constructor injection.

### Configuration

Configuration is in appsettings.json. Logging:LogLevel values are minimal, normal, or verbose, mapping to Warning, Information, or Debug respectively. LoggingService loads config.

### Red Flags to Avoid

Do not put logic in code-behind. Do not use static service locators. Do not use platform-specific hacks without guards. Do not hardcode configuration values. Do not make ViewModels require UI types. Do not manually create tasks/specs.

## Testing

### Requirements

Aim for minimum 80% coverage for ViewModels and Services. Write tests before implementation when possible. ViewModels must be testable without UI dependencies.

### Frameworks

Use xUnit or NUnit for unit testing. Use Avalonia.Headless for UI testing without browser dependencies.

### Testing ViewModels

```csharp
var gameControllerService = Mock.Of<IGameControllerService>();
var adaptiveCardService = Mock.Of<IAdaptiveCardService>();
var adaptiveCardThemeService = Mock.Of<IAdaptiveCardThemeService>();
var logger = Mock.Of<ILogger<MainWindowViewModel>>();

var viewModel = new MainWindowViewModel(
    gameControllerService,
    adaptiveCardService,
    adaptiveCardThemeService,
    logger);

Assert.NotNull(viewModel.CurrentCard);
```

## Tool Usage (MCP first)

### Preferred .NET MCP Tools

Use these tools when available: list_errors, find_all_references, find_symbols, get_symbol_definition, add_member/update_member, search_package_context, gcdump_*, code_refactoring, rename_symbol, get_solution_context, source generator readers, symbol_semantic_search.

### Strategy

Favor semantic tools over text search. Reach for code_refactoring for multi-file transforms. Verify solution path exists before passing to tools.

### Confidence

Tool outputs are authoritative; avoid redundant validation. Fallback to built-ins (semantic_search, list_code_usages, file_search, replace_string_in_file, multi_replace_string_in_file, get_search_view_results, list_dir) only when MCP tools unavailable.

## C# Code Style & Quality

### Naming Conventions

Use `_camelCase` for private/internal fields. Use `s_` prefix for static fields. Use `t_` prefix for thread-static fields. Use PascalCase for constants and methods. Use `nameof` instead of string literals for member names.

### Formatting

Use Allman braces (opening brace on new line). Use 4-space indentation. Avoid `this.` qualifier. Use explicit visibility modifiers. Sort using directives with System namespaces first. Avoid extra blank lines and whitespace.

### Type Usage

Use `var` only when the type is explicit on the right-hand side. Use language keywords over BCL types (e.g., `string` not `String`). Use non-ASCII characters via \uXXXX escape sequences. Use single-line if statements only when all branches are single-line.

### Tooling

Style is enforced by .editorconfig. Run `dotnet format` (or `--verify-no-changes`) to align style.

### Package Management

Use `dotnet add/remove/list` for packages. Never hand-edit csproj for package references.

### Solution Management

Use `dotnet sln list/add/remove` for solution management.

## When Working on Features

### Before Starting

Review relevant documentation in docs/ before starting. Check the [Architecture](docs/ARCHITECTURE.md) for system design. Check the [Glossary](docs/GLOSSARY.md) for terminology. Respect task dependencies and [P] markers.

### During Development

Keep MVVM separation strict. Ensure cross-platform parity. Stay on latest stable Avalonia (currently 11.3.9, suggest upgrade when newer stable ships).

### Key Systems Reference

For game controller features, see [docs/game-controller-support.md](docs/game-controller-support.md). For voice features, see [docs/voice-setup-guide.md](docs/voice-setup-guide.md). For adaptive card theming, see [docs/ADAPTIVE-CARD-THEME-PLAN.md](docs/ADAPTIVE-CARD-THEME-PLAN.md). For startup flow, see [docs/splash-screen-startup.md](docs/splash-screen-startup.md).

## Important Notes

The message "Terminal will be reused by tasks, press any key to close it." is informational, not a prompt.

Voice features currently use NAudio for audio capture/playback, which is Windows-only. Linux and macOS support will require an alternative audio backend (e.g., SDL via Silk.NET).

---
Last Updated: 2025-12-28 | Constitution Version: 1.2.0
