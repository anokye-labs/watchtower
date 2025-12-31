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
        // Clean up preferences file after each test
        if (File.Exists(_preferencesFilePath))
        {
            File.Delete(_preferencesFilePath);
        }
    }

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
        
        // Modify first copy
        retrievedPosition1.X = 999.0;
        
        // Second copy should be unchanged
        Assert.Equal(100.0, retrievedPosition2.X);
    }
}
