using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WatchTower.Services;

/// <summary>
/// Interface for orchestrating application startup.
/// </summary>
public interface IStartupOrchestrator
{
    /// <summary>
    /// Executes the startup workflow asynchronously.
    /// </summary>
    /// <param name="logger">Logger for reporting progress and errors.</param>
    /// <param name="configuration">Application configuration to use.</param>
    /// <returns>The configured ServiceProvider on success, null on failure.</returns>
    Task<ServiceProvider?> ExecuteStartupAsync(IStartupLogger logger, IConfiguration configuration);

    /// <summary>
    /// Executes the startup workflow asynchronously with an optional pre-created UserPreferencesService.
    /// </summary>
    /// <param name="logger">Logger for reporting progress and errors.</param>
    /// <param name="configuration">Application configuration to use.</param>
    /// <param name="userPreferencesService">Optional pre-created user preferences service (e.g., for early window positioning).</param>
    /// <returns>The configured ServiceProvider on success, null on failure.</returns>
    Task<ServiceProvider?> ExecuteStartupAsync(
        IStartupLogger logger,
        IConfiguration configuration,
        IUserPreferencesService? userPreferencesService);
}
