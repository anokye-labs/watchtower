# WatchTower Glossary

This glossary defines codebase-specific terms used throughout the WatchTower application. Terms are organized by category for easier reference.

## Application Architecture

### Ancestral Futurism

The design language combining modern UI with West African cultural elements. The color palette includes holographic cyan (#00F0FF), Ashanti gold (#FFD700), mahogany (#4A1812), and void black (#050508). This theme is applied throughout the application's visual design, from focus indicators to container styles.

**See**: `docs/ADAPTIVE-CARD-THEME-PLAN.md` lines 12-40

### ShellWindow

The primary application window container that hosts both splash and main content. It manages frame rendering, window animations, and content transitions. The ShellWindow replaces the need for separate splash and main windows by providing a unified container with smooth animated transitions.

**Files**: `WatchTower/Views/ShellWindow.axaml`, `WatchTower/ViewModels/ShellWindowViewModel.cs`

### StartupOrchestrator

A service that executes the four-phase application initialization process. The phases are: (1) configuration loading, (2) DI container setup, (3) service registration, and (4) service initialization. It implements `IStartupOrchestrator` and reports progress via `IStartupLogger`.

**File**: `WatchTower/Services/StartupOrchestrator.cs`

### ExecuteStartupAsync()

The async method in `App.axaml.cs` that orchestrates the entire startup process. It calls `StartupOrchestrator`, sets up the gamepad polling timer, triggers the window expansion animation, and transitions to main content.

**File**: `WatchTower/App.axaml.cs`

### AnimateExpansionAsync()

A method in `ShellWindow` that animates the window from the initial splash size to fullscreen over 500ms using cubic ease-out easing. It uses Avalonia's Animation system with KeyFrame animations.

**File**: `WatchTower/Views/ShellWindow.axaml.cs`

### TransitionToMainContent()

A method in `ShellWindowViewModel` that switches `CurrentContent` from `SplashWindowViewModel` to `MainWindowViewModel`. This triggers the XAML DataTemplate change that displays the main application interface.

**File**: `WatchTower/ViewModels/ShellWindowViewModel.cs`

## Splash Screen System

### SplashWindowViewModel

The ViewModel for the startup screen. It implements `IStartupLogger` to capture startup messages, tracks elapsed time, manages the diagnostics panel, and provides `ExitCommand` and `ToggleDiagnosticsCommand` for user interaction.

**File**: `WatchTower/ViewModels/SplashWindowViewModel.cs`

### IStartupLogger

An interface for logging startup progress. It provides three methods: `Info(string)` for informational messages, `Warn(string)` for warnings, and `Error(string, Exception?)` for errors. Implemented by `SplashWindowViewModel` to display messages in the diagnostics panel.

**File**: `WatchTower/Services/IStartupLogger.cs`

### DiagnosticMessages

An `ObservableCollection<string>` in `SplashWindowViewModel` that stores timestamped log entries. It maintains a maximum of 500 messages using a rolling window and auto-scrolls to the bottom when new messages are added.

**File**: `WatchTower/ViewModels/SplashWindowViewModel.cs`

### HangThresholdSeconds

A configuration value (default 30) that determines when to show a slow startup warning. If the elapsed startup time exceeds this threshold, the splash screen displays a "Taking longer than expected..." warning.

**Config**: `appsettings.json` -> `Startup:HangThresholdSeconds`

### IsSlowStartup

A boolean property in `SplashWindowViewModel` that becomes true when the startup duration exceeds `HangThresholdSeconds`. It triggers the display of a warning indicator on the splash screen.

**File**: `WatchTower/ViewModels/SplashWindowViewModel.cs`

### MarkStartupComplete()

A method called by `App.ExecuteStartupAsync()` to signal successful initialization. It stops the timer, sets `IsStartupComplete = true`, and updates the status message to indicate completion.

**File**: `WatchTower/ViewModels/SplashWindowViewModel.cs`

## Main UI System

### MainWindowViewModel

The primary application ViewModel that manages overlay state (`InputOverlayMode`), command routing, gamepad event handling, and Adaptive Card display. It orchestrates the main user interface interactions.

**File**: `WatchTower/ViewModels/MainWindowViewModel.cs`

### InputOverlayMode

An enum defining overlay states: `None`, `RichText`, `Voice`, and `EventLog`. Changes to this property in `MainWindowViewModel` trigger UI animations that show or hide the corresponding overlay panels.

**File**: `WatchTower/ViewModels/MainWindowViewModel.cs`

### IsInputOverlayVisible

A computed boolean property that returns true if `CurrentInputMode` is `RichText` or `Voice`. It triggers the bottom-slide overlay animation in the View when the value changes.

**File**: `WatchTower/ViewModels/MainWindowViewModel.cs`

### IsEventLogVisible

A computed boolean property that returns true if `CurrentInputMode` is `EventLog`. It triggers the left-slide overlay animation for the controller event log panel.

**File**: `WatchTower/ViewModels/MainWindowViewModel.cs`

### ShowRichTextInputCommand

An `ICommand` in `MainWindowViewModel` that sets `CurrentInputMode = RichText`. It can be triggered by Ctrl+R or a gamepad button and displays the text input overlay.

**File**: `WatchTower/ViewModels/MainWindowViewModel.cs`

### CloseOverlayCommand

An `ICommand` that resets `CurrentInputMode = None`. It can be triggered by the Escape key, backdrop tap, or close buttons and hides all overlays.

**File**: `WatchTower/ViewModels/MainWindowViewModel.cs`

### ToggleEventLogCommand

An `ICommand` that toggles `IsEventLogVisible` state. It is triggered by Ctrl+L and shows or hides the controller event log panel.

**File**: `WatchTower/ViewModels/MainWindowViewModel.cs`

## Frame Slicing System

### FrameSliceService

A service that loads and slices decorative frame images into a 5x5 grid. It extracts 16 border pieces (corners and edges) and implements an LRU-5 cache by resolution for performance.

**File**: `WatchTower/Services/FrameSliceService.cs`

### FrameSliceDefinition

A record containing 8 pixel coordinates that define the 5x5 grid boundaries: `Left`, `LeftInner`, `RightInner`, `Right`, `Top`, `TopInner`, `BottomInner`, and `Bottom`. These coordinates determine how the source image is divided into border pieces.

**File**: `WatchTower/Services/IFrameSliceService.cs`

### FrameSlices

A record containing 16 `Bitmap` properties representing the extracted frame border pieces: TopLeft, TopLeftStretch, TopCenter, TopRightStretch, TopRight, and so on. Used by `ShellWindowViewModel` to render the decorative frame.

**File**: `WatchTower/Services/IFrameSliceService.cs`

### LoadResizeAndSlice()

A method in `FrameSliceService` that resizes the source image to the target resolution, scales slice coordinates proportionally, and extracts the border regions. It uses an LRU-5 cache to avoid redundant processing.

**File**: `WatchTower/Services/FrameSliceService.cs`

## Game Controller Integration

### IGameControllerService

The interface for SDL2 game controller integration. It provides methods like `Initialize()`, `Update()`, and `GetControllerState()`, along with events for `ButtonPressed`, `ButtonReleased`, and connection changes.

**File**: `WatchTower/Services/IGameControllerService.cs`

### GameControllerState

A model representing the current state of a controller. It includes a `ButtonStates` dictionary, analog stick positions (`LeftStickX`, `LeftStickY`, `RightStickX`, `RightStickY`), trigger values, and `IsConnected` status.

**File**: `WatchTower/Models/GameControllerState.cs`

### GameControllerButton

An enum of standard controller buttons: `A`, `B`, `X`, `Y`, `DPadUp`, `DPadDown`, `DPadLeft`, `DPadRight`, `LeftShoulder`, `RightShoulder`, `Start`, `Back`, and others.

**File**: `WatchTower/Models/GameControllerButton.cs`

### Dead Zone

A configurable threshold (default 0.15) applied to analog stick inputs to ignore minor movements caused by stick drift. Values below this threshold are treated as zero. Configured in `appsettings.json` -> `Gamepad:DeadZone`.

**File**: `WatchTower/Services/GameControllerService.cs`

## Adaptive Card System

### IAdaptiveCardService

The interface for loading Adaptive Cards from JSON and handling actions. It provides `LoadCardFromJson()` and events for different action types: `SubmitAction`, `OpenUrlAction`, `ExecuteAction`, and `ShowCardAction`.

**See**: `docs/ADAPTIVE-CARD-THEME-PLAN.md`

### IAdaptiveCardThemeService

The interface for generating themed `AdaptiveHostConfig`. The `GetHostConfig()` method returns a configuration with Ancestral Futurism colors, fonts, and spacing applied.

**See**: `docs/ADAPTIVE-CARD-THEME-PLAN.md`

### AdaptiveHostConfig

A configuration object that defines how Adaptive Cards are rendered. It specifies foreground colors, font families and sizes, spacing values, and action configurations. Generated by `AdaptiveCardThemeService` with the Ancestral Futurism theme.

**See**: `docs/ADAPTIVE-CARD-THEME-PLAN.md`

### IFocusNavigationService (planned)

A planned interface for managing UI focus with gamepad input. It is intended to provide `MoveFocus(direction)` and `ActivateFocusedElement()` methods and integrate with Avalonia's XYFocus system, but is not yet implemented in the current codebase.

**See (design/plan)**: `docs/ADAPTIVE-CARD-THEME-PLAN.md`

## Voice System

### IVoiceOrchestrationService

The coordinator service for full-duplex voice operations. It manages both speech recognition and text-to-speech services, handling mode selection and barge-in control.

**File**: `WatchTower/Services/IVoiceOrchestrationService.cs`

### IVoiceRecognitionService

The interface for speech-to-text services. Implementations include `VoskRecognitionService` for offline recognition and `AzureSpeechRecognitionService` for online recognition.

**File**: `WatchTower/Services/IVoiceRecognitionService.cs`

### ITextToSpeechService

The interface for text-to-speech services. Implementations include `PiperTextToSpeechService` for offline synthesis and `AzureSpeechSynthesisService` for online synthesis.

**File**: `WatchTower/Services/ITextToSpeechService.cs`

### VoiceMode

An enum defining voice operation modes: `Offline` (Vosk + Piper, no internet required), `Online` (Azure Speech Services), and `Hybrid` (planned but not yet implemented).

**File**: `WatchTower/Models/VoiceMode.cs`

### VoiceState

A model representing the current state of the voice system, including whether it is listening, speaking, and the current audio activity levels.

**File**: `WatchTower/Models/VoiceState.cs`

### Full-Duplex Mode

A voice operation mode where the system can listen and speak simultaneously. This enables natural conversational interactions without requiring turn-taking.

**See**: `docs/voice-setup-guide.md`

### Barge-in Control

A feature that determines how the system handles interruptions. When enabled, recognition pauses while the system is speaking to prevent echo and feedback.

**See**: `IMPLEMENTATION-VOICE.md`

## Visual Elements

### Adinkra Elements

Planned custom Adaptive Card elements representing Adinkra symbols from West African visual traditions. Examples include GyeNyame, Sankofa, and Dwennimmen. Planned implementation via an `AdinkraIcon` control.

**See**: `docs/ADAPTIVE-CARD-THEME-PLAN.md`, `concept-art/Adinkra Symbols/`

### XYFocus

Avalonia's directional focus navigation system. It enables D-Pad and analog stick navigation between UI elements.

**See**: `docs/ADAPTIVE-CARD-THEME-PLAN.md`

### Holographic Cyan

The primary accent color (#00F0FF) in the Ancestral Futurism theme. Used for focus indicators, UI accents, and borders. Often applied with glow effects and pulse animations for visual feedback.

**See**: `docs/ADAPTIVE-CARD-THEME-PLAN.md`

## Development Terms

### Rude Edits

C# code changes that `dotnet watch` cannot hot reload, triggering an automatic application restart. Examples include interface changes, abstract/virtual/override additions, and type deletions. Configured with `DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true`.

**See**: `.vscode/tasks.json`, `AGENTS.md`

### Hot Reload

A development feature that applies code changes without restarting the application. Supported edits include modifying method bodies, adding methods/fields/properties, and changing attributes. XAML hot reload is available in Debug mode.

**See**: `AGENTS.md`

### MCP Tools

Microsoft Code Platform semantic tools for .NET code analysis. These include `list_errors`, `find_all_references`, `find_symbols`, `get_symbol_definition`, and `code_refactoring`. Preferred over text-based search for code navigation.

**See**: `AGENTS.md`

## Configuration Sections

### SplashSizeRatio

A configuration concept (now replaced by frame-based sizing) that defined the initial splash window size as a percentage of the screen. The current implementation calculates size based on frame static components plus minimum content area.

**See**: `ANIMATION-FLOW.md`

### AnimationDurationMs

Configuration values for animation timing throughout the application. Startup expansion uses 500ms, replay animations use 1000ms each direction, and monitor switch uses 250ms each direction.

**See**: `ShellWindow.axaml.cs`

### MinContentWidth / MinContentHeight

Configuration values in `appsettings.json` that define the minimum content area inside the decorative frame. Default values are 400x300 pixels. The actual window size is calculated by adding frame static components and padding.

**Config**: `appsettings.json` -> `Startup:MinContentWidth`, `Startup:MinContentHeight`

## Testing Terms

### Avalonia.Headless

A testing framework for Avalonia UI applications that enables UI testing without browser dependencies or actual window rendering. It allows testing of UI interactions in a headless environment.

**See**: `AGENTS.md`

### ViewModelBase

An abstract base class providing `INotifyPropertyChanged` implementation and `SetProperty<T>()` helper for property change notifications. All ViewModels in the application inherit from this class.

**File**: `WatchTower/ViewModels/ViewModelBase.cs`

## File Formats

### appsettings.json

The main configuration file for the application. It contains sections for Logging, Gamepad, Startup, Voice, and Frame settings. Located in the `WatchTower/` directory.

**File**: `WatchTower/appsettings.json`

### .env

An environment file for storing sensitive configuration like API keys. It is listed in `.gitignore` and should never be committed. A `.env.example` template is provided for reference.

**File**: `.env` (not committed), `.env.example`

### main-frame.png

The decorative frame source image used for the application window border. It is a high-resolution PNG that gets sliced into 16 pieces using the 5x5 grid system for resolution-independent scaling.

**File**: `WatchTower/Assets/main-frame.png`
