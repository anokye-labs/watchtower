# Contributing to WatchTower

Thank you for your interest in contributing to WatchTower! This document provides guidelines and information for contributors.

## Getting Started

Before contributing, please familiarize yourself with the project by reading the [README](README.md) for an overview, the [Architecture documentation](docs/ARCHITECTURE.md) for system design, the [Glossary](docs/GLOSSARY.md) for codebase-specific terms, and the [Development Guidelines](AGENTS.md) for coding standards.

## Development Environment

### Prerequisites

You will need the .NET 10 SDK from https://dotnet.microsoft.com/download, Visual Studio Code with the C# extension, and Git for version control.

### Setup

Clone the repository and open it in VS Code:

```bash
git clone https://github.com/anokye-labs/watchtower.git
cd watchtower
code .
```

VS Code will prompt for recommended extensions. Install them for the best development experience.

### Building

```bash
dotnet build
```

### Running

```bash
dotnet run --project WatchTower/WatchTower.csproj
```

### Hot Reload Development

```bash
dotnet watch run --project WatchTower/WatchTower.csproj --non-interactive
```

## Architecture Guidelines

WatchTower follows strict MVVM (Model-View-ViewModel) architecture with dependency injection. All contributions must adhere to these patterns.

### MVVM Compliance

Views (XAML files) should contain only bindings and initialization. No business logic should be placed in code-behind files except for event forwarding that cannot be expressed in XAML.

ViewModels contain all presentation logic. They implement `INotifyPropertyChanged` for data binding, expose commands for user actions, and have no dependencies on UI types.

Services encapsulate business logic. They are defined by interfaces, registered in the DI container, and injected into ViewModels via constructor injection.

### Code Organization

Place new Views in `WatchTower/Views/` with the `.axaml` extension. Place new ViewModels in `WatchTower/ViewModels/` with the `ViewModel` suffix. Place new Services in `WatchTower/Services/` with corresponding interfaces prefixed with `I`. Place new Models in `WatchTower/Models/`.

## Code Style

### C# Conventions

Follow .NET Runtime coding guidelines. Use Allman braces (opening brace on new line). Use 4-space indentation. Name private and internal fields with `_camelCase`. Name static fields with `s_` prefix. Name thread-static fields with `t_` prefix. Avoid using `this.` qualifier. Use explicit visibility modifiers. Sort using directives with System namespaces first. Use `var` only when the type is explicit on the right-hand side. Use language keywords over BCL types (e.g., `string` not `String`). Use PascalCase for constants and methods. Use `nameof` instead of string literals for member names.

### Formatting

Run `dotnet format` before committing to ensure consistent formatting. The project uses `.editorconfig` for style enforcement.

### Package Management

Use `dotnet add package` to add packages. Never hand-edit the `.csproj` file for package references.

## Testing

### Requirements

Aim for 80% code coverage for ViewModels and Services. Write tests before implementation when possible. ViewModels should be testable without UI dependencies.

### Frameworks

Use xUnit or NUnit for unit testing. Use Avalonia.Headless for UI testing without browser dependencies.

### Running Tests

```bash
dotnet test
```

## Pull Request Process

### Before Submitting

Ensure your code builds without errors or warnings by running `dotnet build`. Run `dotnet format` to ensure consistent formatting. Write or update tests for your changes. Update documentation if your changes affect public APIs or behavior. Verify your changes work on all target platforms if possible.

### Branch Naming

Use descriptive branch names with the format `feature/description` for new features, `fix/description` for bug fixes, and `docs/description` for documentation changes.

### Commit Messages

Write clear, concise commit messages. Use the imperative mood (e.g., "Add feature" not "Added feature"). Reference issue numbers when applicable.

### PR Description

Describe what your changes do and why. Include any relevant issue numbers. List any breaking changes. Include screenshots for UI changes.

### Review Process

All PRs require review before merging. Address review feedback promptly. Keep PRs focused and reasonably sized.

## Issue Reporting

### Bug Reports

Include a clear description of the bug, steps to reproduce, expected behavior, actual behavior, platform and .NET version, and relevant logs or screenshots.

### Feature Requests

Describe the feature and its use case. Explain how it fits with the project's goals. Consider implementation complexity.

## Documentation

### When to Update Documentation

Update documentation when adding new features, changing existing behavior, modifying public APIs, or fixing documentation errors.

### Documentation Files

The main README provides project overview and quick start. The docs/ARCHITECTURE.md file covers system design. The docs/GLOSSARY.md file defines codebase terms. Feature-specific docs go in the docs/ directory.

### Style

Write in clear, flowing prose. Avoid excessive bullet points in explanatory text. Use code blocks for commands and code examples. Keep documentation up to date with code changes.

## Windows Focus

WatchTower is a Windows-native application targeting Windows 10 and Windows 11. All contributions should be tested on Windows.

### Windows-Optimized Code

Leverage Windows-specific features and APIs when they enhance user experience. Document any Windows version requirements (e.g., Windows 10+ only features) or optimizations in the code.

### Testing

Test on Windows 10 and Windows 11. Document any version-specific behavior.

## License

By contributing to WatchTower, you agree that your contributions will be licensed under the MIT License.

## Questions

If you have questions about contributing, please open an issue or reach out to the maintainers.

## Acknowledgments

Thank you to all contributors who help make WatchTower better!
