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
        
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var watchTowerPath = Path.Combine(appDataPath, "WatchTower");
        _preferencesPath = Path.Combine(watchTowerPath, "user-preferences.json");
        
        ResetToDefaults();
    }

    public void Dispose()
    {
        ResetToDefaults();
    }

    private void ResetToDefaults()
    {
        var defaultPreferences = new UserPreferences
        {
            IsFirstRun = true,
            HasSeenWelcomeScreen = false,
            WelcomeScreenDismissedDate = null,
            ShowWelcomeOnStartup = true,
            ThemeMode = ThemeMode.System,
            FontOverrides = null,
            WindowPosition = null
        };
        _service.SavePreferences(defaultPreferences);
    }

    [Fact]
    public void IsFirstRun_AfterReset_ReturnsTrue()
    {
        ResetToDefaults();
        var isFirstRun = _service.IsFirstRun();
        Assert.True(isFirstRun);
    }

    [Fact]
    public void MarkFirstRunComplete_SetsIsFirstRunToFalse()
    {
        ResetToDefaults();
        Assert.True(_service.IsFirstRun());
        _service.MarkFirstRunComplete();
        Assert.False(_service.IsFirstRun());
    }

    [Fact]
    public void MarkFirstRunComplete_RaisesPreferencesChangedEvent()
    {
        ResetToDefaults();
        var eventRaised = false;
        _service.PreferencesChanged += (s, e) => eventRaised = true;
        _service.MarkFirstRunComplete();
        Assert.True(eventRaised);
    }

    [Fact]
    public void MarkFirstRunComplete_WhenAlreadyComplete_DoesNotRaiseEvent()
    {
        ResetToDefaults();
        _service.MarkFirstRunComplete();
        var eventRaised = false;
        _service.PreferencesChanged += (s, e) => eventRaised = true;
        _service.MarkFirstRunComplete();
        Assert.False(eventRaised);
    }

    [Fact]
    public void HasSeenWelcomeScreen_AfterReset_ReturnsFalse()
    {
        ResetToDefaults();
        var hasSeen = _service.HasSeenWelcomeScreen();
        Assert.False(hasSeen);
    }

    [Fact]
    public void MarkWelcomeScreenSeen_SetsHasSeenWelcomeScreenToTrue()
    {
        ResetToDefaults();
        Assert.False(_service.HasSeenWelcomeScreen());
        _service.MarkWelcomeScreenSeen();
        Assert.True(_service.HasSeenWelcomeScreen());
    }

    [Fact]
    public void MarkWelcomeScreenSeen_SetsDismissedDate()
    {
        ResetToDefaults();
        var beforeCall = DateTime.UtcNow;
        _service.MarkWelcomeScreenSeen();
        var preferences = _service.GetPreferences();
        Assert.NotNull(preferences.WelcomeScreenDismissedDate);
        Assert.True(preferences.WelcomeScreenDismissedDate >= beforeCall);
        Assert.True(preferences.WelcomeScreenDismissedDate <= DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void MarkWelcomeScreenSeen_RaisesPreferencesChangedEvent()
    {
        ResetToDefaults();
        var eventRaised = false;
        _service.PreferencesChanged += (s, e) => eventRaised = true;
        _service.MarkWelcomeScreenSeen();
        Assert.True(eventRaised);
    }

    [Fact]
    public void MarkWelcomeScreenSeen_WhenAlreadySeen_DoesNotRaiseEvent()
    {
        ResetToDefaults();
        _service.MarkWelcomeScreenSeen();
        var eventRaised = false;
        _service.PreferencesChanged += (s, e) => eventRaised = true;
        _service.MarkWelcomeScreenSeen();
        Assert.False(eventRaised);
    }

    [Fact]
    public void GetPreferences_AfterReset_ReturnsDefaultValues()
    {
        ResetToDefaults();
        var preferences = _service.GetPreferences();
        Assert.NotNull(preferences);
        Assert.True(preferences.IsFirstRun);
        Assert.False(preferences.HasSeenWelcomeScreen);
        Assert.Null(preferences.WelcomeScreenDismissedDate);
        Assert.Equal(ThemeMode.System, preferences.ThemeMode);
    }

    [Fact]
    public void SavePreferences_PersistsWelcomeScreenState()
    {
        ResetToDefaults();
        var preferences = _service.GetPreferences();
        preferences.IsFirstRun = false;
        preferences.HasSeenWelcomeScreen = true;
        preferences.WelcomeScreenDismissedDate = DateTime.UtcNow;
        _service.SavePreferences(preferences);
        var newService = new UserPreferencesService(_mockLogger.Object);
        var loadedPreferences = newService.GetPreferences();
        Assert.False(loadedPreferences.IsFirstRun);
        Assert.True(loadedPreferences.HasSeenWelcomeScreen);
        Assert.NotNull(loadedPreferences.WelcomeScreenDismissedDate);
    }

    [Fact]
    public void Constructor_CreatesDefaultPreferences_WhenNoFileExists()
    {
        var service = new UserPreferencesService(_mockLogger.Object);
        var preferences = service.GetPreferences();
        Assert.NotNull(preferences);
        Assert.Equal(ThemeMode.System, preferences.ThemeMode);
        Assert.NotNull(preferences.FirstRunDate);
    }

    [Fact]
    public void Constructor_SetsFirstRunDate_WhenNewInstallation()
    {
        var beforeCreation = DateTime.UtcNow.AddSeconds(-1);
        var service = new UserPreferencesService(_mockLogger.Object);
        var firstRunDate = service.GetFirstRunDate();
        var afterCreation = DateTime.UtcNow.AddSeconds(1);
        Assert.NotNull(firstRunDate);
        Assert.True(firstRunDate.Value >= beforeCreation);
        Assert.True(firstRunDate.Value <= afterCreation);
    }

    [Fact]
    public void GetHasSeenWelcomeScreen_ReturnsDefault_WhenNewInstallation()
    {
        ResetToDefaults();
        var service = new UserPreferencesService(_mockLogger.Object);
        var hasSeenWelcomeScreen = service.GetHasSeenWelcomeScreen();
        Assert.False(hasSeenWelcomeScreen);
    }

    [Fact]
    public void SetHasSeenWelcomeScreen_UpdatesValue_AndPersists()
    {
        ResetToDefaults();
        var service = new UserPreferencesService(_mockLogger.Object);
        service.SetHasSeenWelcomeScreen(true);
        var result = service.GetHasSeenWelcomeScreen();
        Assert.True(result);
    }

    [Fact]
    public void GetShowWelcomeOnStartup_ReturnsDefault_WhenNewInstallation()
    {
        ResetToDefaults();
        var service = new UserPreferencesService(_mockLogger.Object);
        var showWelcomeOnStartup = service.GetShowWelcomeOnStartup();
        Assert.True(showWelcomeOnStartup);
    }

    [Fact]
    public void SetShowWelcomeOnStartup_UpdatesValue_AndPersists()
    {
        ResetToDefaults();
        var service = new UserPreferencesService(_mockLogger.Object);
        service.SetShowWelcomeOnStartup(false);
        var result = service.GetShowWelcomeOnStartup();
        Assert.False(result);
    }

    [Fact]
    public void GetWindowPosition_WhenNoPreferencesSaved_ReturnsNull()
    {
        ResetToDefaults();
        var result = _service.GetWindowPosition();
        Assert.Null(result);
    }

    [Fact]
    public void SetWindowPosition_SavesAndRetrievesPosition()
    {
        ResetToDefaults();
        var windowPosition = new WindowPositionPreferences
        {
            X = 100.0,
            Y = 200.0,
            Width = 800.0,
            Height = 600.0,
            DisplayBounds = new DisplayBounds { X = 0, Y = 0, Width = 1920, Height = 1080 }
        };
        _service.SetWindowPosition(windowPosition);
        var retrievedPosition = _service.GetWindowPosition();
        Assert.NotNull(retrievedPosition);
        Assert.Equal(100.0, retrievedPosition.X);
        Assert.Equal(200.0, retrievedPosition.Y);
        Assert.Equal(800.0, retrievedPosition.Width);
        Assert.Equal(600.0, retrievedPosition.Height);
    }

    [Fact]
    public void SetWindowPosition_WithNull_ClearsPosition()
    {
        ResetToDefaults();
        var windowPosition = new WindowPositionPreferences
        {
            X = 100.0,
            Y = 200.0,
            Width = 800.0,
            Height = 600.0,
            DisplayBounds = new DisplayBounds { X = 0, Y = 0, Width = 1920, Height = 1080 }
        };
        _service.SetWindowPosition(windowPosition);
        _service.SetWindowPosition(null);
        var retrievedPosition = _service.GetWindowPosition();
        Assert.Null(retrievedPosition);
    }

    [Fact]
    public void SetWindowPosition_PersistsAcrossInstances()
    {
        ResetToDefaults();
        var windowPosition = new WindowPositionPreferences
        {
            X = 150.0,
            Y = 250.0,
            Width = 1024.0,
            Height = 768.0,
            DisplayBounds = new DisplayBounds { X = 1920, Y = 0, Width = 1920, Height = 1080 }
        };
        _service.SetWindowPosition(windowPosition);
        var service2 = new UserPreferencesService(_mockLogger.Object);
        var retrievedPosition = service2.GetWindowPosition();
        Assert.NotNull(retrievedPosition);
        Assert.Equal(150.0, retrievedPosition.X);
        Assert.Equal(250.0, retrievedPosition.Y);
    }

    [Fact]
    public void GetWindowPosition_ReturnsDefensiveCopy()
    {
        ResetToDefaults();
        var windowPosition = new WindowPositionPreferences
        {
            X = 100.0,
            Y = 200.0,
            Width = 800.0,
            Height = 600.0,
            DisplayBounds = new DisplayBounds { X = 0, Y = 0, Width = 1920, Height = 1080 }
        };
        _service.SetWindowPosition(windowPosition);
        var retrievedPosition1 = _service.GetWindowPosition();
        var retrievedPosition2 = _service.GetWindowPosition();
        Assert.NotNull(retrievedPosition1);
        Assert.NotNull(retrievedPosition2);
        Assert.NotSame(retrievedPosition1, retrievedPosition2);
        retrievedPosition1.X = 999.0;
        Assert.Equal(100.0, retrievedPosition2.X);
    }
}
