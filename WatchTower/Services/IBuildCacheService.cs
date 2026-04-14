using System.Collections.Generic;
using System.Threading.Tasks;
using WatchTower.Models;

namespace WatchTower.Services;

/// <summary>
/// PLACEHOLDER interface for build cache service.
/// This is a minimal stub to allow the application to compile.
/// The full implementation is tracked in issue #218.
/// </summary>
public interface IBuildCacheService
{
    /// <summary>
    /// Gets a value indicating whether there are any cached builds.
    /// </summary>
    bool HasCachedBuilds { get; }

    /// <summary>
    /// Gets the size of the build cache as a human-readable string.
    /// </summary>
    string CacheSize { get; }

    /// <summary>
    /// Retrieves all cached builds.
    /// </summary>
    Task<IReadOnlyList<BuildInfo>> GetCachedBuildsAsync();

    /// <summary>
    /// Clears the build cache.
    /// </summary>
    Task ClearCacheAsync();
}
