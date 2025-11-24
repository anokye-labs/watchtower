# Implementation Plan: Avalonia Development Environment Setup

**Branch**: `001-avalonia-dev-setup` | **Date**: November 24, 2025 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-avalonia-dev-setup/spec.md`

## Summary

Create a barebones Avalonia desktop application targeting .NET 10 with comprehensive VS Code integration for rapid UI development. The application will feature a single main window with "Hello World" content, full debugging support, XAML hot reload capabilities, and build to a self-contained executable with 99.99% reliability target. Key focus areas: VS Code launch/debug configurations, hot reload for both XAML and simple C# changes, configurable logging, and cross-platform support (Windows/macOS/Linux).

## Technical Context

**Language/Version**: C# with .NET 10 (explicitly specified in project configuration)

**Primary Dependencies**:

- Avalonia UI Framework (11.x - latest stable supporting .NET 8+, forward compatible with .NET 10)
- Avalonia.Desktop (desktop platform support)
- Avalonia.Diagnostics (for hot reload and DevTools)
- .NET Hot Reload SDK components (Edit and Continue infrastructure)

**Storage**: N/A (no persistent storage for this scaffolding phase)

**Testing**: MSTest or xUnit for future unit/integration tests (not required for Phase 0-1)

**Target Platform**: Cross-platform desktop (Windows 10+, macOS 10.15+, Linux with X11/Wayland)

**Project Type**: Single desktop application project

**Performance Goals**:

- Application launch < 5 seconds
- Hot reload completion < 2 seconds
- Clean build < 30 seconds
- UI render 60 fps minimum

**Constraints**:

- Self-contained deployment (includes all runtime dependencies)
- 99.99% reliability target for executable (near-zero runtime failures)
- Hot reload must preserve application state
- Support simple C# hot reload (method bodies) and XAML changes
- Complex structural changes require restart

**Scale/Scope**:

- Single main window application
- Minimal UI (Hello World message)
- Foundation for future UI development iterations
- Configurable logging (3 levels: minimal/normal/verbose)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Status**: ✅ COMPLIANT (Constitution template not yet populated with project-specific rules)

**Notes**: This is a development environment scaffolding feature focused on tooling and configuration rather than application logic. Once project constitution is established, this check should verify:

- Project structure aligns with defined standards
- Build and deployment patterns follow project conventions
- Testing approach matches project requirements
- Documentation standards are met

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
WatchTower/                          # Main Avalonia application project
├── WatchTower.csproj                # Project file with .NET 10 target, NuGet packages
├── Program.cs                       # Application entry point, lifetime configuration
├── App.axaml                        # Application-level XAML (styles, resources)
├── App.axaml.cs                     # Application code-behind (initialization, logging setup)
├── Views/
│   ├── MainWindow.axaml             # Main window XAML definition
│   └── MainWindow.axaml.cs          # Main window code-behind
├── ViewModels/                      # MVVM ViewModels (future)
├── Models/                          # Data models (future)
├── Services/                        # Application services (future)
│   └── LoggingService.cs            # Configurable logging service
└── Assets/                          # Images, fonts, resources

.vscode/                             # VS Code workspace configuration
├── launch.json                      # Debug configurations (Run/Debug modes)
├── tasks.json                       # Build, clean, run tasks
├── settings.json                    # Workspace settings
└── extensions.json                  # Recommended extensions

.config/                             # Configuration files
└── dotnet-tools.json                # .NET local tools manifest (if needed)

WatchTower.Tests/                    # Test project (future)
├── WatchTower.Tests.csproj
├── Unit/
├── Integration/
└── Fixtures/
```

**Structure Decision**: Standard Avalonia desktop application structure using MVVM pattern foundation. The application uses Avalonia's naming convention (`.axaml` for XAML files) and separates Views, ViewModels, Models, and Services. VS Code configuration is workspace-level to support debugging and hot reload. Self-contained publish configuration is in the `.csproj` file.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

**Status**: No violations - this is foundational scaffolding with minimal complexity.

## Phase 0: Research & Technology Selection

**Status**: ✅ COMPLETED (research conducted via DeepWiki and Microsoft Docs)

### Key Research Findings

1. **Avalonia Framework Version**
   - Latest stable: Avalonia 11.x series
   - Requires .NET 8.0 SDK minimum (no explicit .NET 10 in docs, but forward compatible)
   - Primary packages: `Avalonia` and `Avalonia.Desktop`
   - NuGet: Installed via standard NuGet package manager

2. **Hot Reload Capabilities**
   - **XAML Hot Reload**: Built-in via `Avalonia.Diagnostics` and design-time infrastructure
   - Uses `AvaloniaXamlIlRuntimeCompiler` for dynamic XAML loading
   - Enabled through `AvaloniaFilePreview` target in build tasks
   - **C# Hot Reload**: Leverages .NET Hot Reload (Edit and Continue)
   - Simple changes (method bodies, properties) supported
   - Complex changes (signatures, types, constructors) require restart
   - Configured via `launchSettings.json` with `hotReloadEnabled` flag

3. **VS Code Integration**
   - **Essential Extensions**:
     - `avaloniateam.vscode-avalonia` (official Avalonia tooling) - **INSTALLED**
     - `ms-dotnettools.csharp` (C# language support, debugger) - **INSTALLED**
     - `nromanov.dotnet-meteor` (run/debug .NET MAUI/Avalonia apps)
     - `rogalmic.vscode-xml-complete` (XAML completion)
   - **Configuration Files**:
     - `launch.json`: Debug configurations with `coreclr` debugger
     - `tasks.json`: Build tasks using `dotnet build/run/publish`
     - `settings.json`: Avalonia-specific settings
     - `extensions.json`: Recommended extensions list

4. **Self-Contained Deployment**
   - Publish configuration: `<PublishSingleFile>true</PublishSingleFile>`
   - Runtime identifier per platform: `win-x64`, `osx-x64`, `linux-x64`
   - Self-contained: `<SelfContained>true</SelfContained>`
   - Trimming options for size optimization (optional)
   - Diagnostic features for 99.99% reliability: structured error messages

5. **Logging Infrastructure**
   - Built-in: `Microsoft.Extensions.Logging`
   - Levels: Trace, Debug, Information, Warning, Error, Critical
   - Configuration: JSON config file or environment variables
   - Output: Console, Debug window, file sinks (Serilog optional)

### Technology Stack Decisions

| Component | Choice | Rationale |
|-----------|--------|-----------|
| UI Framework | Avalonia 11.x | Cross-platform, mature XAML support, active development |
| Target Framework | .NET 10 | Specified requirement, forward compatible from .NET 8 |
| XAML Hot Reload | Avalonia.Diagnostics | Built-in framework support, proven reliability |
| C# Hot Reload | .NET Hot Reload | Standard .NET capability, VS Code integration |
| Debugger | C# Extension (coreclr) | Official Microsoft debugger, full feature support |
| Build Tool | dotnet CLI | Standard .NET tooling, VS Code task integration |
| Logging | Microsoft.Extensions.Logging | Built-in, configurable, extensible |
| Package Manager | NuGet | Standard .NET ecosystem |
## Phase 1: Design & Contracts

**Output**: data-model.md, contracts/, quickstart.md, .github/copilot-instructions.md

### Data Model (data-model.md)

**Status**: N/A for this feature

This feature is infrastructure/scaffolding focused with no domain data entities. Future application features will define data models as needed.

### API Contracts (contracts/)

**Status**: N/A for this feature

No external API contracts required. This is a standalone desktop application. Future features may add:

- Inter-process communication contracts (if needed)
- Plugin/extension interfaces (if needed)
- Configuration file schemas

### Quick Start Guide (quickstart.md)

**Content**: Step-by-step developer onboarding guide

1. **Prerequisites**
   - Install .NET 10 SDK
   - Install VS Code
   - Clone repository

2. **Setup Steps**
   - Open workspace in VS Code
   - Install recommended extensions (prompted automatically)
   - Run `dotnet restore` or use VS Code task

3. **Development Workflow**
   - Press F5 to launch with debugger
   - Edit XAML in `Views/MainWindow.axaml` - see changes hot reload
   - Edit C# method bodies - see changes hot reload
   - Add/remove methods - restart required (app will notify)

4. **Build & Run**
   - Debug mode: F5 or "Run and Debug"
   - Without debugger: `dotnet run`
   - Build: `dotnet build` or VS Code task "build"
   - Publish: `dotnet publish -c Release -r <rid> --self-contained`

5. **Troubleshooting**
   - Check Hot Reload output window for diagnostics
   - Verify .NET 10 SDK: `dotnet --version`
   - Check logging level in `appsettings.json`

### Agent Context Update

**Action**: Update `.github/copilot-instructions.md` with:

- Avalonia 11.x as primary UI framework
- .NET 10 as target framework
- MVVM architecture pattern
- Hot reload capabilities and limitations
- VS Code as primary IDE with specific extensions
- Self-contained deployment approach

## Phase 2: Implementation Tasks

**Note**: This phase is handled by `/speckit.tasks` command (NOT part of `/speckit.plan`).

The following is a preview of task categories that will be generated:

### Task Categories (Preview)

1. **Project Setup**
   - Create WatchTower.csproj with .NET 10 target
   - Add Avalonia NuGet packages
   - Configure self-contained publish settings
   - Set up project structure (folders)

2. **Application Core**
   - Implement Program.cs entry point
   - Create App.axaml with basic styles
   - Implement App.axaml.cs with logging initialization
   - Configure application lifetime

3. **Main Window UI**
   - Create MainWindow.axaml with Hello World content
   - Implement MainWindow.axaml.cs code-behind
   - Set window title to "WatchTower"
   - Configure window properties (size, position)

4. **VS Code Integration**
   - Create launch.json with Run/Debug configurations
   - Create tasks.json with build/clean/run tasks
   - Create settings.json for Avalonia settings
   - Create extensions.json with recommended extensions

5. **Hot Reload Configuration**
   - Enable Avalonia.Diagnostics package
   - Configure launchSettings.json for hot reload
   - Add hot reload notification service
   - Document hot reload limitations

6. **Logging Infrastructure**
   - Implement LoggingService with configurable levels
   - Create appsettings.json for configuration
   - Add startup logging (milestones)
   - Configure console and debug output

7. **Testing & Validation**
   - Verify application launches < 5 seconds
   - Verify hot reload works for XAML changes
   - Verify hot reload works for simple C# changes
   - Verify debugger attaches and breakpoints work
   - Verify self-contained publish works
   - Test on Windows/macOS/Linux

8. **Documentation**
   - Complete quickstart.md
   - Add inline code comments
   - Document logging configuration
   - Create README.md for repository

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|-----------|
| .NET 10 SDK not available | High | Use .NET 8 as fallback, forward compatible |
| Avalonia 11 incompatibility with .NET 10 | High | Verify with test project, use latest preview if needed |
| Hot reload unreliable | Medium | Document limitations clearly, provide restart workflow |
| VS Code debugging issues | Medium | Provide detailed launch.json, document troubleshooting |
| Cross-platform build differences | Medium | Test on all platforms, document platform-specific issues |
| Self-contained size too large | Low | Use trimming options, document size vs. compatibility tradeoff |

## Success Criteria Mapping

| Requirement | Implementation | Validation |
|-------------|----------------|------------|
| SC-001: Launch < 5s | Optimize startup, measure with logging | Stopwatch in Program.cs |
| SC-002: Debugger 100% reliable | Standard VS Code C# debugger | Manual breakpoint tests |
| SC-003: Hot reload < 2s | Use Avalonia.Diagnostics | Timer in hot reload handler |
| SC-004: Edit-save-view < 5s | Hot reload + auto-save | Manual timing test |
| SC-005: Cross-platform | Self-contained publish | Test builds on 3 platforms |
| SC-006: Build < 30s | Minimal dependencies | Measure `dotnet build` time |
| SC-007: 95% hot reload success | Standard XAML/C# changes | Test common scenarios |
| SC-008: 2-command CLI | `dotnet build && dotnet run` | Document in quickstart.md |
| SC-009: Error display < 2s | Exception handlers, UI feedback | Introduce syntax errors |
| SC-010: Onboard < 10 min | Clear quickstart.md | Fresh dev machine test |
| SC-011: 99.99% reliability | Error handling, diagnostics | Stress testing |
| SC-012: Clear diagnostics | Structured error messages | Test failure scenarios |

## Next Steps

1. Review this plan for completeness
2. Run `/speckit.tasks` to generate detailed task breakdown
3. Begin Phase 2 implementation following task order
4. Update quickstart.md as implementation progresses
5. Test on target platforms continuously
6. Document any deviations or discoveries

---

**Plan Status**: ✅ COMPLETE - Ready for task generation
**Last Updated**: November 24, 2025
