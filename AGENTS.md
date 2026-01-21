# WatchTower - AI Agent Instructions

This document provides guidelines for AI agents and developers working on the WatchTower codebase. For detailed architecture information, see [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md). For codebase-specific terminology, see [docs/GLOSSARY.md](docs/GLOSSARY.md).

## FAL.AI

NOTE: the FAL.AI Key is in the local .env file when creating scripts, create them to load it from there.

## Development Environment Setup Steps

This section outlines the required steps to set up a development environment for WatchTower. These steps are also documented in the [`.github/workflows/copilot-setup-steps.yml`](.github/workflows/copilot-setup-steps.yml) workflow, which can be run to validate your environment setup.

### Prerequisites

1. **Operating System**: Windows 10/11 (WatchTower is Windows-native)
2. **.NET 10 SDK**: Install the latest .NET 10 preview SDK
   - Download from: https://dotnet.microsoft.com/download/dotnet/10.0
   - Verify installation: `dotnet --version`
3. **PowerShell**: PowerShell 7+ recommended (comes with Windows but verify version)
   - Verify installation: `$PSVersionTable` or `pwsh --version`
   - Windows PowerShell 5.1+ is minimum requirement
4. **Git**: For version control
5. **IDE**: Visual Studio Code (recommended) or Visual Studio 2022+

### Environment Configuration

#### FAL.AI API Key Setup

WatchTower uses FAL.AI for AI-powered features. You must configure the FAL_KEY environment variable:

1. **Create a `.env` file** in the project root directory (ignored by git):
   ```
   FAL_KEY=your-fal-api-key-here
   ```

2. **For GitHub Actions**: The FAL_KEY is configured as a repository secret (or environment secret) in the repository settings.

3. **Obtaining a FAL.AI Key**: Visit https://fal.ai to create an account and generate an API key.

### Setup Steps

1. **Clone the repository**:
   ```bash
   git clone https://github.com/anokye-labs/watchtower.git
   cd watchtower
   ```

2. **Verify .NET installation**:
   ```bash
   dotnet --version
   dotnet --info
   ```

3. **Verify PowerShell**:
   ```powershell
   $PSVersionTable
   ```

4. **Configure FAL_KEY**:
   - Create `.env` file in project root with your FAL.AI API key
   - The file should contain: `FAL_KEY=your-api-key`

5. **Restore dependencies**:
   ```bash
   dotnet restore WatchTower.slnx
   ```

6. **Build the solution**:
   ```bash
   dotnet build WatchTower.slnx -c Debug
   ```

7. **Run tests**:
   ```bash
   dotnet test WatchTower.slnx -c Debug --verbosity normal
   ```

8. **Run the application**:
   ```bash
   dotnet run --project WatchTower/WatchTower.csproj
   ```

   Or use watch mode for hot reload during development:
   ```bash
   dotnet watch run --project WatchTower/WatchTower.csproj --non-interactive
   ```

### Validating Your Setup

You can validate your development environment by running the setup workflow manually:

1. Go to the repository's Actions tab on GitHub
2. Select "Development Environment Setup" workflow
3. Click "Run workflow"
4. Choose "validate_only" option to skip build/test steps

Alternatively, follow steps 2-7 above to validate locally.

### Troubleshooting

- **Missing .NET 10**: Ensure you've installed the latest .NET 10 preview SDK from https://dotnet.microsoft.com/download/dotnet/10.0
- **FAL_KEY not found**: Verify `.env` file exists in project root and contains valid key
- **Build failures**: Run `dotnet clean` then `dotnet restore` before building
- **PowerShell version issues**: Update to PowerShell 7+ for best compatibility

### Additional Tools (Optional)

- **Visual Studio Code Extensions**:
  - C# Dev Kit
  - .NET Runtime Install Tool
  - Avalonia for VS Code
- **Debugging Tools**: 
  - VS Code debugging configured in `.vscode/launch.json`
  - Press F5 to attach debugger

## Project Overview

WatchTower is an Avalonia UI-based application with strict MVVM architecture and dependency injection on .NET 10. It is Windows-native, targeting win-x64 with self-contained single-file publish. The project uses only open source dependencies (MIT/Apache 2.0).

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
3. **Windows-Native** - Optimized for Windows with native audio/input integration for best-in-class experience.
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

### Publish

```bash
dotnet publish -c Release -r win-x64 --self-contained true
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

Do not put logic in code-behind. Do not use static service locators. Do not hardcode configuration values. Do not make ViewModels require UI types. Do not manually create tasks/specs.

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

Keep MVVM separation strict. Stay on latest stable Avalonia (currently 11.3.9, suggest upgrade when newer stable ships). Leverage Windows-specific features when they enhance user experience.

### Key Systems Reference

For game controller features, see [docs/game-controller-support.md](docs/game-controller-support.md). For voice features, see [docs/voice-setup-guide.md](docs/voice-setup-guide.md). For adaptive card theming, see [docs/ADAPTIVE-CARD-THEME-PLAN.md](docs/ADAPTIVE-CARD-THEME-PLAN.md). For startup flow, see [docs/splash-screen-startup.md](docs/splash-screen-startup.md).

## Agent Flow Project Management

Work is tracked via the [Agent Flow System](docs/agent-workflow.md).

**Key Rules:**
- Check Dependencies field before starting any Ready task
- [P] prerequisite markers → Priority field: [P0]=P0, [P1]=P1, etc.
- Set Status=Blocked if blocked; always populate Dependencies with blocker issue numbers
- When encountering `requires:human-decision` label, do NOT proceed - set Blocked and assign to @hoopsomuah
- Read `.github/agent-config.yml` for full agent behavior rules

## Important Notes

The message "Terminal will be reused by tasks, press any key to close it." is informational, not a prompt.

Voice features use NAudio for native Windows audio integration, providing reliable and high-performance audio capture/playback on Windows 10/11.

---
Last Updated: 2025-12-28 | Constitution Version: 1.2.0
