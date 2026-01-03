# Testing Guide

This guide documents testing patterns, best practices, and conventions for the WatchTower project.

## Table of Contents

1. [Overview](#overview)
2. [Test Infrastructure](#test-infrastructure)
3. [Testing Patterns](#testing-patterns)
4. [Writing Tests](#writing-tests)
5. [Running Tests](#running-tests)
6. [Coverage Requirements](#coverage-requirements)
7. [Continuous Integration](#continuous-integration)

## Overview

WatchTower uses a comprehensive test infrastructure to ensure code quality and prevent regressions. The test suite includes:

- **Unit Tests**: Test individual classes and methods in isolation
- **Integration Tests**: Test component interactions and workflows
- **Regression Tests**: Prevent known bugs from reoccurring

### Test Framework

- **xUnit 2.***: Primary test framework
- **Moq 4.***: Mocking framework for service dependencies
- **Avalonia.Headless.XUnit 11.***: UI testing without actual windows
- **coverlet.collector 6.***: Code coverage collection

## Test Infrastructure

### Project Structure

```
WatchTower.Tests/
├── Services/           # Service tests
│   ├── LoggingServiceTests.cs
│   ├── UserPreferencesServiceTests.cs
│   ├── AdaptiveCardServiceTests.cs
│   └── ...
├── ViewModels/         # ViewModel tests
│   ├── ViewModelBaseTests.cs
│   ├── RelayCommandTests.cs
│   ├── MainWindowViewModelTests.cs
│   └── ...
├── TestHelpers/        # Reusable test utilities
│   ├── ServiceMocks.cs       # Mock factory methods
│   ├── TestUtilities.cs      # Test data builders
│   └── TestFixtures.cs       # Base classes for tests
└── Integration/        # Integration tests
    └── ...
```

### Test Helpers

#### ServiceMocks

Factory methods for creating mock services with common default setups:

```csharp
// Create a mock logger
var logger = ServiceMocks.CreateLogger<MyService>();

// Create a mock game controller service
var gameController = ServiceMocks.CreateGameControllerService(isInitialized: true);

// Create a mock adaptive card service
var cardService = ServiceMocks.CreateAdaptiveCardService();
```

#### TestUtilities

Helper methods for common test scenarios:

```csharp
// Create test data
var card = TestUtilities.CreateSampleAdaptiveCard();
var json = TestUtilities.CreateSampleAdaptiveCardJson();

// Test property changed events
var wasRaised = TestUtilities.WasPropertyChangedRaised(
    viewModel, 
    nameof(ViewModel.Property), 
    () => viewModel.Property = newValue);

// Wait for async conditions
var result = await TestUtilities.WaitForConditionAsync(
    () => service.IsReady, 
    timeoutMs: 1000);
```

#### Test Base Classes

Base classes provide common setup for different test types:

```csharp
// For ViewModel tests
public class MyViewModelTests : ViewModelTestBase
{
    // Provides ILoggerFactory and CreateLogger<T>()
}

// For Service tests
public class MyServiceTests : ServiceTestBase
{
    // Provides ILoggerFactory, IConfiguration, and TempDirectory
}
```

## Testing Patterns

### Unit Test Pattern

```csharp
[Fact]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange - Set up test dependencies and inputs
    var service = new MyService(logger);
    var input = "test-input";
    
    // Act - Execute the method being tested
    var result = service.ProcessInput(input);
    
    // Assert - Verify the expected outcome
    Assert.Equal("expected-output", result);
}
```

### Testing ViewModels

ViewModels should be tested without UI dependencies:

```csharp
[Fact]
public void Property_WhenChanged_RaisesPropertyChanged()
{
    // Arrange
    var mockService = ServiceMocks.CreateMyService();
    var viewModel = new MyViewModel(mockService.Object);
    var eventRaised = false;
    
    viewModel.PropertyChanged += (s, e) => 
    {
        if (e.PropertyName == nameof(MyViewModel.MyProperty))
            eventRaised = true;
    };
    
    // Act
    viewModel.MyProperty = "new-value";
    
    // Assert
    Assert.True(eventRaised);
    Assert.Equal("new-value", viewModel.MyProperty);
}
```

### Testing Commands

```csharp
[Fact]
public void Command_WhenExecuted_PerformsAction()
{
    // Arrange
    var viewModel = new MyViewModel();
    var executed = false;
    
    // Act
    viewModel.MyCommand.Execute(null);
    
    // Assert
    Assert.True(executed);
}

[Fact]
public void Command_WithCanExecuteFalse_CannotExecute()
{
    // Arrange
    var viewModel = new MyViewModel { IsEnabled = false };
    
    // Act
    var canExecute = viewModel.MyCommand.CanExecute(null);
    
    // Assert
    Assert.False(canExecute);
}
```

### Testing Services

Services should be tested with mocked dependencies:

```csharp
[Fact]
public void Service_ProcessesData_Successfully()
{
    // Arrange
    var mockDependency = new Mock<IDependency>();
    mockDependency.Setup(d => d.GetData()).Returns("test-data");
    
    var service = new MyService(mockDependency.Object);
    
    // Act
    var result = service.Process();
    
    // Assert
    Assert.NotNull(result);
    mockDependency.Verify(d => d.GetData(), Times.Once);
}
```

### Testing Events

```csharp
[Fact]
public void Service_WhenActionOccurs_RaisesEvent()
{
    // Arrange
    var service = new MyService();
    var eventRaised = false;
    MyEventArgs? eventArgs = null;
    
    service.MyEvent += (sender, args) =>
    {
        eventRaised = true;
        eventArgs = args;
    };
    
    // Act
    service.TriggerAction();
    
    // Assert
    Assert.True(eventRaised);
    Assert.NotNull(eventArgs);
}
```

### Testing Async Operations

```csharp
[Fact]
public async Task Service_ProcessesAsync_Successfully()
{
    // Arrange
    var service = new MyService();
    
    // Act
    var result = await service.ProcessAsync();
    
    // Assert
    Assert.NotNull(result);
}

[Fact]
public async Task Service_WhenTimeout_ThrowsException()
{
    // Arrange
    var service = new MyService(timeout: 1);
    
    // Act & Assert
    await Assert.ThrowsAsync<TimeoutException>(() => 
        service.ProcessAsync());
}
```

### Testing Thread Safety

```csharp
[Fact]
public void Service_ConcurrentAccess_IsSafe()
{
    // Arrange
    var service = new MyService();
    
    // Act - Multiple concurrent operations
    var tasks = Enumerable.Range(0, 10)
        .Select(i => Task.Run(() => service.Process(i)))
        .ToArray();
    
    Task.WaitAll(tasks);
    
    // Assert - Service should be in valid state
    Assert.True(service.IsValid);
}
```

### Testing Disposal

```csharp
[Fact]
public void Service_WhenDisposed_ReleasesResources()
{
    // Arrange
    var service = new MyService();
    
    // Act
    service.Dispose();
    
    // Assert
    Assert.Throws<ObjectDisposedException>(() => service.Process());
}

[Fact]
public void Service_DisposedMultipleTimes_DoesNotThrow()
{
    // Arrange
    var service = new MyService();
    
    // Act & Assert
    service.Dispose();
    var exception = Record.Exception(() => service.Dispose());
    Assert.Null(exception);
}
```

## Writing Tests

### Best Practices

1. **Test One Thing**: Each test should verify a single behavior
2. **Arrange-Act-Assert**: Follow the AAA pattern consistently
3. **Descriptive Names**: Use the format `MethodName_Scenario_ExpectedBehavior`
4. **Independent Tests**: Tests should not depend on each other
5. **Fast Tests**: Unit tests should execute quickly (< 100ms)
6. **Deterministic Tests**: Tests should always produce the same result

### Naming Conventions

```csharp
// Good
[Fact]
public void GetUser_WithValidId_ReturnsUser()

[Fact]
public void GetUser_WithInvalidId_ReturnsNull()

[Fact]
public void SaveUser_WithNullUser_ThrowsArgumentNullException()

// Bad
[Fact]
public void Test1()

[Fact]
public void TestGetUser()

[Fact]
public void UserTest()
```

### What to Test

**DO Test:**
- Public methods and properties
- Edge cases and boundary conditions
- Error handling and exceptions
- Event raising
- Property change notifications
- Command execution and CanExecute
- Thread safety for concurrent code
- Disposal and cleanup

**DON'T Test:**
- Private methods (test through public API)
- Third-party libraries
- Framework code
- Trivial getters/setters (unless they have logic)

## Running Tests

### Command Line

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run tests with detailed output
dotnet test --verbosity detailed

# Run specific test class
dotnet test --filter "FullyQualifiedName~MainWindowViewModelTests"

# Run specific test method
dotnet test --filter "Name=PropertyChanged_WhenSet_RaisesEvent"
```

### Visual Studio Code

Press F5 or use the Test Explorer to run tests interactively.

### Watch Mode

```bash
dotnet watch test
```

## Coverage Requirements

### Minimum Coverage Targets

- **ViewModels**: 80% line coverage
- **Services**: 80% line coverage  
- **ViewModelBase**: 90% line coverage
- **RelayCommand**: 90% line coverage
- **Overall**: 75% line coverage

### Viewing Coverage

```bash
# Generate HTML coverage report
dotnet tool install -g dotnet-reportgenerator-globaltool
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:./TestResults/**/coverage.cobertura.xml \
                -targetdir:./coveragereport \
                -reporttypes:Html

# Open the report
# Linux/macOS: xdg-open ./coveragereport/index.html
# Windows: start "" "./coveragereport/index.html"
```

### Coverage in CI/CD

Coverage reports are automatically generated for pull requests and displayed as comments.

## Continuous Integration

### Automated Testing

Tests run automatically on:
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop`
- Manual workflow dispatch

### Multi-Platform Testing

Tests run on all supported platforms:
- **Ubuntu (Linux)**: linux-x64 runtime
- **Windows**: win-x64 runtime
- **macOS**: osx-x64 runtime

### Test Failure Policy

- All tests must pass before merging
- Coverage must not decrease
- New code should include tests

## Regression Tests

Document known bugs that have been fixed to prevent regression:

```csharp
[Fact]
public void Issue58_EventUnsubscription_DoesNotLeakMemory()
{
    // Regression test for GitHub issue #58
    // Ensures event handlers are properly unsubscribed
    
    // Arrange
    var fixture = new EventSubscriptionTestFixture();
    var service = new MyService();
    var viewModel = new MyViewModel(service);
    
    fixture.Track(viewModel);
    
    // Act
    viewModel.Dispose();
    viewModel = null;
    
    // Assert
    fixture.VerifyCollected(); // Should be garbage collected
}
```

## Examples

See the test files in `WatchTower.Tests/` for complete examples:

- `ViewModels/ViewModelBaseTests.cs` - Property change notification testing
- `ViewModels/RelayCommandTests.cs` - Command testing patterns
- `Services/UserPreferencesServiceTests.cs` - File I/O and thread-safety testing
- `Services/AdaptiveCardServiceTests.cs` - Event testing patterns

## Questions?

If you have questions about testing:
1. Check existing test files for similar scenarios
2. Review this guide for patterns
3. Open an issue or discussion on GitHub
