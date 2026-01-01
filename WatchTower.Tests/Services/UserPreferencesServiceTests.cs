using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using WatchTower.Models;
using WatchTower.Services;

namespace WatchTower.Tests.Services;

public class UserPreferencesServiceTests : IDisposable
{
    private readonly UserPreferencesService _service;
    private readonly Mock<ILogger<UserPreferencesService>> _mockLogger;
    private readonly string _preferencesPath;

    public UserPreferencesServiceTests()
    {
        _mockLogger = new Mock<ILogger<UserPreferencesService>>();
        _service = new UserPreferencesService(_mockLogger.Object);
        
        // Get the actual preferences path for cleanup
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var watchTowerPath = Path.Combine(appDataPath, "WatchTower");
        _preferencesPath = Path.Combine(watchTowerPath, "user-preferences.json");
        
        // Start with a clean state - reset to defaults
        ResetToDefaults();
    }

    public void Dispose()
    {
        // Clean up - restore to defaults after tests
        ResetToDefaults();
    }

    private void ResetToDefaults()
    {
        var defaultPreferences = new UserPreferences
        {
            IsFirstRun = true,
            HasSeenWelcomeScreen = false,
            WelcomeScreenDismissedDate = null,
            ThemeMode = ThemeMode.System,
            FontOverrides = null
        };
        _service.SavePreferences(defaultPreferences);
    }

    [Fact]
    public void IsFirstRun_AfterReset_ReturnsTrue()
    {
        // Arrange
        ResetToDefaults();

        // Act
        var isFirstRun = _service.IsFirstRun();

        // Assert
        Assert.True(isFirstRun);
    }

    [Fact]
    public void MarkFirstRunComplete_SetsIsFirstRunToFalse()
    {
        // Arrange
        ResetToDefaults();
        Assert.True(_service.IsFirstRun());

        // Act
        _service.MarkFirstRunComplete();

        // Assert
        Assert.False(_service.IsFirstRun());
    }

    [Fact]
    public void MarkFirstRunComplete_RaisesPreferencesChangedEvent()
    {
        // Arrange
        ResetToDefaults();
        var eventRaised = false;
        _service.PreferencesChanged += (s, e) => eventRaised = true;

        // Act
        _service.MarkFirstRunComplete();

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void MarkFirstRunComplete_WhenAlreadyComplete_DoesNotRaiseEvent()
    {
        // Arrange
        ResetToDefaults();
        _service.MarkFirstRunComplete();
        var eventRaised = false;
        _service.PreferencesChanged += (s, e) => eventRaised = true;

        // Act
        _service.MarkFirstRunComplete();

        // Assert
        Assert.False(eventRaised);
    }

    [Fact]
    public void HasSeenWelcomeScreen_AfterReset_ReturnsFalse()
    {
        // Arrange
        ResetToDefaults();

        // Act
        var hasSeen = _service.HasSeenWelcomeScreen();

        // Assert
        Assert.False(hasSeen);
    }

    [Fact]
    public void MarkWelcomeScreenSeen_SetsHasSeenWelcomeScreenToTrue()
    {
        // Arrange
        ResetToDefaults();
        Assert.False(_service.HasSeenWelcomeScreen());

        // Act
        _service.MarkWelcomeScreenSeen();

        // Assert
        Assert.True(_service.HasSeenWelcomeScreen());
    }

    [Fact]
    public void MarkWelcomeScreenSeen_SetsDismissedDate()
    {
        // Arrange
        ResetToDefaults();
        var beforeCall = DateTime.UtcNow;

        // Act
        _service.MarkWelcomeScreenSeen();
        var preferences = _service.GetPreferences();

        // Assert
        Assert.NotNull(preferences.WelcomeScreenDismissedDate);
        Assert.True(preferences.WelcomeScreenDismissedDate >= beforeCall);
        Assert.True(preferences.WelcomeScreenDismissedDate <= DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void MarkWelcomeScreenSeen_RaisesPreferencesChangedEvent()
    {
        // Arrange
        ResetToDefaults();
        var eventRaised = false;
        _service.PreferencesChanged += (s, e) => eventRaised = true;

        // Act
        _service.MarkWelcomeScreenSeen();

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void MarkWelcomeScreenSeen_WhenAlreadySeen_DoesNotRaiseEvent()
    {
        // Arrange
        ResetToDefaults();
        _service.MarkWelcomeScreenSeen();
        var eventRaised = false;
        _service.PreferencesChanged += (s, e) => eventRaised = true;

        // Act
        _service.MarkWelcomeScreenSeen();

        // Assert
        Assert.False(eventRaised);
    }

    [Fact]
    public void GetPreferences_AfterReset_ReturnsDefaultValues()
    {
        // Arrange
        ResetToDefaults();

        // Act
        var preferences = _service.GetPreferences();

        // Assert
        Assert.NotNull(preferences);
        Assert.True(preferences.IsFirstRun);
        Assert.False(preferences.HasSeenWelcomeScreen);
        Assert.Null(preferences.WelcomeScreenDismissedDate);
        Assert.Equal(ThemeMode.System, preferences.ThemeMode);
    }

    [Fact]
    public void SavePreferences_PersistsWelcomeScreenState()
    {
        // Arrange
        ResetToDefaults();
        var preferences = _service.GetPreferences();
        preferences.IsFirstRun = false;
        preferences.HasSeenWelcomeScreen = true;
        preferences.WelcomeScreenDismissedDate = DateTime.UtcNow;

        // Act
        _service.SavePreferences(preferences);

        // Create new service instance to verify persistence
        var newService = new UserPreferencesService(_mockLogger.Object);
        var loadedPreferences = newService.GetPreferences();

        // Assert
        Assert.False(loadedPreferences.IsFirstRun);
        Assert.True(loadedPreferences.HasSeenWelcomeScreen);
        Assert.NotNull(loadedPreferences.WelcomeScreenDismissedDate);
    }
}
