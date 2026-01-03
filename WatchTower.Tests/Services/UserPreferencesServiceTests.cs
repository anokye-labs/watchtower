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
    private readonly Mock<ILogger<UserPreferencesService>> _loggerMock;
    private readonly string _preferencesFilePath;

    public UserPreferencesServiceTests()
    {
        // Create a temporary directory for test preferences
        var tempDir = Path.Combine(Path.GetTempPath(), $"WatchTowerTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        
        // Mock the preferences file path by using environment variable
        Environment.SetEnvironmentVariable("APPDATA", tempDir);
        Environment.SetEnvironmentVariable("HOME", tempDir);
        
        _loggerMock = new Mock<ILogger<UserPreferencesService>>();
        
        // Calculate the preferences file path
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var watchTowerPath = Path.Combine(appDataPath, "WatchTower");
        _preferencesFilePath = Path.Combine(watchTowerPath, "user-preferences.json");
        
        // Clean up any existing preferences before each test
        if (File.Exists(_preferencesFilePath))
        {
            File.Delete(_preferencesFilePath);
        }
    }

    public void Dispose()
    {
        // Clean up test files
        try
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var watchTowerPath = Path.Combine(appDataPath, "WatchTower");
            if (Directory.Exists(watchTowerPath))
            {
                Directory.Delete(watchTowerPath, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    // ==================== Welcome Screen Tests ====================

    [Fact]
    public void Constructor_CreatesDefaultPreferences_WhenNoFileExists()
    {
        // Arrange & Act
        var service = new UserPreferencesService(_loggerMock.Object);
        var preferences = service.GetPreferences();

        // Assert
        Assert.NotNull(preferences);
        Assert.Equal(ThemeMode.System, preferences.ThemeMode);
        Assert.False(preferences.HasSeenWelcomeScreen);
        Assert.True(preferences.ShowWelcomeOnStartup);
        Assert.NotNull(preferences.FirstRunDate);
    }

    [Fact]
    public void Constructor_SetsFirstRunDate_WhenNewInstallation()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow.AddSeconds(-1);
        
        // Act
        var service = new UserPreferencesService(_loggerMock.Object);
        var firstRunDate = service.GetFirstRunDate();
        
        var afterCreation = DateTime.UtcNow.AddSeconds(1);

        // Assert
        Assert.NotNull(firstRunDate);
        Assert.True(firstRunDate.Value >= beforeCreation);
        Assert.True(firstRunDate.Value <= afterCreation);
    }

    [Fact]
    public void GetHasSeenWelcomeScreen_ReturnsDefault_WhenNewInstallation()
    {
        // Arrange
        var service = new UserPreferencesService(_loggerMock.Object);

        // Act
        var hasSeenWelcomeScreen = service.GetHasSeenWelcomeScreen();

        // Assert
        Assert.False(hasSeenWelcomeScreen);
    }

    [Fact]
    public void SetHasSeenWelcomeScreen_UpdatesValue_AndPersists()
    {
        // Arrange
        var service = new UserPreferencesService(_loggerMock.Object);

        // Act
        service.SetHasSeenWelcomeScreen(true);
        var result = service.GetHasSeenWelcomeScreen();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetShowWelcomeOnStartup_ReturnsDefault_WhenNewInstallation()
    {
        // Arrange
        var service = new UserPreferencesService(_loggerMock.Object);

        // Act
        var showWelcomeOnStartup = service.GetShowWelcomeOnStartup();

        // Assert
        Assert.True(showWelcomeOnStartup);
    }

    [Fact]
    public void SetShowWelcomeOnStartup_UpdatesValue_AndPersists()
    {
        // Arrange
        var service = new UserPreferencesService(_loggerMock.Object);

        // Act
        service.SetShowWelcomeOnStartup(false);
        var result = service.GetShowWelcomeOnStartup();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void SaveAndLoadPreferences_SerializesNewProperties_Correctly()
    {
        // Arrange
        var service1 = new UserPreferencesService(_loggerMock.Object);
        var originalFirstRunDate = service1.GetFirstRunDate();

        // Act - Save preferences with new properties set
        var preferences = service1.GetPreferences();
        preferences.HasSeenWelcomeScreen = true;
        preferences.ShowWelcomeOnStartup = false;
        service1.SavePreferences(preferences);

        // Create a new service instance to load from disk
        var service2 = new UserPreferencesService(_loggerMock.Object);
        var loadedPreferences = service2.GetPreferences();

        // Assert
        Assert.True(loadedPreferences.HasSeenWelcomeScreen);
        Assert.False(loadedPreferences.ShowWelcomeOnStartup);
        Assert.NotNull(loadedPreferences.FirstRunDate);
        // FirstRunDate should be preserved (not changed by SavePreferences)
        Assert.Equal(originalFirstRunDate, loadedPreferences.FirstRunDate);
    }

    [Fact]
    public void LoadPreferences_AppliesMigration_ForExistingUsersWithoutNewProperties()
    {
        // Arrange - Create a preferences file with only old properties
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var watchTowerPath = Path.Combine(appDataPath, "WatchTower");
        Directory.CreateDirectory(watchTowerPath);
        var preferencesPath = Path.Combine(watchTowerPath, "user-preferences.json");
        
        var oldPreferencesJson = @"{
  ""themeMode"": ""Dark"",
  ""fontOverrides"": null
}";
        File.WriteAllText(preferencesPath, oldPreferencesJson);

        // Act
        var service = new UserPreferencesService(_loggerMock.Object);
        var preferences = service.GetPreferences();

        // Assert - Default values should be applied
        Assert.False(preferences.HasSeenWelcomeScreen);
        Assert.True(preferences.ShowWelcomeOnStartup);
        Assert.NotNull(preferences.FirstRunDate);
    }

    [Fact]
    public void SetHasSeenWelcomeScreen_RaisesPreferencesChangedEvent()
    {
        // Arrange
        var service = new UserPreferencesService(_loggerMock.Object);
        UserPreferences? changedPreferences = null;
        service.PreferencesChanged += (sender, prefs) => changedPreferences = prefs;

        // Act
        service.SetHasSeenWelcomeScreen(true);

        // Assert
        Assert.NotNull(changedPreferences);
        Assert.True(changedPreferences.HasSeenWelcomeScreen);
    }

    [Fact]
    public void SetShowWelcomeOnStartup_RaisesPreferencesChangedEvent()
    {
        // Arrange
        var service = new UserPreferencesService(_loggerMock.Object);
        UserPreferences? changedPreferences = null;
        service.PreferencesChanged += (sender, prefs) => changedPreferences = prefs;

        // Act
        service.SetShowWelcomeOnStartup(false);

        // Assert
        Assert.NotNull(changedPreferences);
        Assert.False(changedPreferences.ShowWelcomeOnStartup);
    }

    [Fact]
    public void SetHasSeenWelcomeScreen_DoesNotRaiseEvent_WhenValueUnchanged()
    {
        // Arrange
        var service = new UserPreferencesService(_loggerMock.Object);
        service.SetHasSeenWelcomeScreen(false); // Set to default value
        
        var eventRaised = false;
        service.PreferencesChanged += (sender, prefs) => eventRaised = true;

        // Act
        service.SetHasSeenWelcomeScreen(false); // Set to same value

        // Assert
        Assert.False(eventRaised);
    }

    [Fact]
    public void SetShowWelcomeOnStartup_DoesNotRaiseEvent_WhenValueUnchanged()
    {
        // Arrange
        var service = new UserPreferencesService(_loggerMock.Object);
        service.SetShowWelcomeOnStartup(true); // Set to default value
        
        var eventRaised = false;
        service.PreferencesChanged += (sender, prefs) => eventRaised = true;

        // Act
        service.SetShowWelcomeOnStartup(true); // Set to same value

        // Assert
        Assert.False(eventRaised);
    }

    [Fact]
    public void GetPreferences_ReturnsDefensiveCopy()
    {
        // Arrange
        var service = new UserPreferencesService(_loggerMock.Object);
        
        // Act
        var preferences1 = service.GetPreferences();
        preferences1.HasSeenWelcomeScreen = true;
        
        var preferences2 = service.GetPreferences();

        // Assert - Changes to returned copy should not affect service state
        Assert.False(preferences2.HasSeenWelcomeScreen);
    }

    [Fact]
    public void FirstRunDate_RemainsConstant_AcrossMultipleLoads()
    {
        // Arrange
        var service1 = new UserPreferencesService(_loggerMock.Object);
        var firstRunDate1 = service1.GetFirstRunDate();

        // Act - Create new service instance to simulate app restart
        System.Threading.Thread.Sleep(100); // Small delay to ensure time difference
        var service2 = new UserPreferencesService(_loggerMock.Object);
        var firstRunDate2 = service2.GetFirstRunDate();

        // Assert - FirstRunDate should not change on subsequent loads
        Assert.Equal(firstRunDate1, firstRunDate2);
    }

    [Fact]
    public void AllNewProperties_AreIncludedInSavedPreferences()
    {
        // Arrange
        var service = new UserPreferencesService(_loggerMock.Object);
        
        // Act
        service.SetHasSeenWelcomeScreen(true);
        service.SetShowWelcomeOnStartup(false);
        
        // Read the saved JSON file
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var watchTowerPath = Path.Combine(appDataPath, "WatchTower");
        var preferencesPath = Path.Combine(watchTowerPath, "user-preferences.json");
        var json = File.ReadAllText(preferencesPath);

        // Assert - All new properties should be in the JSON
        Assert.Contains("hasSeenWelcomeScreen", json);
        Assert.Contains("showWelcomeOnStartup", json);
        Assert.Contains("firstRunDate", json);
    }

    [Fact]
    public void SavePreferences_PreservesOriginalFirstRunDate()
    {
        // Arrange
        var service = new UserPreferencesService(_loggerMock.Object);
        var originalFirstRunDate = service.GetFirstRunDate();
        Assert.NotNull(originalFirstRunDate);

        // Act - Try to save preferences with a different FirstRunDate
        var preferences = service.GetPreferences();
        var newDate = DateTime.UtcNow.AddDays(10);
        preferences.FirstRunDate = newDate;
        service.SavePreferences(preferences);

        // Assert - FirstRunDate should not have changed
        var currentFirstRunDate = service.GetFirstRunDate();
        Assert.Equal(originalFirstRunDate, currentFirstRunDate);
        Assert.NotEqual(newDate, currentFirstRunDate);
    }

    [Fact]
    public void SavePreferences_DoesNotMutateInputParameter()
    {
        // Arrange
        var service = new UserPreferencesService(_loggerMock.Object);
        var originalFirstRunDate = service.GetFirstRunDate();
        
        // Act - Try to save preferences with a different FirstRunDate
        var preferences = service.GetPreferences();
        var attemptedDate = DateTime.UtcNow.AddDays(10);
        preferences.FirstRunDate = attemptedDate;
        service.SavePreferences(preferences);

        // Assert - Input parameter should not be mutated by the service
        Assert.Equal(attemptedDate, preferences.FirstRunDate);
        Assert.NotEqual(originalFirstRunDate, preferences.FirstRunDate);
    }

    // ==================== Window Position Tests ====================

    [Fact]
    public void GetWindowPosition_WhenNoPreferencesSaved_ReturnsNull()
    {
        // Arrange
        var service = new UserPreferencesService(_loggerMock.Object);

        // Act
        var result = service.GetWindowPosition();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SetWindowPosition_SavesAndRetrievesPosition()
    {
        // Arrange
        var service = new UserPreferencesService(_loggerMock.Object);
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
        service.SetWindowPosition(windowPosition);
        var retrievedPosition = service.GetWindowPosition();

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
        var service = new UserPreferencesService(_loggerMock.Object);
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
        service.SetWindowPosition(windowPosition);

        // Act
        service.SetWindowPosition(null);
        var retrievedPosition = service.GetWindowPosition();

        // Assert
        Assert.Null(retrievedPosition);
    }

    [Fact]
    public void SetWindowPosition_PersistsAcrossInstances()
    {
        // Arrange
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
        var service1 = new UserPreferencesService(_loggerMock.Object);
        service1.SetWindowPosition(windowPosition);

        // Act - Load with second instance
        var service2 = new UserPreferencesService(_loggerMock.Object);
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
        var service = new UserPreferencesService(_loggerMock.Object);
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
        service.SetWindowPosition(windowPosition);

        // Act
        var retrievedPosition1 = service.GetWindowPosition();
        var retrievedPosition2 = service.GetWindowPosition();

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
