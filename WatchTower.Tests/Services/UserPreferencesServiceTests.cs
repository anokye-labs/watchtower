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
            FontOverrides = null,
            WindowPosition = null
        };
        _service.SavePreferences(defaultPreferences);
    }

    // ==================== First Run Tests ====================

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

    // ==================== Welcome Screen Tests ====================

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

    // ==================== Window Position Tests ====================

    [Fact]
    public void GetWindowPosition_WhenNoPreferencesSaved_ReturnsNull()
    {
        // Arrange
        ResetToDefaults();

        // Act
        var result = _service.GetWindowPosition();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SetWindowPosition_SavesAndRetrievesPosition()
    {
        // Arrange
        ResetToDefaults();
        var windowPosition = new WindowPositionPreferences
        {
            X = 100.0,
            Y = 200.0,
            Width = 800.0,
            Height = 600.0,
            DisplayBounds = new DisplayBounds
            {
                X = 0,
                Y = 0,
                Width = 1920,
                Height = 1080
            }
        };

        // Act
        _service.SetWindowPosition(windowPosition);
        var retrievedPosition = _service.GetWindowPosition();

        // Assert
        Assert.NotNull(retrievedPosition);
        Assert.Equal(100.0, retrievedPosition.X);
        Assert.Equal(200.0, retrievedPosition.Y);
        Assert.Equal(800.0, retrievedPosition.Width);
        Assert.Equal(600.0, retrievedPosition.Height);
        Assert.NotNull(retrievedPosition.DisplayBounds);
        Assert.Equal(0, retrievedPosition.DisplayBounds.X);
        Assert.Equal(0, retrievedPosition.DisplayBounds.Y);
        Assert.Equal(1920, retrievedPosition.DisplayBounds.Width);
        Assert.Equal(1080, retrievedPosition.DisplayBounds.Height);
    }

    [Fact]
    public void SetWindowPosition_WithNull_ClearsPosition()
    {
        // Arrange
        ResetToDefaults();
        var windowPosition = new WindowPositionPreferences
        {
            X = 100.0,
            Y = 200.0,
            Width = 800.0,
            Height = 600.0,
            DisplayBounds = new DisplayBounds
            {
                X = 0,
                Y = 0,
                Width = 1920,
                Height = 1080
            }
        };
        _service.SetWindowPosition(windowPosition);

        // Act
        _service.SetWindowPosition(null);
        var retrievedPosition = _service.GetWindowPosition();

        // Assert
        Assert.Null(retrievedPosition);
    }

    [Fact]
    public void SetWindowPosition_PersistsAcrossInstances()
    {
        // Arrange
        ResetToDefaults();
        var windowPosition = new WindowPositionPreferences
        {
            X = 150.0,
            Y = 250.0,
            Width = 1024.0,
            Height = 768.0,
            DisplayBounds = new DisplayBounds
            {
                X = 1920,
                Y = 0,
                Width = 1920,
                Height = 1080
            }
        };

        // Act - Save with first instance
        _service.SetWindowPosition(windowPosition);

        // Act - Load with second instance
        var service2 = new UserPreferencesService(_mockLogger.Object);
        var retrievedPosition = service2.GetWindowPosition();

        // Assert
        Assert.NotNull(retrievedPosition);
        Assert.Equal(150.0, retrievedPosition.X);
        Assert.Equal(250.0, retrievedPosition.Y);
        Assert.Equal(1024.0, retrievedPosition.Width);
        Assert.Equal(768.0, retrievedPosition.Height);
        Assert.NotNull(retrievedPosition.DisplayBounds);
        Assert.Equal(1920, retrievedPosition.DisplayBounds.X);
        Assert.Equal(0, retrievedPosition.DisplayBounds.Y);
        Assert.Equal(1920, retrievedPosition.DisplayBounds.Width);
        Assert.Equal(1080, retrievedPosition.DisplayBounds.Height);
    }

    [Fact]
    public void GetWindowPosition_ReturnsDefensiveCopy()
    {
        // Arrange
        ResetToDefaults();
        var windowPosition = new WindowPositionPreferences
        {
            X = 100.0,
            Y = 200.0,
            Width = 800.0,
            Height = 600.0,
            DisplayBounds = new DisplayBounds
            {
                X = 0,
                Y = 0,
                Width = 1920,
                Height = 1080
            }
        };
        _service.SetWindowPosition(windowPosition);

        // Act
        var retrievedPosition1 = _service.GetWindowPosition();
        var retrievedPosition2 = _service.GetWindowPosition();

        // Assert - Should be different instances
        Assert.NotNull(retrievedPosition1);
        Assert.NotNull(retrievedPosition2);
        Assert.NotSame(retrievedPosition1, retrievedPosition2);
        
        // Modify first copy - both top-level and nested properties
        retrievedPosition1.X = 999.0;
        if (retrievedPosition1.DisplayBounds != null)
        {
            retrievedPosition1.DisplayBounds.X = 9999;
        }
        
        // Second copy should be unchanged (deep copy)
        Assert.Equal(100.0, retrievedPosition2.X);
        Assert.NotNull(retrievedPosition2.DisplayBounds);
        Assert.Equal(0, retrievedPosition2.DisplayBounds.X);
    }
}
