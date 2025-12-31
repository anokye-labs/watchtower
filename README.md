# WatchTower

A cross-platform desktop application built with Avalonia UI on .NET 10, designed to provide an immersive, gamepad-first user interface experience. WatchTower showcases a distinctive "Ancestral Futurism" design language that combines modern UI patterns with cultural elements from West African visual traditions, including Adinkra symbols.

## Overview

WatchTower serves as a platform for displaying dynamic, full-screen Adaptive Card interfaces with integrated gamepad navigation. The application is designed for end users who want to interact with rich, adaptive content through a gamepad-optimized interface, content creators designing Adaptive Card experiences, and developers building cross-platform desktop applications with Avalonia UI.

### Key Capabilities

The application provides Adaptive Card rendering with a custom "Ancestral Futurism" theme featuring holographic cyan, Ashanti gold, mahogany, and void black color schemes. It offers gamepad-first navigation through full SDL2 integration for game controller input with XYFocus navigation and visual feedback. Cross-platform deployment produces single-file, self-contained executables for Windows (win-x64), macOS (osx-x64), and Linux (linux-x64). Dynamic UI overlays include animated input panels for rich text and voice input, plus an event log with smooth transitions. The adaptive frame system provides resolution-independent decorative borders using 5x5 grid image slicing.

## Features

- **Cross-platform**: Windows, macOS, Linux support
- **MVVM Architecture**: Strict separation of concerns
- **Game Controller Support**: Navigate with gamepad
- **AI Agent Integration**: MCP (Model Context Protocol) support for AI-assisted development
- **Adaptive Cards**: Rich UI component rendering

## MCP Proxy Platform

WatchTower includes the **Avalonia MCP Proxy Platform** - a reusable, open-source solution that enables AI agents (Claude, GitHub Copilot, etc.) to interact with Avalonia applications.

**What it does:**
- Allows agents to inspect UI state, click elements, type text, capture screenshots
- Provides a unified interface for agents to work with multiple Avalonia apps
- Enables iterative development: agent sees UI → suggests changes → developer implements → agent verifies

**Components:**
- `Avalonia.Mcp.Core` - Library for embedding MCP handlers in Avalonia apps
- `Avalonia.McpProxy` - Standalone proxy server that federates app handlers
- WatchTower - First app using the platform

See [docs/mcp-proxy-architecture.md](docs/mcp-proxy-architecture.md) for detailed architecture.

## Quick Start

### Prerequisites

Install the .NET 10 SDK from https://dotnet.microsoft.com/download before proceeding.

### Installation

```bash
# Clone the repository
git clone https://github.com/anokye-labs/watchtower.git
cd watchtower

# Open in VS Code
code .
```

VS Code will prompt for recommended extensions. Install them for the best development experience.

### Running the Application

**Option 1: VS Code (Recommended)**

Press F5 to build and run with debugging. The application will display a splash screen during initialization, then animate to the main interface.

**Option 2: Command Line**

```bash
# Build the project
dotnet build

# Run the application
dotnet run --project WatchTower/WatchTower.csproj
```

**Option 3: Hot Reload Development**

```bash
dotnet watch run --project WatchTower/WatchTower.csproj --non-interactive
```

This mode automatically restarts on code changes and hot reloads supported edits.

## Architecture

WatchTower follows a strict MVVM (Model-View-ViewModel) architecture with dependency injection. The architecture is organized into three layers.

```
View Layer (UI)              ViewModel Layer (Logic)       Service Layer (Business Logic)
--------------------         ----------------------        -----------------------------
MainWindow.axaml         ->  MainWindowViewModel       ->  IGameControllerService
ShellWindow.axaml        ->  ShellWindowViewModel      ->  IAdaptiveCardService
SplashWindow.axaml       ->  SplashWindowViewModel     ->  IVoiceOrchestrationService
                                                          StartupOrchestrator
```

### Design Principles

**MVVM Compliance**: Views contain only bindings and initialization. All logic resides in ViewModels and Services.

**Dependency Injection**: Services are registered in `App.axaml.cs` and injected via constructors.

**Cross-Platform**: No platform-specific code. Works identically on Windows, macOS, and Linux.

**Testability**: ViewModels are testable without UI dependencies through mocked service interfaces.

For detailed architecture documentation, see [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Project Structure

```
WatchTower/                   # Main application
├── Program.cs                # Application entry point
├── App.axaml.cs              # Framework initialization, DI setup
├── appsettings.json          # Configuration (logging, gamepad, voice, frame)
├── Views/                    # XAML UI definitions
│   ├── MainWindow.axaml      # Primary application UI with overlays
│   ├── ShellWindow.axaml     # Adaptive container with frame grid
│   └── SplashWindow.axaml    # Startup loading screen
├── ViewModels/               # Presentation logic (no UI dependencies)
│   ├── MainWindowViewModel.cs
│   ├── ShellWindowViewModel.cs
│   └── SplashWindowViewModel.cs
├── Services/                 # Business logic, testable components
│   ├── GameControllerService.cs
│   ├── AdaptiveCardService.cs
│   └── StartupOrchestrator.cs
├── Models/                   # Data structures
└── Assets/                   # Embedded resources

docs/                         # Technical documentation
├── ARCHITECTURE.md           # Detailed system architecture
├── GLOSSARY.md               # Codebase-specific terms
├── game-controller-support.md
├── voice-setup-guide.md
└── README.md                 # Documentation index

concept-art/                  # Visual design assets
└── Adinkra Symbols/          # Cultural icon assets
```

## Features

### Game Controller Support

Full hardware support for game controller input using SDL2 via Silk.NET. Features include real cross-platform gamepad detection, SDL Game Controller Database for automatic button mapping, hot-plug support for controller connect/disconnect, and 60 FPS polling synchronized with UI rendering.

See [Game Controller Support](docs/game-controller-support.md) for detailed documentation.

### Voice Capabilities

Full-duplex voice capabilities with both offline and online modes. Offline mode (default) uses Vosk for speech recognition and Piper for TTS with no internet required. Online mode uses Azure Speech Services for higher accuracy. Full-duplex mode enables listening and speaking simultaneously.

Note: Voice features currently require Windows due to NAudio dependency. Linux and macOS support will require an alternative audio backend.

See [Voice Setup Guide](docs/voice-setup-guide.md) for configuration and model downloads.

### Adaptive Cards

Display interactive card content with the Ancestral Futurism theme. The theming system applies custom colors, fonts, and spacing to create an immersive visual experience.

See [Adaptive Card Theme Plan](docs/ADAPTIVE-CARD-THEME-PLAN.md) for theming details.

### Animated Shell Window

A unified shell window that hosts both splash and main content with smooth animated transitions. The decorative frame uses 5x5 grid slicing for resolution-independent scaling.

## Configuration

The application is configured through `WatchTower/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": "normal"        // minimal | normal | verbose
  },
  "Gamepad": {
    "DeadZone": 0.15            // 0.0 - 1.0, radial dead zone
  },
  "Startup": {
    "HangThresholdSeconds": 30,
    "MinContentWidth": 400,
    "MinContentHeight": 300
  },
  "Voice": {
    "Mode": "offline",          // offline | online
    "Vosk": { "ModelPath": "models/vosk-model-small-en-us-0.15" },
    "Piper": { "ModelPath": "models/piper", "Voice": "en_US-lessac-medium" }
  },
  "Frame": {
    "SourceUri": "avares://WatchTower/Assets/main-frame.png",
    "Scale": 0.20,
    "BackgroundColor": "#261208"
  }
}
```

## Development

### Building

```bash
dotnet build                    # Compile the project
```

### Running with Hot Reload

```bash
dotnet watch run --project WatchTower/WatchTower.csproj --non-interactive
```

Hot reload applies most C# edits automatically. Rude edits (interface changes, type deletions, etc.) trigger automatic restarts.

### Debugging

Press F5 in VS Code to attach the debugger. Set breakpoints in any C# file. XAML hot reload is available in Debug mode.

### Publishing

```bash
# Windows
dotnet publish -c Release -r win-x64 --self-contained true

# macOS
dotnet publish -c Release -r osx-x64 --self-contained true

# Linux
dotnet publish -c Release -r linux-x64 --self-contained true
```

Output: `WatchTower/bin/Release/net10.0/{rid}/publish/`

## Keyboard Shortcuts

### Splash Mode
- `D` - Toggle diagnostics panel
- `ESC` or `X` - Exit application

### Main Mode
- `Ctrl+R` - Toggle rich text input overlay
- `Ctrl+M` - Toggle voice input overlay
- `Ctrl+L` - Toggle event log overlay
- `ESC` - Close any open overlay

## Tech Stack

- **Framework**: Avalonia 11.3.9 + .NET 10
- **Architecture**: MVVM + Dependency Injection
- **Game Controllers**: SDL2 via Silk.NET
- **Voice (Offline)**: Vosk + Piper
- **Voice (Online)**: Azure Cognitive Services
- **Adaptive Cards**: Iciclecreek.AdaptiveCards.Rendering.Avalonia

## Documentation

| Document | Description |
|----------|-------------|
| [Architecture](docs/ARCHITECTURE.md) | Detailed system architecture and design |
| [Glossary](docs/GLOSSARY.md) | Codebase-specific terms and definitions |
| [MCP Proxy Architecture](docs/mcp-proxy-architecture.md) | AI agent integration via MCP protocol |
| [Game Controller Support](docs/game-controller-support.md) | SDL2 gamepad integration guide |
| [Voice Setup Guide](docs/voice-setup-guide.md) | Voice feature configuration |
| [Splash Screen Startup](docs/splash-screen-startup.md) | Startup flow and splash screen |
| [Adaptive Card Theme Plan](docs/ADAPTIVE-CARD-THEME-PLAN.md) | Theming system architecture |
| [Development Guidelines](AGENTS.md) | AI agent and developer guidelines |
| [Contributing](CONTRIBUTING.md) | Contribution guidelines |
| [Documentation Index](docs/README.md) | Complete documentation index |

## License

This project is open source under the MIT License. See [LICENSE](LICENSE) for details.

## Automation Test
Validates fixed field names in workflow.
