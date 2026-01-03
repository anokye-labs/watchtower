# AdaptiveCard Display Engine

## Overview

The WatchTower application now includes a comprehensive AdaptiveCard Display Engine that serves as the core UI rendering system. This implementation allows the application to display rich, interactive, and responsive user interfaces using Microsoft's Adaptive Cards framework integrated with Avalonia UI.

## Architecture

This implementation follows strict **MVVM (Model-View-ViewModel)** pattern with dependency injection, ensuring clean separation of concerns and maintainability.

### Components

#### 1. Services Layer (`/Services`)

**IAdaptiveCardService** - Interface defining the contract for AdaptiveCard operations:
- `LoadCardFromJson(string cardJson)` - Parses AdaptiveCards from JSON strings
- `CreateSampleCard()` - Creates demonstration cards

**AdaptiveCardService** - Service implementation that:
- Manages card loading and parsing
- Provides sample card creation
- Handles errors with comprehensive logging
- Injects `ILogger<AdaptiveCardService>` for diagnostics

#### 2. ViewModels Layer (`/ViewModels`)

**ViewModelBase** - Abstract base class providing:
- `INotifyPropertyChanged` implementation
- `SetProperty<T>()` helper for property change notifications
- Foundation for all ViewModels in the application

**MainWindowViewModel** - Main window's ViewModel that:
- Manages the currently displayed AdaptiveCard
- Orchestrates the AdaptiveCardService
- Exposes `CurrentCard` property bound to the view
- Provides methods to load cards from JSON or samples
- No UI dependencies - fully testable in isolation

#### 3. Views Layer (`/Views`)

**MainWindow.axaml** - Main window XAML that:
- Declares AdaptiveCardView control with data binding
- Binds to MainWindowViewModel's `CurrentCard` property
- Uses `ScrollViewer` for card scrolling
- Maintains visual acrylic blur background
- **Contains no logic** - pure presentation

### Dependency Injection Setup

The `App.axaml.cs` has been updated to configure DI:

```csharp
services.AddSingleton<IAdaptiveCardService, AdaptiveCardService>();
services.AddTransient<MainWindowViewModel>();
```

ViewModels receive their dependencies via constructor injection, following best practices.

## NuGet Packages

The following packages were added to enable AdaptiveCard rendering:

- **Iciclecreek.AdaptiveCards.Rendering.Avalonia** (v1.0.4) - Renders AdaptiveCards in Avalonia
- **Microsoft.Extensions.DependencyInjection** (v10.0.0) - Dependency injection container

## Usage

### Displaying a Sample Card

The application automatically loads and displays a sample AdaptiveCard on startup:

```csharp
// In MainWindowViewModel constructor
LoadSampleCard();
```

### Loading Custom Cards from JSON

```csharp
var cardJson = @"{
    ""type"": ""AdaptiveCard"",
    ""version"": ""1.5"",
    ""body"": [
        {
            ""type"": ""TextBlock"",
            ""text"": ""Hello World""
        }
    ]
}";

viewModel.LoadCardFromJson(cardJson);
```

### Extending with Custom Cards

To create custom AdaptiveCards:

1. Implement card creation in `AdaptiveCardService`
2. Expose the method through `IAdaptiveCardService`
3. Call from ViewModel to update `CurrentCard` property
4. The View automatically updates via data binding

## Sample Card

The default sample card demonstrates:
- Title and subtitle text blocks
- Multi-column layout with status indicators
- Action buttons (Learn More link)
- Proper spacing and styling

## MVVM Compliance

✅ **ViewModels**: Contain all UI logic and state management  
✅ **Views**: Pure XAML with data binding only  
✅ **Services**: Encapsulate business logic  
✅ **Dependency Injection**: All dependencies injected via constructor  
✅ **Testability**: ViewModels have no UI dependencies

## Windows Support

The AdaptiveCard rendering works on Windows (win-x64), leveraging Avalonia's rendering capabilities.

## Future Enhancements

Potential improvements to consider:
- Custom AdaptiveCard host config for theming
- Action handling for Submit and Execute actions
- Dynamic card loading from external sources
- Card templates library
- User interaction logging and analytics

## Testing

To test ViewModels:

```csharp
var logger = Mock.Of<ILogger<AdaptiveCardService>>();
var service = new AdaptiveCardService(logger);
var vmLogger = Mock.Of<ILogger<MainWindowViewModel>>();
var viewModel = new MainWindowViewModel(service, vmLogger);

Assert.NotNull(viewModel.CurrentCard);
```

No UI dependencies required for unit testing.

## References

- [Adaptive Cards Official Site](https://adaptivecards.io/)
- [Iciclecreek.AdaptiveCards.Rendering.Avalonia GitHub](https://github.com/tomlm/Iciclecreek.AdaptiveCards.Rendering.Avalonia)
- [Avalonia UI Documentation](https://docs.avaloniaui.net/)
