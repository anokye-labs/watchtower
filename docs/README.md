# WatchTower Documentation

This directory contains technical documentation for the WatchTower application. The documentation is organized into several categories to help you find the information you need quickly.

## Getting Started

If you're new to WatchTower, start with these documents in order:

1. **[Main README](../README.md)** - Project overview, quick start, and installation
2. **[Architecture](ARCHITECTURE.md)** - Understand the system design and component interactions
3. **[Glossary](GLOSSARY.md)** - Learn codebase-specific terms and definitions
4. **[Development Guidelines](../AGENTS.md)** - Coding standards and workflow

## Documentation Index

### Core Documentation

| Document | Description | Audience |
|----------|-------------|----------|
| [ARCHITECTURE.md](ARCHITECTURE.md) | Detailed system architecture, MVVM patterns, data flow | All developers |
| [GLOSSARY.md](GLOSSARY.md) | Codebase-specific terms and definitions | All developers |
| [../CONTRIBUTING.md](../CONTRIBUTING.md) | Contribution guidelines and PR process | Contributors |
| [../AGENTS.md](../AGENTS.md) | AI agent and developer guidelines | AI agents, developers |

### Feature Documentation

| Document | Description | Related Code |
|----------|-------------|--------------|
| [game-controller-support.md](game-controller-support.md) | SDL2-based gamepad integration guide | `Services/GameControllerService.cs` |
| [voice-setup-guide.md](voice-setup-guide.md) | Voice feature configuration and setup | `Services/VoiceOrchestrationService.cs` |
| [ADAPTIVE-CARD-THEME-PLAN.md](ADAPTIVE-CARD-THEME-PLAN.md) | Theming system implementation plan | `Services/AdaptiveCardThemeService.cs` |
| [ADAPTIVECARD_DISPLAY_ENGINE.md](ADAPTIVECARD_DISPLAY_ENGINE.md) | Adaptive Card rendering system overview | `Services/AdaptiveCardService.cs` |
| [fluent-icons-usage.md](fluent-icons-usage.md) | Fluent UI icons integration guide | `Views/*.axaml` |

### Startup and Animation

| Document | Description | Related Code |
|----------|-------------|--------------|
| [splash-screen-startup.md](splash-screen-startup.md) | Startup flow and splash screen architecture | `Services/StartupOrchestrator.cs` |
| [splash-screen-visual-guide.md](splash-screen-visual-guide.md) | Visual states and UI design for splash screen | `Views/SplashWindow.axaml` |
| [../ANIMATION-FLOW.md](../ANIMATION-FLOW.md) | Startup animation sequence diagrams | `Views/ShellWindow.axaml.cs` |
| [../VISUAL-MOCKUP.md](../VISUAL-MOCKUP.md) | Animation sequence visual mockups | `Views/ShellWindow.axaml.cs` |

### Implementation Summaries

| Document | Description | Status |
|----------|-------------|--------|
| [../IMPLEMENTATION-SUMMARY.md](../IMPLEMENTATION-SUMMARY.md) | Game controller implementation summary | Complete |
| [../IMPLEMENTATION-SUMMARY-SPLASH.md](../IMPLEMENTATION-SUMMARY-SPLASH.md) | Splash screen implementation summary | Complete |
| [../IMPLEMENTATION-ANIMATED-SHELL.md](../IMPLEMENTATION-ANIMATED-SHELL.md) | Shell window animation implementation | Complete |
| [../IMPLEMENTATION-VOICE.md](../IMPLEMENTATION-VOICE.md) | Voice system implementation summary | Complete |
| [../QUICK-START-ANIMATION.md](../QUICK-START-ANIMATION.md) | Animation feature quick start | Complete |

## Architecture Overview

WatchTower follows strict MVVM (Model-View-ViewModel) architecture with dependency injection. The architecture is organized into three layers:

```
View Layer (UI)              ViewModel Layer (Logic)       Service Layer (Business Logic)
--------------------         ----------------------        -----------------------------
MainWindow.axaml         ->  MainWindowViewModel       ->  IGameControllerService
ShellWindow.axaml        ->  ShellWindowViewModel      ->  IAdaptiveCardService
SplashWindow.axaml       ->  SplashWindowViewModel     ->  IVoiceOrchestrationService
                                                          StartupOrchestrator
                                                          FrameSliceService
```

For detailed architecture documentation, see [ARCHITECTURE.md](ARCHITECTURE.md).

## Key Concepts

### Ancestral Futurism Design Language

WatchTower uses a distinctive visual style combining modern UI with West African cultural elements. The color palette includes:

- **Holographic Cyan** (#00F0FF) - Interactive elements, focus indicators
- **Ashanti Gold** (#FFD700) - Important elements, accents
- **Mahogany** (#4A1812) - Borders, containers
- **Void Black** (#050508) - Backgrounds

See [concept-art/](../concept-art/) for design inspiration and Adinkra symbol references.

### Shell Window System

The application uses a unified ShellWindow that hosts both splash and main content, enabling smooth animated transitions from startup to the main interface. Key features include:

- **5x5 Grid Slicing** - Resolution-independent frame scaling
- **Animated Expansion** - 500ms cubic ease-out from splash to fullscreen
- **Content Switching** - Seamless transition between splash and main ViewModels

### Gamepad-First Navigation

Full SDL2 integration provides cross-platform game controller support:

- **XYFocus Navigation** - D-Pad and analog stick control UI focus
- **Button Mapping** - Standard Xbox/PlayStation button layouts
- **Hot-Plug Support** - Automatic controller detection
- **60 FPS Polling** - Synchronized with UI rendering

### Voice System

Full-duplex voice capabilities with offline and online modes:

- **Offline Mode** - Vosk (ASR) + Piper (TTS), no internet required
- **Online Mode** - Azure Cognitive Services for higher accuracy
- **Full-Duplex** - Simultaneous listening and speaking

## Configuration Reference

The application is configured through `WatchTower/appsettings.json`. Key sections include:

| Section | Purpose | Documentation |
|---------|---------|---------------|
| `Logging` | Log level configuration | [ARCHITECTURE.md](ARCHITECTURE.md) |
| `Gamepad` | Dead zone and controller settings | [game-controller-support.md](game-controller-support.md) |
| `Startup` | Hang threshold and window sizing | [splash-screen-startup.md](splash-screen-startup.md) |
| `Voice` | Voice mode and model paths | [voice-setup-guide.md](voice-setup-guide.md) |
| `Frame` | Decorative frame configuration | [splash-screen-visual-guide.md](splash-screen-visual-guide.md) |

## Quick Links

### For New Developers
- [Quick Start](../README.md#quick-start) - Get the application running
- [Architecture](ARCHITECTURE.md) - Understand the codebase
- [Glossary](GLOSSARY.md) - Learn the terminology

### For Contributors
- [Contributing Guidelines](../CONTRIBUTING.md) - How to contribute
- [Development Guidelines](../AGENTS.md) - Coding standards
- [Testing](../AGENTS.md#testing) - Testing requirements

### For Feature Development
- [Game Controller](game-controller-support.md) - Gamepad integration
- [Voice Features](voice-setup-guide.md) - Voice system setup
- [Adaptive Cards](ADAPTIVE-CARD-THEME-PLAN.md) - Card theming

## Related Resources

- [Avalonia UI Documentation](https://docs.avaloniaui.net/)
- [Adaptive Cards Official Site](https://adaptivecards.io/)
- [SDL2 GameController API](https://wiki.libsdl.org/SDL_GameController)
- [Vosk Speech Recognition](https://alphacephei.com/vosk/)
- [Piper TTS](https://github.com/rhasspy/piper)
