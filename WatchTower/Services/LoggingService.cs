using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace WatchTower.Services;

/// <summary>
/// Provides configurable logging functionality with support for minimal, normal, and verbose levels.
/// Configuration is loaded from appsettings.json.
/// </summary>
public class LoggingService
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IConfiguration _configuration;

    public LoggingService()
    {
        // Load configuration from appsettings.json
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        // Create logger factory with configured log level
        var logLevel = GetConfiguredLogLevel();
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(logLevel)
                .AddConsole();
        });
    }

    /// <summary>
    /// Creates a logger for the specified type.
    /// </summary>
    public ILogger<T> CreateLogger<T>()
    {
        return _loggerFactory.CreateLogger<T>();
    }

    /// <summary>
    /// Creates a logger with the specified category name.
    /// </summary>
    public ILogger CreateLogger(string categoryName)
    {
        return _loggerFactory.CreateLogger(categoryName);
    }

    /// <summary>
    /// Gets the configuration instance.
    /// </summary>
    public IConfiguration GetConfiguration()
    {
        return _configuration;
    }

    /// <summary>
    /// Gets the configured log level from appsettings.json.
    /// Defaults to Information (normal) if not configured.
    /// </summary>
    private LogLevel GetConfiguredLogLevel()
    {
        var logLevelString = _configuration["Logging:LogLevel"] ?? "normal";
        
        return logLevelString.ToLowerInvariant() switch
        {
            "minimal" => LogLevel.Warning,      // Only warnings and errors
            "normal" => LogLevel.Information,   // Info, warnings, and errors (default)
            "verbose" => LogLevel.Debug,        // All messages including debug
            _ => LogLevel.Information
        };
    }
}
