using System;
using Moq;
using Xunit;
using WatchTower.Services;
using WatchTower.ViewModels;

namespace WatchTower.Tests.ViewModels;

public class WelcomeContentViewModelTests
{
    private readonly Mock<IUserPreferencesService> _mockPreferencesService;

    public WelcomeContentViewModelTests()
    {
        _mockPreferencesService = new Mock<IUserPreferencesService>();
    }

    [Fact]
    public void Constructor_WithNullService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new WelcomeContentViewModel(null!));
    }

    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        // Act
        var viewModel = new WelcomeContentViewModel(_mockPreferencesService.Object);

        // Assert
        Assert.NotNull(viewModel);
        Assert.False(viewModel.DontShowAgain);
        Assert.NotNull(viewModel.DismissCommand);
    }

    [Fact]
    public void DontShowAgain_SetProperty_UpdatesValue()
    {
        // Arrange
        var viewModel = new WelcomeContentViewModel(_mockPreferencesService.Object);

        // Act
        viewModel.DontShowAgain = true;

        // Assert
        Assert.True(viewModel.DontShowAgain);
    }

    [Fact]
    public void DismissCommand_WhenDontShowAgainIsTrue_CallsMarkWelcomeScreenSeen()
    {
        // Arrange
        var viewModel = new WelcomeContentViewModel(_mockPreferencesService.Object);
        viewModel.DontShowAgain = true;

        // Act
        viewModel.DismissCommand.Execute(null);

        // Assert
        _mockPreferencesService.Verify(s => s.MarkWelcomeScreenSeen(), Times.Once);
    }

    [Fact]
    public void DismissCommand_WhenDontShowAgainIsFalse_DoesNotCallMarkWelcomeScreenSeen()
    {
        // Arrange
        var viewModel = new WelcomeContentViewModel(_mockPreferencesService.Object);
        viewModel.DontShowAgain = false;

        // Act
        viewModel.DismissCommand.Execute(null);

        // Assert
        _mockPreferencesService.Verify(s => s.MarkWelcomeScreenSeen(), Times.Never);
    }

    [Fact]
    public void DismissCommand_RaisesWelcomeDismissedEvent()
    {
        // Arrange
        var viewModel = new WelcomeContentViewModel(_mockPreferencesService.Object);
        var eventRaised = false;
        viewModel.WelcomeDismissed += (s, e) => eventRaised = true;

        // Act
        viewModel.DismissCommand.Execute(null);

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void DismissCommand_RaisesWelcomeDismissedEventWithCorrectSender()
    {
        // Arrange
        var viewModel = new WelcomeContentViewModel(_mockPreferencesService.Object);
        object? sender = null;
        viewModel.WelcomeDismissed += (s, e) => sender = s;

        // Act
        viewModel.DismissCommand.Execute(null);

        // Assert
        Assert.Same(viewModel, sender);
    }

    [Fact]
    public void DismissCommand_RaisesWelcomeDismissedEventWithEmptyArgs()
    {
        // Arrange
        var viewModel = new WelcomeContentViewModel(_mockPreferencesService.Object);
        EventArgs? args = null;
        viewModel.WelcomeDismissed += (s, e) => args = e;

        // Act
        viewModel.DismissCommand.Execute(null);

        // Assert
        Assert.NotNull(args);
        Assert.Same(EventArgs.Empty, args);
    }

    [Fact]
    public void DismissCommand_CanExecute_ReturnsTrue()
    {
        // Arrange
        var viewModel = new WelcomeContentViewModel(_mockPreferencesService.Object);

        // Act
        var canExecute = viewModel.DismissCommand.CanExecute(null);

        // Assert
        Assert.True(canExecute);
    }
}
