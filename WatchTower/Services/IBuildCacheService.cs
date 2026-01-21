using System;
using System.Threading;
using System.Threading.Tasks;

namespace WatchTower.Services;

/// <summary>
/// Service for caching and managing downloaded builds.
/// </summary>
public interface IBuildCacheService
{
    /// <summary>
    /// Checks if a build is already cached locally.
    /// </summary>
    Task<bool> IsBuildCachedAsync(string buildId);
    
    /// <summary>
    /// Gets the local path to a cached build executable.
    /// </summary>
    /// <returns>The path if cached, or null if not found.</returns>
    Task<string?> GetCachedBuildPathAsync(string buildId);
    
    /// <summary>
    /// Downloads a build from the specified URL and caches it locally.
    /// </summary>
    /// <param name="buildId">Unique identifier for the build.</param>
    /// <param name="downloadUrl">URL to download from.</param>
    /// <param name="progress">Progress reporter for download status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Path to the cached executable.</returns>
    Task<string> DownloadAndCacheBuildAsync(
        string buildId,
        string downloadUrl,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken);
    
    /// <summary>
    /// Clears all cached builds.
    /// </summary>
    Task ClearAllCacheAsync();
}

/// <summary>
/// Represents download progress information.
/// </summary>
public record DownloadProgress
{
    public double PercentComplete { get; init; }
    public double BytesPerSecond { get; init; }
}
