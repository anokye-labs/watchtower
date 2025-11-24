# WatchTower - AI Agent Instructions

## Project Architecture

**WatchTower** is a cross-platform desktop application built with **Avalonia UI** (.NET 10) following strict **MVVM pattern** with dependency injection. The project uses a **specification-driven development workflow** (Spec-Kit) where features begin as specs, evolve into plans, and decompose into tasks.

### Key Structural Decisions

- **MVVM Enforcement**: ViewModels contain ALL logic; Views (XAML) contain ONLY presentation/bindings. No code-behind logic except initialization.
- **Service Layer**: Business logic lives in `Services/`, injected into ViewModels via DI. ViewModels orchestrate services; services encapsulate operations.
- **Cross-Platform First**: Windows/macOS/Linux are equal targets. Project configured for self-contained, single-file deployment per platform (`RuntimeIdentifiers: win-x64;osx-x64;linux-x64`).
- **Latest Avalonia**: Use most recent stable Avalonia (currently 11.3.9). Proactively suggest upgrades when new versions release.

### Directory Structure

```
WatchTower/               # Main application project
  Models/                 # Data structures (currently empty, to be populated per feature)
  ViewModels/             # UI logic and state management (currently empty)
  Views/                  # XAML UI definitions (MainWindow.axaml exists)
  Services/               # Business logic (LoggingService.cs present)
  App.axaml[.cs]          # Application lifecycle and DI setup
  Program.cs              # Entry point with error handling
  appsettings.json        # Configuration (logging levels, etc.)

.specify/                 # Specification-driven workflow
  memory/constitution.md  # Project principles (MVVM, cross-platform, DI, testing)
  templates/              # Spec, plan, task, checklist templates
  scripts/powershell/     # Workflow automation scripts

.github/agents/           # AI agent definitions for Spec-Kit workflow
  speckit.*.agent.md      # Agents for specify, plan, tasks, implement, analyze, etc.

specs/NNN-feature-name/   # Feature specifications (one per branch)
  spec.md                 # Requirements and user stories
  plan.md                 # Technical design and architecture
  tasks.md                # Implementation task breakdown
  checklists/             # Quality validation checklists
```

## Critical Workflows

### General Instructions
* Use `runSubagent` when working on code to encapsulate changes.
* ALWAYS prefer internal tools over invoking scripts through the terminal. When you must invoke a tool, provide sufficient justification

### Spec-Kit Feature Development

The project uses a structured workflow with PowerShell scripts in `.specify/scripts/powershell/`:

1. **Create Feature**: `create-new-feature.ps1 -Json <description>` → Creates branch, initializes spec
2. **Clarify Spec**: Use `speckit.clarify` agent to resolve ambiguities interactively
3. **Generate Plan**: `setup-plan.ps1` → Creates `plan.md` with architecture, data models, contracts
4. **Generate Tasks**: Use `speckit.tasks` agent → Produces `tasks.md` with ordered, parallelizable tasks
5. **Analyze Quality**: Use `speckit.analyze` agent → Validates consistency across spec/plan/tasks
6. **Implement**: Use `speckit.implement` agent → Executes tasks in dependency order

**Key Script**: `check-prerequisites.ps1` - Run with `-Json` to get feature paths and validation status. Used by all agents.

### Build & Run

```bash
# Development build (Debug with hot reload)
dotnet build

# Run application
dotnet run --project WatchTower/WatchTower.csproj

# Publish self-contained single-file for specific platform
dotnet publish -c Release -r win-x64 --self-contained true
dotnet publish -c Release -r osx-x64 --self-contained true
dotnet publish -c Release -r linux-x64 --self-contained true
```

### Debugging in VS Code

- Press **F5** to launch with debugger attached
- Breakpoints work in all C# files (ViewModels, Services, code-behind)
- Hot reload enabled for XAML changes (Debug mode only)
- Simple C# hot reload supported (method bodies only)
- Press **F12** in running app to open DevTools (Debug mode)

### Hot Reload Capabilities

**XAML Hot Reload** (Always works in Debug mode):
- Text, colors, layouts, control properties
- Changes appear within 1-2 seconds of save
- Powered by `Avalonia.Diagnostics` package
- Application state preserved during reload

**C# Hot Reload** (Supported changes):
- ✅ Method body modifications
- ✅ Property value updates  
- ✅ Event handler logic changes
- ❌ Adding/removing methods (requires restart)
- ❌ Changing signatures/types (requires restart)
- ❌ Constructor modifications (requires restart)

**Configuration**:
- `Properties/launchSettings.json`: `"hotReloadEnabled": true`
- `WatchTower.csproj`: `Avalonia.Diagnostics` in Debug mode only
- Hot reload timing logged in console output

## Project-Specific Conventions

### MVVM Pattern (NON-NEGOTIABLE)

**Correct ViewModel Example** (logic in ViewModel):
```csharp
public class MainWindowViewModel : ViewModelBase
{
    private readonly IMyService _service;
    private string _statusText;
    
    public MainWindowViewModel(IMyService service)
    {
        _service = service;
        LoadDataCommand = ReactiveCommand.CreateFromTask(LoadDataAsync);
    }
    
    public string StatusText
    {
        get => _statusText;
        set => this.RaiseAndSetIfChanged(ref _statusText, value);
    }
    
    public ICommand LoadDataCommand { get; }
    
    private async Task LoadDataAsync() 
    {
        StatusText = await _service.GetStatusAsync();
    }
}
```

**WRONG** (logic in code-behind):
```csharp
// MainWindow.axaml.cs - DON'T DO THIS
private void Button_Click(object sender, RoutedEventArgs e)
{
    // ❌ NO LOGIC HERE - belongs in ViewModel
    StatusText.Text = _service.GetStatus();
}
```

### Service Pattern

Services are injected into ViewModels and registered in `App.axaml.cs`:

```csharp
// App.axaml.cs
public override void OnFrameworkInitializationCompleted()
{
    var services = new ServiceCollection();
    services.AddSingleton<ILoggingService, LoggingService>();
    services.AddTransient<MainWindowViewModel>();
    var provider = services.BuildServiceProvider();
    
    desktop.MainWindow = new MainWindow 
    { 
        DataContext = provider.GetRequiredService<MainWindowViewModel>() 
    };
}
```

### Configuration

- `appsettings.json` controls logging verbosity: `"Logging:LogLevel": "minimal|normal|verbose"`
- Maps to `LogLevel.Warning`, `LogLevel.Information`, `LogLevel.Debug`
- LoggingService loads configuration automatically

### Testing Requirements

- **Minimum 80% coverage** for ViewModels and Services
- ViewModels must be testable without UI dependencies (no Avalonia types in constructor)
- Use xUnit or NUnit for test framework
- Tests written BEFORE implementation (TDD strongly recommended per constitution)

## Constitution Compliance

The `.specify/memory/constitution.md` defines **non-negotiable principles**:

1. **MVVM Architecture** - ViewModels have logic, Views have presentation
2. **Service Layer** - Business logic in services, ViewModels orchestrate
3. **Test-First Development** - Write tests before implementation
4. **Dependency Injection** - All dependencies injected via DI container
5. **Cross-Platform Native** - Windows/macOS/Linux equal support
6. **Latest Avalonia** - Stay current with stable releases

Code reviews MUST verify MVVM compliance. Architecture violations MUST be rejected.

## Common Integration Points

- **Logging**: Use `LoggingService.CreateLogger<T>()` in all components
- **Configuration**: Load via `IConfiguration` from `appsettings.json`
- **DI Registration**: Add services in `App.OnFrameworkInitializationCompleted()`
- **Cross-platform rendering**: Use Avalonia's platform detection (`UsePlatformDetect()`)

## When Working on Features

1. **Always check the constitution first** (`.specify/memory/constitution.md`)
2. **Read the spec** (`specs/NNN-feature-name/spec.md`) for requirements and user stories
3. **Reference the plan** (`plan.md`) for architecture decisions and data models
4. **Follow tasks sequentially** (`tasks.md`) respecting dependencies and [P] parallel markers
5. **Maintain MVVM separation** - if logic touches Views, move it to ViewModels
6. **Test ViewModels independently** - no UI dependencies in tests

## Red Flags to Avoid

- ❌ Code-behind logic in `.axaml.cs` files (except initialization)
- ❌ Static service locators or singletons outside DI
- ❌ Platform-specific code without conditional compilation guards
- ❌ Hardcoded configuration values (use `appsettings.json`)
- ❌ ViewModels that cannot be instantiated without UI types
- ❌ Creating tasks/specs manually (use Spec-Kit scripts and agents)

---

**Last Updated**: 2025-11-24 | **Constitution Version**: 1.1.0
