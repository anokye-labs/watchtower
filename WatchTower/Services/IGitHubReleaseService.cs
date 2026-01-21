using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WatchTower.Services;

/// <summary>
/// Service for retrieving GitHub releases and pull request builds.
/// </summary>
public interface IGitHubReleaseService
{
    /// <summary>
    /// Validates a GitHub authentication token.
    /// </summary>
    Task<bool> ValidateTokenAsync(string token);
    
    /// <summary>
    /// Sets the authentication token for GitHub API calls.
    /// </summary>
    void SetAuthToken(string token);
    
    /// <summary>
    /// Retrieves the list of published releases.
    /// </summary>
    Task<IReadOnlyList<ReleaseInfo>> GetReleasesAsync();
    
    /// <summary>
    /// Retrieves the list of pull request builds (requires authentication).
    /// </summary>
    Task<IReadOnlyList<PullRequestBuildInfo>> GetPullRequestBuildsAsync();
}

/// <summary>
/// Information about a GitHub release.
/// </summary>
public record ReleaseInfo
{
    public required string TagName { get; init; }
    public required string Name { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string AssetDownloadUrl { get; init; }
}

/// <summary>
/// Information about a pull request build.
/// </summary>
public record PullRequestBuildInfo
{
    public required int PullRequestNumber { get; init; }
    public required string Title { get; init; }
    public required string Author { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string ArtifactDownloadUrl { get; init; }
}
