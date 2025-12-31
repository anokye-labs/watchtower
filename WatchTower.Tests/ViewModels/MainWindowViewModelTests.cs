using Xunit;
using Moq;
using WatchTower.ViewModels;
using WatchTower.Services;
using WatchTower.Tests.TestHelpers;
using WatchTower.Models;
using AdaptiveCards;

namespace WatchTower.Tests.ViewModels;

public class MainWindowViewModelTests
{
    [Fact]
    public void TestInfrastructure_WhenRunning_IsConfiguredCorrectly()
    {
        // This is a placeholder test to verify test infrastructure works
        Assert.True(true);
    }

    #region Event Unsubscription Regression Tests

    [Fact]
    public void Dispose_UnsubscribesFromGameControllerButtonPressedEvent()
    {
        // Arrange
        var mockControllerService = ServiceMocks.CreateGameControllerService();
        var mockCardService = ServiceMocks.CreateAdaptiveCardService();
        var mockThemeService = ServiceMocks.CreateAdaptiveCardThemeService();
        var mockLogger = ServiceMocks.CreateLogger<MainWindowViewModel>();

        var viewModel = new MainWindowViewModel(
            mockControllerService.Object,
            mockCardService.Object,
            mockThemeService.Object,
            mockLogger.Object);

        var initialButtonPressCount = viewModel.ButtonPressCount;

        // Act - Dispose the ViewModel
        viewModel.Dispose();

        // Raise the event after disposal
        mockControllerService.Raise(s => s.ButtonPressed += null,
            new GameControllerButtonEventArgs(0, GameControllerButton.A));

        // Assert - The handler should not have been invoked
        Assert.Equal(initialButtonPressCount, viewModel.ButtonPressCount);
        Assert.Equal("None", viewModel.LastButtonPressed);
    }

    [Fact]
    public void Dispose_UnsubscribesFromGameControllerButtonReleasedEvent()
    {
        // Arrange
        var mockControllerService = ServiceMocks.CreateGameControllerService();
        var mockCardService = ServiceMocks.CreateAdaptiveCardService();
        var mockThemeService = ServiceMocks.CreateAdaptiveCardThemeService();
        var mockLogger = ServiceMocks.CreateLogger<MainWindowViewModel>();

        var viewModel = new MainWindowViewModel(
            mockControllerService.Object,
            mockCardService.Object,
            mockThemeService.Object,
            mockLogger.Object);

        var initialEventCount = viewModel.ControllerEvents.Count;

        // Act - Dispose the ViewModel
        viewModel.Dispose();

        // Raise the event after disposal
        mockControllerService.Raise(s => s.ButtonReleased += null,
            new GameControllerButtonEventArgs(0, GameControllerButton.B));

        // Assert - No new events should be added to the collection
        Assert.Equal(initialEventCount, viewModel.ControllerEvents.Count);
    }

    [Fact]
    public void Dispose_UnsubscribesFromControllerConnectedEvent()
    {
        // Arrange
        var mockControllerService = ServiceMocks.CreateGameControllerService();
        var mockCardService = ServiceMocks.CreateAdaptiveCardService();
        var mockThemeService = ServiceMocks.CreateAdaptiveCardThemeService();
        var mockLogger = ServiceMocks.CreateLogger<MainWindowViewModel>();

        var viewModel = new MainWindowViewModel(
            mockControllerService.Object,
            mockCardService.Object,
            mockThemeService.Object,
            mockLogger.Object);

        var initialEventCount = viewModel.ControllerEvents.Count;
        var initialStatusText = viewModel.StatusText;

        // Act - Dispose the ViewModel
        viewModel.Dispose();

        // Raise the event after disposal
        mockControllerService.Raise(s => s.ControllerConnected += null,
            new GameControllerEventArgs(0, "Test Controller"));

        // Assert - Status should not have changed
        Assert.Equal(initialEventCount, viewModel.ControllerEvents.Count);
        Assert.Equal(initialStatusText, viewModel.StatusText);
    }

    [Fact]
    public void Dispose_UnsubscribesFromControllerDisconnectedEvent()
    {
        // Arrange
        var mockControllerService = ServiceMocks.CreateGameControllerService();
        var mockCardService = ServiceMocks.CreateAdaptiveCardService();
        var mockThemeService = ServiceMocks.CreateAdaptiveCardThemeService();
        var mockLogger = ServiceMocks.CreateLogger<MainWindowViewModel>();

        var viewModel = new MainWindowViewModel(
            mockControllerService.Object,
            mockCardService.Object,
            mockThemeService.Object,
            mockLogger.Object);

        var initialEventCount = viewModel.ControllerEvents.Count;
        var initialStatusText = viewModel.StatusText;

        // Act - Dispose the ViewModel
        viewModel.Dispose();

        // Raise the event after disposal
        mockControllerService.Raise(s => s.ControllerDisconnected += null,
            new GameControllerEventArgs(0, "Test Controller"));

        // Assert - Status should not have changed
        Assert.Equal(initialEventCount, viewModel.ControllerEvents.Count);
        Assert.Equal(initialStatusText, viewModel.StatusText);
    }

    [Fact]
    public void Dispose_UnsubscribesFromThemeChangedEvent()
    {
        // Arrange
        var mockControllerService = ServiceMocks.CreateGameControllerService();
        var mockCardService = ServiceMocks.CreateAdaptiveCardService();
        var mockThemeService = ServiceMocks.CreateAdaptiveCardThemeService();
        var mockLogger = ServiceMocks.CreateLogger<MainWindowViewModel>();

        var viewModel = new MainWindowViewModel(
            mockControllerService.Object,
            mockCardService.Object,
            mockThemeService.Object,
            mockLogger.Object);

        var initialHostConfig = viewModel.HostConfig;

        // Act - Dispose the ViewModel
        viewModel.Dispose();

        // Create a new host config
        var newHostConfig = new AdaptiveCards.Rendering.AdaptiveHostConfig();

        // Raise the event after disposal
        mockThemeService.Raise(s => s.ThemeChanged += null,
            new ThemeChangedEventArgs(ThemeMode.Light, ThemeMode.Light, newHostConfig));

        // Assert - HostConfig should not have changed
        Assert.Same(initialHostConfig, viewModel.HostConfig);
    }

    [Fact]
    public void Dispose_UnsubscribesFromCardActionInvokedEvent()
    {
        // Arrange
        var mockControllerService = ServiceMocks.CreateGameControllerService();
        var mockCardService = ServiceMocks.CreateAdaptiveCardService();
        var mockThemeService = ServiceMocks.CreateAdaptiveCardThemeService();
        var mockLogger = ServiceMocks.CreateLogger<MainWindowViewModel>();

        var viewModel = new MainWindowViewModel(
            mockControllerService.Object,
            mockCardService.Object,
            mockThemeService.Object,
            mockLogger.Object);

        var initialEventCount = viewModel.ControllerEvents.Count;

        // Act - Dispose the ViewModel
        viewModel.Dispose();

        // Raise the event after disposal
        var submitAction = new AdaptiveSubmitAction();
        mockCardService.Raise(s => s.ActionInvoked += null,
            new AdaptiveCardActionEventArgs(submitAction));

        // Assert - No new events should be added to the collection
        Assert.Equal(initialEventCount, viewModel.ControllerEvents.Count);
    }

    [Fact]
    public void Dispose_UnsubscribesFromCardSubmitActionEvent()
    {
        // Arrange
        var mockControllerService = ServiceMocks.CreateGameControllerService();
        var mockCardService = ServiceMocks.CreateAdaptiveCardService();
        var mockThemeService = ServiceMocks.CreateAdaptiveCardThemeService();
        var mockLogger = ServiceMocks.CreateLogger<MainWindowViewModel>();

        var viewModel = new MainWindowViewModel(
            mockControllerService.Object,
            mockCardService.Object,
            mockThemeService.Object,
            mockLogger.Object);

        var initialEventCount = viewModel.ControllerEvents.Count;

        // Act - Dispose the ViewModel
        viewModel.Dispose();

        // Raise the event after disposal
        var submitAction = new AdaptiveSubmitAction();
        var inputValues = new System.Collections.Generic.Dictionary<string, object>();
        mockCardService.Raise(s => s.SubmitAction += null,
            new AdaptiveCardSubmitEventArgs(submitAction, inputValues));

        // Assert - No new events should be added to the collection
        Assert.Equal(initialEventCount, viewModel.ControllerEvents.Count);
    }

    [Fact]
    public void Dispose_UnsubscribesFromCardOpenUrlActionEvent()
    {
        // Arrange
        var mockControllerService = ServiceMocks.CreateGameControllerService();
        var mockCardService = ServiceMocks.CreateAdaptiveCardService();
        var mockThemeService = ServiceMocks.CreateAdaptiveCardThemeService();
        var mockLogger = ServiceMocks.CreateLogger<MainWindowViewModel>();

        var viewModel = new MainWindowViewModel(
            mockControllerService.Object,
            mockCardService.Object,
            mockThemeService.Object,
            mockLogger.Object);

        var initialEventCount = viewModel.ControllerEvents.Count;

        // Act - Dispose the ViewModel
        viewModel.Dispose();

        // Raise the event after disposal
        var openUrlAction = new AdaptiveOpenUrlAction { Url = new System.Uri("https://example.com") };
        mockCardService.Raise(s => s.OpenUrlAction += null,
            new AdaptiveCardActionEventArgs(openUrlAction));

        // Assert - No new events should be added to the collection
        Assert.Equal(initialEventCount, viewModel.ControllerEvents.Count);
    }

    [Fact]
    public void Dispose_UnsubscribesFromCardExecuteActionEvent()
    {
        // Arrange
        var mockControllerService = ServiceMocks.CreateGameControllerService();
        var mockCardService = ServiceMocks.CreateAdaptiveCardService();
        var mockThemeService = ServiceMocks.CreateAdaptiveCardThemeService();
        var mockLogger = ServiceMocks.CreateLogger<MainWindowViewModel>();

        var viewModel = new MainWindowViewModel(
            mockControllerService.Object,
            mockCardService.Object,
            mockThemeService.Object,
            mockLogger.Object);

        var initialEventCount = viewModel.ControllerEvents.Count;

        // Act - Dispose the ViewModel
        viewModel.Dispose();

        // Raise the event after disposal
        var executeAction = new AdaptiveExecuteAction { Verb = "testVerb" };
        mockCardService.Raise(s => s.ExecuteAction += null,
            new AdaptiveCardActionEventArgs(executeAction));

        // Assert - No new events should be added to the collection
        Assert.Equal(initialEventCount, viewModel.ControllerEvents.Count);
    }

    [Fact]
    public void Dispose_UnsubscribesFromCardShowCardActionEvent()
    {
        // Arrange
        var mockControllerService = ServiceMocks.CreateGameControllerService();
        var mockCardService = ServiceMocks.CreateAdaptiveCardService();
        var mockThemeService = ServiceMocks.CreateAdaptiveCardThemeService();
        var mockLogger = ServiceMocks.CreateLogger<MainWindowViewModel>();

        var viewModel = new MainWindowViewModel(
            mockControllerService.Object,
            mockCardService.Object,
            mockThemeService.Object,
            mockLogger.Object);

        var initialEventCount = viewModel.ControllerEvents.Count;

        // Act - Dispose the ViewModel
        viewModel.Dispose();

        // Raise the event after disposal
        var showCardAction = new AdaptiveShowCardAction { Card = new AdaptiveCard("1.3") };
        mockCardService.Raise(s => s.ShowCardAction += null,
            new AdaptiveCardActionEventArgs(showCardAction));

        // Assert - No new events should be added to the collection
        Assert.Equal(initialEventCount, viewModel.ControllerEvents.Count);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes_WithoutException()
    {
        // Arrange
        var mockControllerService = ServiceMocks.CreateGameControllerService();
        var mockCardService = ServiceMocks.CreateAdaptiveCardService();
        var mockThemeService = ServiceMocks.CreateAdaptiveCardThemeService();
        var mockLogger = ServiceMocks.CreateLogger<MainWindowViewModel>();

        var viewModel = new MainWindowViewModel(
            mockControllerService.Object,
            mockCardService.Object,
            mockThemeService.Object,
            mockLogger.Object);

        // Act & Assert - Multiple dispose calls should not throw
        viewModel.Dispose();
        viewModel.Dispose();
        viewModel.Dispose();

        // Verify - Should still be able to raise events without exceptions
        mockControllerService.Raise(s => s.ButtonPressed += null,
            new GameControllerButtonEventArgs(0, GameControllerButton.A));
    }

    #endregion
}
