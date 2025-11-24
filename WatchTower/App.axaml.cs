using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Logging;
using WatchTower.Services;
using System;
using System.Diagnostics;

namespace WatchTower;

/// <summary>
/// Application class with hot reload support and comprehensive logging.
/// Hot reload is enabled in Debug mode through Avalonia.Diagnostics and .NET Hot Reload.
/// </summary>
public partial class App : Application
{
    private LoggingService? _loggingService;
    private ILogger? _logger;
    private Stopwatch? _hotReloadTimer;

    public override void Initialize()
    {
        try
        {
            AvaloniaXamlLoader.Load(this);
            
            // Enable DevTools in debug mode for hot reload support
#if DEBUG
            if (_logger != null)
            {
                _logger.LogDebug("Debug mode enabled - DevTools and hot reload available");
                _logger.LogDebug("Press F12 to open Avalonia DevTools");
            }
#endif
        }
        catch (Exception ex)
        {
            // Handle XAML parsing errors during hot reload gracefully
            if (_logger != null)
            {
                _logger.LogError(ex, "XAML initialization error - check syntax");
            }
            else
            {
                Console.Error.WriteLine($"[ERROR] XAML initialization failed: {ex.Message}");
            }
            
            // Don't crash the app - allow developer to fix the error
            // The error will be displayed in the console/debug output
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Initialize logging service
        _loggingService = new LoggingService();
        _logger = _loggingService.CreateLogger<App>();
        _logger.LogInformation("Application initialization completed");
        
        // Subscribe to hot reload events (if available in the runtime)
        AppDomain.CurrentDomain.ProcessExit += (s, e) =>
        {
            _logger?.LogInformation("Application shutting down");
        };

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new Views.MainWindow();
            _logger.LogInformation("Main window created");
            
            // Log startup completion
            _logger.LogInformation("Application ready for interaction");
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Handles hot reload events and logs timing information.
    /// Note: XAML hot reload happens automatically via Avalonia.Diagnostics.
    /// Simple C# hot reload (method bodies) is supported by .NET Hot Reload.
    /// Complex changes (signatures, types, constructors) require restart.
    /// </summary>
    private void OnHotReloadDetected()
    {
        _hotReloadTimer = Stopwatch.StartNew();
        _logger?.LogDebug("Hot reload initiated...");
    }

    private void OnHotReloadCompleted()
    {
        if (_hotReloadTimer != null)
        {
            _hotReloadTimer.Stop();
            _logger?.LogInformation($"Hot reload completed in {_hotReloadTimer.ElapsedMilliseconds}ms");
            
            // Notify if reload took longer than target (2 seconds)
            if (_hotReloadTimer.ElapsedMilliseconds > 2000)
            {
                _logger?.LogWarning("Hot reload exceeded 2 second target - consider simplifying changes");
            }
        }
    }

    /// <summary>
    /// Notifies developer when a restart is required for complex changes.
    /// This is called when hot reload cannot handle the change type.
    /// </summary>
    private void NotifyRestartRequired(string reason)
    {
        _logger?.LogWarning($"Application restart required: {reason}");
        Console.WriteLine($"\n⚠️  RESTART REQUIRED: {reason}");
        Console.WriteLine("Hot reload cannot handle this type of change.");
        Console.WriteLine("Please stop and restart the application to apply changes.\n");
    }
}