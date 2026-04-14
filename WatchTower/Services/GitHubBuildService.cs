using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using WatchTower.Models;

namespace WatchTower.Services;

/// <summary>
/// PLACEHOLDER implementation of <see cref="IGitHubBuildService"/>.
/// This is a minimal stub to allow the application to compile.
/// The full implementation is tracked in issue #218.
/// </summary>
public class GitHubBuildService : IGitHubBuildService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public GitHubBuildService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc/>
    public bool IsAuthenticated => false;

    /// <inheritdoc/>
    public string? AuthenticatedUser => null;

    /// <inheritdoc/>
    public Task<bool> AuthenticateAsync(string token)
    {
        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<BuildInfo>> GetBuildsAsync()
    {
        return Task.FromResult<IReadOnlyList<BuildInfo>>(new List<BuildInfo>());
    }
}
