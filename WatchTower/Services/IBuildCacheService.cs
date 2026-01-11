using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WatchTower.Models;

namespace WatchTower.Services;

/// <summary>
/// Service for managing build downloads and local caching.
/// </summary>
public interface IBuildCacheService
{
    /// <summary>
    /// Gets the local path to a cached build if it exists.
    /// </summary>
    /// <param name="buildId">Unique identifier for the build.</param>
    /// <returns>Path to the executable if cached, otherwise null.</returns>
    Task<string?> GetCachedBuildPathAsync(string buildId);

    /// <summary>
    /// Checks if a build is already cached locally.
    /// </summary>
    /// <param name="buildId">Unique identifier for the build.</param>
    /// <returns>True if the build is cached, otherwise false.</returns>
    Task<bool> IsBuildCachedAsync(string buildId);

    /// <summary>
    /// Downloads a build and caches it locally.
    /// </summary>
    /// <param name="buildId">Unique identifier for the build.</param>
    /// <param name="downloadUrl">URL to download the build ZIP file from.</param>
    /// <param name="progress">Optional progress reporter for download updates.</param>
    /// <param name="ct">Cancellation token for the download operation.</param>
    /// <returns>Path to the downloaded executable.</returns>
    Task<string> DownloadAndCacheBuildAsync(
        string buildId,
        string downloadUrl,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Removes all cached builds.
    /// </summary>
    Task ClearCacheAsync();

    /// <summary>
    /// Removes cached builds older than the specified age.
    /// </summary>
    /// <param name="maxAge">Maximum age for cached builds.</param>
    Task CleanOldBuildsAsync(TimeSpan maxAge);

    /// <summary>
    /// Gets the total size of all cached builds in bytes.
    /// </summary>
    /// <returns>Total cache size in bytes.</returns>
    long GetCacheSizeBytes();

    /// <summary>
    /// Gets a list of all cached builds.
    /// </summary>
    /// <returns>Read-only list of cached build metadata.</returns>
    IReadOnlyList<BuildInfo> GetCachedBuilds();
}
