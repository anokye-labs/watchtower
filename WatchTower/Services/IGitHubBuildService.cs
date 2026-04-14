using System.Collections.Generic;
using System.Threading.Tasks;
using WatchTower.Models;

namespace WatchTower.Services;

/// <summary>
/// PLACEHOLDER interface for GitHub build service.
/// This is a minimal stub to allow the application to compile.
/// The full implementation is tracked in issue #218.
/// </summary>
public interface IGitHubBuildService
{
    /// <summary>
    /// Gets a value indicating whether the user is authenticated with GitHub.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the currently authenticated GitHub user name, or null if not authenticated.
    /// </summary>
    string? AuthenticatedUser { get; }

    /// <summary>
    /// Authenticates with GitHub using the provided personal access token.
    /// </summary>
    Task<bool> AuthenticateAsync(string token);

    /// <summary>
    /// Retrieves available builds from GitHub.
    /// </summary>
    Task<IReadOnlyList<BuildInfo>> GetBuildsAsync();
}
