using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WatchTower.Tests.TestHelpers;

/// <summary>
/// Provides test fixtures and common test setup utilities.
/// </summary>
public static class TestFixtures
{
    /// <summary>
    /// Creates a test configuration with default values.
    /// </summary>
    public static Dictionary<string, string?> CreateTestConfiguration()
    {
        return new Dictionary<string, string?>
        {
            ["Logging:LogLevel"] = "normal",
            ["Gamepad:DeadZone"] = "0.15",
            ["Startup:HangThresholdSeconds"] = "30",
            ["Voice:Mode"] = "offline"
        };
    }
}

/// <summary>
/// Base class for ViewModel tests with common setup.
/// </summary>
public abstract class ViewModelTestBase : IDisposable
{
    protected ILoggerFactory LoggerFactory { get; }
    
    protected ViewModelTestBase()
    {
        // Create a logger factory for tests
        LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddConsole();
        });
    }
    
    protected ILogger<T> CreateLogger<T>()
    {
        return LoggerFactory.CreateLogger<T>();
    }
    
    public virtual void Dispose()
    {
        LoggerFactory?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Base class for Service tests with common setup including configuration.
/// </summary>
public abstract class ServiceTestBase : IDisposable
{
    protected ILoggerFactory LoggerFactory { get; }
    protected IConfiguration Configuration { get; }
    protected string TempDirectory { get; }
    
    protected ServiceTestBase()
    {
        // Create a logger factory for tests
        LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddConsole();
        });
        
        // Create a test configuration
        Configuration = CreateTestConfiguration();
        
        // Create a temporary directory for file-based tests
        TempDirectory = TestUtilities.CreateTempDirectory();
    }
    
    protected ILogger<T> CreateLogger<T>()
    {
        return LoggerFactory.CreateLogger<T>();
    }
    
    protected virtual IConfiguration CreateTestConfiguration()
    {
        var configData = TestFixtures.CreateTestConfiguration();
        
        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();
    }
    
    public virtual void Dispose()
    {
        LoggerFactory?.Dispose();
        
        // Clean up temp directory
        if (Directory.Exists(TempDirectory))
        {
            try
            {
                Directory.Delete(TempDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
        
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Fixture for tests that need to verify event subscriptions and memory leaks.
/// </summary>
public class EventSubscriptionTestFixture : IDisposable
{
    private readonly List<WeakReference> _weakReferences = new();
    
    /// <summary>
    /// Tracks an object for memory leak detection.
    /// </summary>
    public void Track(object obj)
    {
        _weakReferences.Add(new WeakReference(obj));
    }
    
    /// <summary>
    /// Verifies that tracked objects have been garbage collected.
    /// This helps detect memory leaks from event subscription issues.
    /// </summary>
    public void VerifyCollected()
    {
        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        // Check if objects were collected
        var aliveCount = _weakReferences.Count(wr => wr.IsAlive);
        if (aliveCount > 0)
        {
            throw new InvalidOperationException(
                $"Memory leak detected: {aliveCount} of {_weakReferences.Count} tracked objects are still alive");
        }
    }
    
    public void Dispose()
    {
        _weakReferences.Clear();
        GC.SuppressFinalize(this);
    }
}
