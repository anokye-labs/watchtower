# WatchTower Documentation

This directory contains technical documentation for the WatchTower application.

## Quick Reference

| Document | Description |
|----------|-------------|
| [label-system.md](label-system.md) | GitHub label system with Akan names for agent workflow |
| [game-controller-support.md](game-controller-support.md) | SDL2-based gamepad integration guide |
| [splash-screen-startup.md](splash-screen-startup.md) | Startup flow and splash screen architecture |
| [splash-screen-visual-guide.md](splash-screen-visual-guide.md) | Visual states and UI design for splash screen |
| [fluent-icons-usage.md](fluent-icons-usage.md) | Fluent UI icons integration guide |
| [ADAPTIVECARD_DISPLAY_ENGINE.md](ADAPTIVECARD_DISPLAY_ENGINE.md) | Adaptive Card rendering system overview |
| [ADAPTIVE-CARD-THEME-PLAN.md](ADAPTIVE-CARD-THEME-PLAN.md) | Theming system implementation plan |

## Root-Level Documentation

Additional documentation is available in the repository root:

| Document | Description |
|----------|-------------|
| [README.md](../README.md) | Project overview and quick start |
| [AGENTS.md](../AGENTS.md) | AI agent development guidelines |
| [ANIMATION-FLOW.md](../ANIMATION-FLOW.md) | Startup animation sequence diagrams |
| [IMPLEMENTATION-ANIMATED-SHELL.md](../IMPLEMENTATION-ANIMATED-SHELL.md) | Shell window animation implementation |
| [IMPLEMENTATION-SUMMARY.md](../IMPLEMENTATION-SUMMARY.md) | Game controller implementation summary |
| [IMPLEMENTATION-SUMMARY-SPLASH.md](../IMPLEMENTATION-SUMMARY-SPLASH.md) | Splash screen implementation summary |
| [QUICK-START-ANIMATION.md](../QUICK-START-ANIMATION.md) | Animation feature quick start |
| [VISUAL-MOCKUP.md](../VISUAL-MOCKUP.md) | Animation sequence visual mockups |

## Architecture Overview

WatchTower follows strict MVVM architecture with dependency injection:

```
View Layer (UI)          -> ViewModels (Logic)        -> Services (Business Logic)
ShellWindow.axaml           ShellWindowViewModel         IGameControllerService
MainWindow.axaml            MainWindowViewModel          IAdaptiveCardService
SplashWindow.axaml          SplashWindowViewModel        StartupOrchestrator
```

## Key Concepts

### Ancestral Futurism Design Language
WatchTower uses a distinctive visual style combining modern UI with West African cultural elements. The color palette includes holographic cyan (#00F0FF), Ashanti gold (#FFD700), mahogany (#4A1812), and void black (#050508). See [concept-art/](../concept-art/) for design inspiration.

### Shell Window System
The application uses a unified ShellWindow that hosts both splash and main content, enabling smooth animated transitions from startup to the main interface. The decorative frame uses a 5x5 grid slicing system for resolution-independent scaling.

### Gamepad-First Navigation
Full SDL2 integration provides cross-platform game controller support with XYFocus navigation and visual feedback for D-Pad/analog stick control.

## Getting Started

For development setup, see the [main README](../README.md). For AI agent development guidelines, see [AGENTS.md](../AGENTS.md).
