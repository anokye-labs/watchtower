# WatchTower

Professional IDE-style docking interface built with Avalonia UI (.NET 10) following MVVM architecture.

## Features

✅ **Professional Docking System** - Fully functional IDE-style panel management
- 🎯 Default 3-panel layout (Explorer, Document Area, Terminal)
- 📏 Resizable panels with splitter controls
- 🔄 Drag-and-drop panel rearrangement
- 🪟 Floating windows for multi-monitor workflows
- 📑 Tabbed panel groups for space efficiency
- ✂️ Split views for side-by-side documents
- 💾 Auto-save layout persistence across sessions
- ⌨️ Menu bar with keyboard shortcuts (Ctrl+B, Ctrl+`, F11, etc.)

✅ **Game Controller Support** - Cross-platform gamepad input
- 🎮 Xbox, PlayStation, and generic USB controller support
- 🔘 Standard button mapping (A/B/X/Y, D-Pad, triggers, analog sticks)
- 📡 Event-based input handling (button press/release)
- 🔌 Hot-plug detection (connect/disconnect events)
- 🎯 Foundation ready for UI navigation and custom bindings

Built on [Dock.Avalonia](https://github.com/wieslawsoltes/Dock) for native docking capabilities.

## Quick Start

```bash
# 1. Install .NET 10 SDK from https://dotnet.microsoft.com/download
# 2. Clone and open
git clone <repository-url>
cd watchtower
code .

# 3. Press F5 in VS Code to run
```

That's it. VS Code will prompt for extensions. Install them.

## Development

**Hot Reload**: Edit `.axaml` files → save → changes appear instantly (F5 mode only)

**Debug**: Press F5 → Set breakpoints → Step through code

**Build**: `Ctrl+Shift+B` or F5 handles it

## Project Structure

```
WatchTower/          # Main app
├── Views/           # XAML UI
├── ViewModels/      # UI logic (MVVM)
├── Services/        # Business logic
└── Models/          # Data

.vscode/             # Already configured
specs/               # Feature docs
```

## Key Files

- `Program.cs` - Entry point
- `App.axaml[.cs]` - App initialization
- `Views/MainWindow.axaml` - Main UI
- `appsettings.json` - Logging config (`"minimal"` | `"normal"` | `"verbose"`)

## Architecture Rules

**MVVM Pattern** (non-negotiable):
- Views = XAML only, zero logic
- ViewModels = all logic, testable
- Services = business logic, DI injected

See [`.github/copilot-instructions.md`](.github/copilot-instructions.md) for details.

## Publishing

```bash
# Windows
dotnet publish -c Release -r win-x64 --self-contained

# macOS
dotnet publish -c Release -r osx-x64 --self-contained

# Linux
dotnet publish -c Release -r linux-x64 --self-contained
```

Output: `WatchTower/bin/Release/net10.0/{rid}/publish/`

## Tech Stack

- Avalonia 11.3.9 + .NET 10
- MVVM + Dependency Injection
- Cross-platform (Windows/macOS/Linux)

## Resources

- [Avalonia Docs](https://docs.avaloniaui.net/)
- [Project Instructions](.github/copilot-instructions.md)
- [Game Controller Support](docs/game-controller-support.md)
- [Specs](specs/)
