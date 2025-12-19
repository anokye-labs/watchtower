using System;

namespace WatchTower.Services;

/// <summary>
/// Interface for logging startup progress and diagnostics.
/// Used to report initialization status to the splash screen.
/// </summary>
public interface IStartupLogger
{
    /// <summary>
    /// Logs an informational message about startup progress.
    /// </summary>
    void Info(string message);

    /// <summary>
    /// Logs a warning message during startup.
    /// </summary>
    void Warn(string message);

    /// <summary>
    /// Logs an error message with optional exception details.
    /// </summary>
    void Error(string message, Exception? ex = null);
}
