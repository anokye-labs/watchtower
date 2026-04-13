# WatchTower

Cross-platform desktop application built with Avalonia UI on .NET 10, featuring a gamepad-first "Ancestral Futurism" design language for rendering Adaptive Card interfaces.

## Tech Stack
- .NET 10 / C#
- Avalonia UI 11.3
- MVVM architecture (CommunityToolkit.Mvvm)
- SDL2 for gamepad input
- MCP (Model Context Protocol) integration via Avalonia.Mcp.Core
- xUnit for testing

## Build & Test
```bash
dotnet build WatchTower.slnx
dotnet test WatchTower.slnx
```

## Project Structure
- `WatchTower/` — Main application (ViewModels, Views, Services, Models, Converters)
- `WatchTower.Tests/` — Application tests
- `src/Avalonia.Mcp.Core/` — MCP protocol library for Avalonia
- `src/Avalonia.Mcp.Core.Tests/` — MCP library tests
- `src/Avalonia.McpProxy/` — MCP proxy server
- `src/Avalonia.McpProxy.Tests/` — Proxy tests
- `src/McpTestApp/` — Test harness
- `docs/` — Architecture docs, glossary

## Conventions
- Strict MVVM — Views never contain business logic
- Use `CommunityToolkit.Mvvm` source generators (`[ObservableProperty]`, `[RelayCommand]`)
- Avalonia compiled bindings enabled by default
- XYFocus navigation for gamepad support
- Single-file self-contained executables for deployment (win-x64, osx-x64, linux-x64)
- Ancestral Futurism theme: holographic cyan, Ashanti gold, mahogany, void black

## Important Notes
- See `AGENTS.md` for detailed development setup, architecture, and coding guidelines
- FAL.AI key is stored in a local `.env` file — never commit secrets
- The `.github/workflows/copilot-setup-steps.yml` documents the full environment setup
