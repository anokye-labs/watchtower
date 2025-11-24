# WatchTower Constitution

## Core Principles

### I. MVVM Architecture Pattern

All UI components MUST follow the Model-View-ViewModel (MVVM) pattern with strict separation of concerns:

- **ViewModels**: Handle all business logic, state management, data binding, and user interaction handling
- **Views**: Handle presentation only (XAML/UI markup), no logic beyond data binding expressions
- **Models**: Represent data structures and business entities
- **NO code-behind logic** in Views except initialization; all interaction logic belongs in ViewModels
- ViewModels must be independently testable without UI dependencies

**Rationale**: MVVM provides clear separation of concerns, enables comprehensive unit testing of logic without UI dependencies, and follows Avalonia best practices for maintainable cross-platform applications.

### II. Service Layer Architecture

Business logic MUST be organized into service classes that ViewModels consume:

- Services encapsulate reusable business operations
- Services are injected via dependency injection
- Services have clear interfaces for testability
- ViewModels orchestrate services but don't implement complex logic themselves

**Rationale**: Keeps ViewModels thin, promotes code reuse, and maintains single responsibility principle.

### III. Test-First Development

TDD approach is strongly recommended:

- Write tests for ViewModels before implementation
- Tests verify business logic independent of UI
- Integration tests for service interactions
- UI tests for critical user workflows

**Rationale**: Ensures code quality, catches regressions early, and validates the MVVM separation is maintained.

### IV. Dependency Injection

All dependencies MUST be injected:

- Services injected into ViewModels via constructor injection
- ViewModels registered in DI container
- No static dependencies or service locators
- Clear dependency graph for testing and maintenance

**Rationale**: Enables loose coupling, testability, and maintainability of the application architecture.

## Technology Standards

### Cross-Platform Native Requirements

**MANDATORY**: Application MUST run natively on Windows, macOS, and Linux with equal functionality:

- All three platforms are first-class targets
- Platform-specific rendering optimizations are permitted where beneficial
- Native look-and-feel per platform is encouraged
- No platform may be treated as second-tier or unsupported

### Avalonia UI Framework

**MANDATORY**: Use the latest stable version of Avalonia UI:

- Always use the most recent stable Avalonia release
- Proactively upgrade to new major/minor versions when released
- Monitor Avalonia releases and offer to upgrade when updates are available
- Cross-platform UI framework targeting Windows, macOS, Linux
- XAML-based declarative UI design
- Reactive UI patterns with property change notifications
- Built-in MVVM support with data binding
- Platform-specific features via conditional rendering where needed

**Rationale**: Latest Avalonia versions provide best performance, bug fixes, new features, and ongoing platform support. Staying current prevents technical debt and ensures access to modern UI capabilities.

### Development Stack

- .NET 10+ for application development (latest LTS or stable)
- C# as primary programming language (latest language version)
- Avalonia UI (latest stable version - currently check NuGet for most recent)
- Dependency injection via built-in DI container
- xUnit or NUnit for testing framework

## Code Quality Standards

### Testability Requirements

- ViewModels MUST be unit testable without UI dependencies
- Services MUST have interface abstractions for mocking
- Minimum 80% code coverage for ViewModels and Services
- Integration tests for cross-service interactions

### Code Organization

- Clear folder structure: Models/, ViewModels/, Views/, Services/
- One class per file with meaningful names
- Proper namespace organization matching folder structure
- No circular dependencies between layers

## Governance

This constitution supersedes all other development practices and conventions. All features, code reviews, and architectural decisions MUST align with these principles.

### Amendment Process

- Constitution changes require explicit documentation and rationale
- Version must be incremented following semantic versioning
- Major changes require team review and consensus
- All dependent templates and documentation must be updated

### Compliance

- All code reviews MUST verify MVVM pattern compliance
- Architecture violations MUST be rejected in PR reviews
- Regular audits to ensure ongoing compliance
- Technical debt related to architecture must be prioritized
- Cross-platform testing on all three target platforms before releases
- Avalonia version MUST be kept current with latest stable releases

**Version**: 1.1.0 | **Ratified**: 2025-11-24 | **Last Amended**: 2025-11-24
