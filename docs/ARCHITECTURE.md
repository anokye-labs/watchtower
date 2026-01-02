# WatchTower Architecture

This document provides a comprehensive overview of the WatchTower application architecture, including its core systems, design patterns, and component interactions.

## Overview

WatchTower is a Windows-native desktop application built with Avalonia UI on .NET 10, designed to provide an immersive, gamepad-first user interface experience. The application showcases a distinctive "Ancestral Futurism" design language that combines modern UI patterns with cultural elements from West African visual traditions, specifically Adinkra symbols.

## Design Philosophy

### Ancestral Futurism

The application's visual identity is built around the "Ancestral Futurism" design language, which combines modern UI aesthetics with West African cultural elements. The color palette consists of holographic cyan (#00F0FF) for interactive elements and focus indicators, Ashanti gold (#FFD700) for important elements and accents, mahogany (#4A1812) for borders and containers, and void black (#050508) for backgrounds.

### Core Principles

The architecture is guided by five core principles. First, MVVM Architecture First ensures that all logic resides in ViewModels and Services, with Views containing only bindings and presentation code. Second, Open Source Only means the project uses only MIT or Apache 2.0 licensed dependencies. Third, Windows-Native enables full utilization of Windows APIs and ecosystem for optimal performance and feature support. Fourth, Testability Required means ViewModels are testable without UI dependencies using Avalonia.Headless. Fifth, Design System Compliance ensures consistent styling through Avalonia CSS variables with dark as the primary theme.

## MVVM Architecture

WatchTower follows a strict Model-View-ViewModel (MVVM) architecture with dependency injection. The architecture is organized into three distinct layers.

### Layer Overview

```
View Layer (UI)              ViewModel Layer (Logic)       Service Layer (Business Logic)
--------------------         ----------------------        -----------------------------
MainWindow.axaml         ->  MainWindowViewModel       ->  IGameControllerService
ShellWindow.axaml        ->  ShellWindowViewModel      ->  IAdaptiveCardService
SplashWindow.axaml       ->  SplashWindowViewModel     ->  IAdaptiveCardThemeService
                                                          IVoiceOrchestrationService
                                                          StartupOrchestrator
                                                          FrameSliceService
```

### View Layer

Views are defined in XAML files located in `WatchTower/Views/`. They contain only bindings and initialization code, with no business logic. Code-behind files are limited to event forwarding and initialization that cannot be expressed in XAML.

### ViewModel Layer

ViewModels contain all presentation logic and are located in `WatchTower/ViewModels/`. They implement `INotifyPropertyChanged` for data binding, expose commands for user actions, and orchestrate services to fulfill user requests. ViewModels have no dependencies on UI types, making them fully testable.

### Service Layer

Services encapsulate business logic and are located in `WatchTower/Services/`. They are registered in the dependency injection container and injected into ViewModels via constructor injection. Services are defined by interfaces to enable testing with mocks.

## Directory Structure

```
WatchTower/
├── Program.cs                    # Application entry point
├── App.axaml.cs                  # Framework initialization, DI setup
├── appsettings.json              # Configuration (logging, gamepad, voice, frame)
├── WatchTower.csproj             # Project definition, dependencies
│
├── Views/                        # XAML UI definitions (Views only)
│   ├── MainWindow.axaml          # Primary application UI with overlays
│   ├── ShellWindow.axaml         # Adaptive container with frame grid
│   └── SplashWindow.axaml        # Startup loading screen
│
├── ViewModels/                   # Presentation logic (no UI dependencies)
│   ├── MainWindowViewModel.cs    # Overlay state, command routing
│   ├── ShellWindowViewModel.cs   # Content switching, frame management
│   ├── SplashWindowViewModel.cs  # Startup progress, diagnostics
│   └── VoiceControlViewModel.cs  # Voice feature controls
│
├── Services/                     # Business logic, testable components
│   ├── GameControllerService.cs  # SDL2 polling, button events
│   ├── AdaptiveCardService.cs    # Card loading, action handling
│   ├── StartupOrchestrator.cs    # Multi-phase initialization
│   ├── FrameSliceService.cs      # 5x5 image grid extraction
│   ├── VoiceOrchestrationService.cs  # Full-duplex voice coordination
│   └── I*.cs                     # Service interfaces
│
├── Models/                       # Data structures
│   ├── GameControllerState.cs    # Controller button/analog state
│   ├── GameControllerButton.cs   # Button enumeration
│   ├── VoiceMode.cs              # Voice mode enumeration
│   └── VoiceState.cs             # Voice system state
│
├── Converters/                   # Data binding converters
│   ├── BoolToTextConverter.cs    # Boolean to text for UI
│   └── DivideConverter.cs        # Numeric division for scaling
│
└── Assets/                       # Embedded resources
    └── main-frame.png            # Decorative frame source image

docs/                             # Technical documentation
├── ARCHITECTURE.md               # This file
├── GLOSSARY.md                   # Codebase-specific terms
├── game-controller-support.md    # Gamepad integration guide
├── voice-setup-guide.md          # Voice feature configuration
├── splash-screen-startup.md      # Startup sequence details
└── ADAPTIVE-CARD-THEME-PLAN.md   # Theming system architecture

concept-art/                      # Visual design assets
└── Adinkra Symbols/              # Cultural icon assets
```

## Core Systems

### 1. Startup System

The startup system manages application initialization from launch to the main interface.

**Flow**: `Program.cs` -> `App.OnFrameworkInitializationCompleted()` -> `ShellWindow` (splash) -> `AnimateExpansionAsync()` -> `TransitionToMainContent()`

**Key Components**:

The `Program.cs` file serves as the application entry point, configuring the Avalonia builder and handling .NET 10 runtime checks. The `App.axaml.cs` file handles DI setup and service orchestration, creating the initial window and starting the async startup workflow. The `StartupOrchestrator` service executes a four-phase initialization process: loading configuration, setting up the DI container, registering services, and initializing services.

**Startup Phases**:

Phase 1 loads configuration from `appsettings.json`. Phase 2 configures the dependency injection container. Phase 3 registers application services including logging, adaptive cards, game controller, and voice services. Phase 4 initializes services that require explicit initialization such as the game controller.

### 2. Shell Window System

The Shell Window system provides a unified container that hosts both splash and main content, enabling smooth animated transitions.

**Key Responsibilities**:

Frame rendering loads `main-frame.png` and slices it into 16 border pieces using 8 coordinates from the configuration. Content switching toggles between `SplashWindowViewModel` and `MainWindowViewModel`. Animations handle expansion (500ms), monitor switching (250ms), and replay (1000ms). Resolution adaptation uses an LRU-5 cache for different display scales.

**5x5 Grid Slicing**:

The decorative frame uses a 5x5 grid slicing system for resolution-independent scaling. The slice coordinates define 8 boundary points (Left, LeftInner, RightInner, Right, Top, TopInner, BottomInner, Bottom) that divide the source image into 16 border pieces. This ensures corners maintain their detail while edges stretch smoothly during window animations.

### 3. Main UI System

The Main UI system displays Adaptive Cards and manages input overlays.

**Features**:

Adaptive Card Display renders full-screen cards with the themed Ancestral Futurism styling. Overlays include an input overlay that slides from the bottom (300ms) for rich text or voice input, and an event log that slides from the left (300ms) for controller events. Keyboard shortcuts include Escape to close overlays, Ctrl+R for rich text input, Ctrl+M for voice input, and Ctrl+L to toggle the event log. Gamepad integration maps button events to commands.

### 4. Game Controller System

The game controller system provides full SDL2 integration for game controller support.

**Architecture**:

```
Application Layer (ViewModels subscribe to events)
                    |
                    v
IGameControllerService (Interface with events)
                    |
                    v
GameControllerService (SDL2-based implementation)
                    |
                    v
Silk.NET.SDL v2.22.0 (SDL2 bindings via Silk.NET)
```

**Features**:

The system provides gamepad detection via Silk.NET.SDL, uses the SDL Game Controller Database for automatic button mapping, supports hot-plug for controller connect/disconnect events, and polls at 60 FPS synchronized with UI rendering. Radial dead zone processing is configurable with a default of 15%.

### 5. Voice System

The voice system provides full-duplex voice capabilities with offline and online modes.

**Architecture**:

```
IVoiceOrchestrationService (Coordinator)
    ├── IVoiceRecognitionService (ASR)
    │   ├── VoskRecognitionService (Offline)
    │   └── AzureSpeechRecognitionService (Online)
    └── ITextToSpeechService (TTS)
        ├── PiperTextToSpeechService (Offline)
        └── AzureSpeechSynthesisService (Online)
```

**Modes**:

Offline mode uses Vosk for speech recognition and Piper for text-to-speech with no internet required. Online mode uses Azure Cognitive Services for both recognition and synthesis. Hybrid mode is planned but not yet implemented.

### 6. Adaptive Card System

The Adaptive Card system renders Microsoft Adaptive Cards with custom theming.

**Components**:

`IAdaptiveCardService` loads cards from JSON and handles actions including Submit, OpenUrl, Execute, and ShowCard. `IAdaptiveCardThemeService` generates `AdaptiveHostConfig` with Ancestral Futurism colors, fonts, and spacing. Focus navigation with gamepad input is handled directly by Avalonia's built-in XYFocus system.

## Dependency Injection

Services are registered in `App.axaml.cs` during startup. The registration follows these patterns:

**Singleton Services** are created once and shared across the application. These include `IGameControllerService`, `IAdaptiveCardService`, `IAdaptiveCardThemeService`, `IVoiceOrchestrationService`, and `IVoiceRecognitionService`.

**Transient Services** are created fresh for each request. These include ViewModels like `MainWindowViewModel` and `VoiceControlViewModel`.

**Configuration** is loaded from `appsettings.json` and injected via `IConfiguration`.

## Configuration

The application is configured through `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": "normal"  // minimal | normal | verbose
  },
  "Gamepad": {
    "DeadZone": 0.15      // 0.0 - 1.0, radial dead zone
  },
  "Startup": {
    "HangThresholdSeconds": 30,
    "MinContentWidth": 400,
    "MinContentHeight": 300
  },
  "Voice": {
    "Mode": "offline",    // offline | online | hybrid
    "Vosk": { "ModelPath": "models/vosk-model-small-en-us-0.15" },
    "Piper": { "ModelPath": "models/piper", "Voice": "en_US-lessac-medium" },
    "Azure": { "SpeechKey": "", "SpeechRegion": "" }
  },
  "Frame": {
    "SourceUri": "avares://WatchTower/Assets/main-frame.png",
    "Scale": 0.20,
    "BackgroundColor": "#261208",
    "Padding": { "Left": 80, "Top": 60, "Right": 80, "Bottom": 60 },
    "Slice": { /* 8 boundary coordinates */ }
  }
}
```

## Data Flow

### Startup Data Flow

```
App.OnFrameworkInitializationCompleted()
    |
    v
Create ShellWindow with SplashWindowViewModel
    |
    v
Task.Run(ExecuteStartupAsync)
    |
    v
StartupOrchestrator.ExecuteStartupAsync()
    |-- Phase 1: Load configuration
    |-- Phase 2: Configure DI
    |-- Phase 3: Register services
    |-- Phase 4: Initialize services
    |
    v
Mark startup complete
    |
    v
AnimateExpansionAsync() (500ms)
    |
    v
TransitionToMainContent(MainWindowViewModel)
```

### Game Controller Data Flow

```
SDL2 Hardware
    |
    v
GameControllerService.Update() (60 FPS)
    |
    v
Button state change detected
    |
    v
ButtonPressed/ButtonReleased event
    |
    v
ViewModel event handler
    |
    v
Update ViewModel properties
    |
    v
UI updates via data binding
```

### Voice Data Flow

```
Microphone Input
    |
    v
VoiceRecognitionService (Vosk/Azure)
    |
    v
VoiceOrchestrationService
    |
    v
SpeechRecognized event
    |
    v
ViewModel processes text
    |
    v
VoiceOrchestrationService.SpeakAsync()
    |
    v
TextToSpeechService (Piper/Azure)
    |
    v
Audio Output
```

## Deployment

WatchTower is designed for Windows deployment (win-x64) with self-contained single-file distribution.

Voice features use NAudio for Windows-native audio capture/playback, providing reliable integration with the Windows audio stack.

## Testing Strategy

**ViewModel Testing**:

ViewModels are designed to be testable without UI dependencies. Services are injected via interfaces, allowing mock implementations for testing. The recommended frameworks are xUnit or NUnit with a target of 80% coverage for ViewModels and Services.

**UI Testing**:

Avalonia.Headless enables UI testing without browser dependencies or actual window rendering. This allows testing of UI interactions in a headless environment.

## Related Documentation

For more detailed information on specific systems, see the following documents:

- [Game Controller Support](game-controller-support.md) - SDL2 gamepad integration details
- [Voice Setup Guide](voice-setup-guide.md) - Voice feature configuration
- [Splash Screen Startup](splash-screen-startup.md) - Startup flow details
- [Adaptive Card Theme Plan](ADAPTIVE-CARD-THEME-PLAN.md) - Theming system architecture
- [Glossary](GLOSSARY.md) - Codebase-specific terms and definitions
