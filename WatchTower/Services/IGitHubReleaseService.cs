using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WatchTower.Models;

namespace WatchTower.Services;

/// <summary>
/// Service interface for accessing GitHub releases and pull request build artifacts.
/// </summary>
public interface IGitHubReleaseService
{
    /// <summary>
    /// Gets a list of releases from the configured repository.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A readonly list of release information.</returns>
    Task<IReadOnlyList<ReleaseInfo>> GetReleasesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a list of pull request build artifacts from the configured repository.
    /// Requires authentication.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A readonly list of pull request build information.</returns>
    Task<IReadOnlyList<PullRequestBuildInfo>> GetPullRequestBuildsAsync(CancellationToken ct = default);

    /// <summary>
    /// Downloads an asset from the specified URL with optional progress reporting.
    /// </summary>
    /// <param name="downloadUrl">The URL of the asset to download.</param>
    /// <param name="progress">Optional progress reporter (0.0 to 1.0).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A stream containing the downloaded asset.</returns>
    Task<Stream> DownloadAssetAsync(string downloadUrl, IProgress<double>? progress, CancellationToken ct = default);

    /// <summary>
    /// Sets the authentication token for accessing private resources.
    /// </summary>
    /// <param name="token">The GitHub personal access token, or null to clear authentication.</param>
    void SetAuthToken(string? token);

    /// <summary>
    /// Gets a value indicating whether the service is currently authenticated.
    /// </summary>
    bool IsAuthenticated { get; }
}
