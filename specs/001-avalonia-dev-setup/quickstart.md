# WatchTower Quick Start

**Last Updated**: November 24, 2025

## 30-Second Setup

```bash
# Install .NET 10 SDK from https://dotnet.microsoft.com/download
# Then:
git clone <repository-url>
cd watchtower
code .
# Press F5 in VS Code
```

Done. VS Code prompts for extensions → install them.

## Development

**Press F5** in VS Code. That's the workflow.

- **Hot Reload**: Edit `.axaml` files → save → changes appear (< 2s)
- **Debug**: Click left margin to set breakpoints
- **Build**: `Ctrl+Shift+B` (but F5 handles this)

**Hot Reload Rules**:
- ✅ XAML changes, method body edits
- ❌ Adding/removing methods → restart needed

**DevTools**: Press F12 in running app (Debug mode)

## Publishing

```bash
# Windows
dotnet publish -c Release -r win-x64 --self-contained

# macOS Intel
dotnet publish -c Release -r osx-x64 --self-contained

# macOS Apple Silicon
dotnet publish -c Release -r osx-arm64 --self-contained

# Linux
dotnet publish -c Release -r linux-x64 --self-contained
```

Output: `WatchTower/bin/Release/net10.0/{rid}/publish/`

## Config

Edit `WatchTower/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": "normal"  // "minimal" | "normal" | "verbose"
  }
}
```

## Troubleshooting

**App won't start**: `dotnet --version` should show 10.x

**Breakpoints don't work**: Reload VS Code window (`Ctrl+Shift+P` → "Reload Window")

**Hot reload not working**: Must be in Debug mode (F5), not Release

**Build fails**: `dotnet clean && dotnet build`

**Linux missing libs**: `sudo apt-get install libx11-dev libice-dev libsm-dev`

## Resources

- [Avalonia Docs](https://docs.avaloniaui.net/)
- [.NET Docs](https://docs.microsoft.com/dotnet/)
- [Project Instructions](../../.github/copilot-instructions.md)
