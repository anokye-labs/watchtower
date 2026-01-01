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

    /// <summary>
    /// Reports detailed progress with current step, total steps, and description.
    /// </summary>
    /// <param name="currentStep">The current step number (1-based).</param>
    /// <param name="totalSteps">The total number of steps.</param>
    /// <param name="description">Description of the current step.</param>
    void ReportProgress(int currentStep, int totalSteps, string description);
}
