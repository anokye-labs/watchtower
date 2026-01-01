# Splash Screen Startup Flow

## Overview

The WatchTower application uses an enhanced splash screen to provide visual feedback during application startup. The splash screen features the application logo, Ancestral Futurism branding, granular progress tracking with 10 detailed steps, and an animated Gye Nyame symbol. This document describes how the splash screen works, how to modify startup phases, and how to use the diagnostics panel.

## Architecture

### Components

1. **SplashWindow** (`Views/SplashWindow.axaml`)
   - Borderless, centered window with Ancestral Futurism styling
   - Displays logo, tagline ("Ancestral Futurism AI Framework"), version number
   - Shows animated Gye Nyame symbol during startup
   - Displays progress bar (Holographic Cyan) with percentage and current phase
   - Shows status message, elapsed time, and optional diagnostics
   - Keyboard shortcuts: `D` (toggle diagnostics), `ESC` (exit)

2. **SplashWindowViewModel** (`ViewModels/SplashWindowViewModel.cs`)
   - Implements `IStartupLogger` to capture startup messages
   - Tracks progress percentage (0-100) and current phase description
   - Manages timer for elapsed time display
   - Handles hang detection based on configurable threshold
   - Provides diagnostics message collection

3. **StartupOrchestrator** (`Services/StartupOrchestrator.cs`)
   - Orchestrates the multi-phase startup workflow with 10 granular steps
   - Reports detailed progress to `IStartupLogger` using `ReportProgress` method
   - Wraps initialization in try/catch for error handling

4. **IStartupLogger** (`Services/IStartupLogger.cs`)
   - Interface for logging startup progress
   - Methods: `Info(string)`, `Warn(string)`, `Error(string, Exception?)`, `ReportProgress(int, int, string)`

## Startup Flow

1. **Framework Initialization** (`App.OnFrameworkInitializationCompleted`)
   - Loads configuration from `appsettings.json`
   - Creates and shows `ShellWindow` with `SplashWindow` content immediately
   - Starts async startup workflow in background

2. **Startup Phases** (orchestrated by `StartupOrchestrator` - 10 steps)
   - **Step 1**: Initialize environment
   - **Step 2**: Load configuration
   - **Step 3**: Setup dependency injection
   - **Step 4**: Register logging services
   - **Step 5**: Register core services (UserPreferences, AdaptiveCard, GameController)
   - **Step 6**: Register MCP handler
   - **Step 7**: Register voice services (based on configured mode)
   - **Step 8**: Build service provider
   - **Step 9**: Initialize game controller
   - **Step 10**: Initialize voice services

3. **Success Path**
   - Mark startup complete (shows success checkmark)
   - Animate shell window expansion (500ms)
   - Transition to main content
   - Check for first-run and show welcome screen if needed

4. **Failure Path**
   - Log error to diagnostics
   - Mark startup as failed
   - Keep `SplashWindow` open with diagnostics visible
   - User can review error and exit

## Configuration

### appsettings.json

```json
{
  "Startup": {
    "HangThresholdSeconds": 30
  }
}
```

- **HangThresholdSeconds**: Time in seconds before showing "slow startup" warning

## Adding or Modifying Startup Phases

To add a new startup phase:

1. Open `Services/StartupOrchestrator.cs`
2. Add your phase in `ExecuteStartupAsync` method
3. Use the `logger` parameter to report progress:

```csharp
logger.Info("Phase N/M: Your phase description...");
await YourInitializationMethodAsync();
logger.Info("Your phase completed successfully");
```

**Best Practices:**
- Always wrap phases in try/catch within the orchestrator
- Log start and completion of each phase
- Use descriptive phase names
- Report warnings for non-critical failures
- Report errors for critical failures

## Diagnostics Panel

### Usage

- **Toggle**: Press `D` key
- **Auto-show**: Opens automatically on startup failure
- **Content**: Timestamped startup messages (INFO, WARN, ERROR)
- **Scrolling**: Last 500 messages kept (rolling window)

### Message Format

```
[HH:mm:ss.fff] LEVEL: Message
```

Example:
```
[14:23:45.123] INFO: Phase 1/4: Loading configuration...
[14:23:45.234] INFO: Configuration loaded successfully
[14:23:45.345] WARN: Game controller service initialization failed
```

### Error Details

When exceptions occur, the diagnostics panel shows:
- Error message
- Exception type and message
- First line of stack trace

## Hang Detection

If startup exceeds the configured threshold (default 30 seconds):
- Status message changes to "Startup is taking longer than expected..."
- Warning indicator appears
- Warning logged to diagnostics
- Application continues (doesn't auto-exit)
- User can:
  - Wait for completion
  - View diagnostics for details
  - Exit using ESC or X button

## Exit Behavior

User can exit at any time during startup:
- Press `ESC` key
- Click the `X` button in top-right corner

Both trigger a clean application shutdown via the lifetime manager.

## Customization

### Visual Appearance

Edit `Views/SplashWindow.axaml` to customize:
- Window size (default: 600x400)
- Colors and styling
- Logo/branding
- Layout and spacing

### Timer Interval

Modify `SplashWindowViewModel` constructor:
```csharp
_timer = new DispatcherTimer
{
    Interval = TimeSpan.FromMilliseconds(100) // Adjust tick rate
};
```

### Diagnostics Message Limit

Modify `TrimDiagnosticMessages()` in `SplashWindowViewModel`:
```csharp
while (DiagnosticMessages.Count > 500) // Adjust limit
```

## Testing

### Simulating Slow Startup

In `StartupOrchestrator.ExecuteStartupAsync`, add delays:
```csharp
await Task.Delay(5000); // 5 second delay
```

### Simulating Startup Failure

Throw an exception in any startup phase:
```csharp
throw new InvalidOperationException("Simulated failure");
```

### Verifying Hang Detection

Set a low threshold in `appsettings.json`:
```json
"Startup": {
  "HangThresholdSeconds": 5
}
```

## Troubleshooting

**Splash doesn't show:**
- Check that `App.OnFrameworkInitializationCompleted` creates `SplashWindow` first
- Verify `desktop.MainWindow = splashWindow` is set before `Show()`

**Diagnostics not appearing:**
- Ensure `IStartupLogger` implementation calls `Dispatcher.UIThread.Post` for UI updates
- Check that messages are added to `DiagnosticMessages` collection

**Hang detection not working:**
- Verify `appsettings.json` is being loaded correctly
- Check timer is started in `SplashWindowViewModel` constructor
- Confirm `HangThresholdSeconds` configuration value

**Main window not showing after startup:**
- Check for exceptions in `ExecuteStartupAsync`
- Verify `MarkStartupComplete()` is called
- Ensure main window creation is on UI thread (`Dispatcher.UIThread.InvokeAsync`)
