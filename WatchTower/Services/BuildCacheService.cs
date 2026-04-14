using System.Collections.Generic;
using System.Threading.Tasks;
using WatchTower.Models;

namespace WatchTower.Services;

/// <summary>
/// PLACEHOLDER implementation of <see cref="IBuildCacheService"/>.
/// This is a minimal stub to allow the application to compile.
/// The full implementation is tracked in issue #218.
/// </summary>
public class BuildCacheService : IBuildCacheService
{
    /// <inheritdoc/>
    public bool HasCachedBuilds => false;

    /// <inheritdoc/>
    public string CacheSize => "0 MB";

    /// <inheritdoc/>
    public Task<IReadOnlyList<BuildInfo>> GetCachedBuildsAsync()
    {
        return Task.FromResult<IReadOnlyList<BuildInfo>>(new List<BuildInfo>());
    }

    /// <inheritdoc/>
    public Task ClearCacheAsync()
    {
        return Task.CompletedTask;
    }
}
