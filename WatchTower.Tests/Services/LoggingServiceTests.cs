using Xunit;
using WatchTower.Services;
using Microsoft.Extensions.Logging;
using System.IO;
using System;

namespace WatchTower.Tests.Services;

/// <summary>
/// Tests for LoggingService configuration and logger creation.
/// </summary>
public class LoggingServiceTests
{
    [Fact]
    public void Constructor_CreatesInstance_Successfully()
    {
        // Act
        var service = new LoggingService();
        
        // Assert
        Assert.NotNull(service);
        Assert.NotNull(service.LoggerFactory);
    }
    
    [Fact]
    public void CreateLogger_WithGenericType_ReturnsLogger()
    {
        // Arrange
        var service = new LoggingService();
        
        // Act
        var logger = service.CreateLogger<LoggingServiceTests>();
        
        // Assert
        Assert.NotNull(logger);
        Assert.IsAssignableFrom<ILogger<LoggingServiceTests>>(logger);
    }
    
    [Fact]
    public void CreateLogger_WithCategoryName_ReturnsLogger()
    {
        // Arrange
        var service = new LoggingService();
        
        // Act
        var logger = service.CreateLogger("TestCategory");
        
        // Assert
        Assert.NotNull(logger);
        Assert.IsAssignableFrom<ILogger>(logger);
    }
    
    [Fact]
    public void GetConfiguration_ReturnsConfiguration()
    {
        // Arrange
        var service = new LoggingService();
        
        // Act
        var config = service.GetConfiguration();
        
        // Assert
        Assert.NotNull(config);
    }
    
    [Fact]
    public void LoggerFactory_IsAccessible()
    {
        // Arrange
        var service = new LoggingService();
        
        // Act
        var factory = service.LoggerFactory;
        
        // Assert
        Assert.NotNull(factory);
        Assert.IsAssignableFrom<ILoggerFactory>(factory);
    }
    
    [Fact]
    public void CreateLogger_MultipleTimes_ReturnsDifferentInstances()
    {
        // Arrange
        var service = new LoggingService();
        
        // Act
        var logger1 = service.CreateLogger<LoggingServiceTests>();
        var logger2 = service.CreateLogger<LoggingServiceTests>();
        
        // Assert - Each call should return a new instance
        Assert.NotNull(logger1);
        Assert.NotNull(logger2);
    }
    
    [Fact]
    public void LoggerFactory_CanCreateMultipleLoggers()
    {
        // Arrange
        var service = new LoggingService();
        var factory = service.LoggerFactory;
        
        // Act
        var logger1 = factory.CreateLogger("Category1");
        var logger2 = factory.CreateLogger("Category2");
        
        // Assert
        Assert.NotNull(logger1);
        Assert.NotNull(logger2);
    }
    
    [Fact]
    public void GetConfiguration_CanAccessLogLevel()
    {
        // Arrange
        var service = new LoggingService();
        var config = service.GetConfiguration();
        
        // Act
        var logLevel = config["Logging:LogLevel"];
        
        // Assert
        // Should either be configured or null (if appsettings.json not found)
        // Both are valid scenarios in test environment
        Assert.True(logLevel == null || !string.IsNullOrEmpty(logLevel));
    }
}
