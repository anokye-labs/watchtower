using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Octokit;
using WatchTower.Models;

namespace WatchTower.Services;

/// <summary>
/// Service implementation for accessing GitHub releases and pull request build artifacts.
/// </summary>
public class GitHubReleaseService : IGitHubReleaseService, IDisposable
{
    private readonly ILogger<GitHubReleaseService> _logger;
    private readonly IConfiguration _configuration;
    private readonly GitHubClient _client;
    private readonly HttpClient _httpClient;
    private readonly string _owner;
    private readonly string _repo;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private IReadOnlyList<ReleaseInfo>? _cachedReleases;
    private DateTime? _cacheExpiration;
    private const int CacheDurationMinutes = 5;

    public bool IsAuthenticated { get; private set; }

    public GitHubReleaseService(
        ILogger<GitHubReleaseService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClient = new HttpClient();

        // Load configuration
        _owner = _configuration.GetValue<string>("DevMenu:GitHubOwner") ?? "anokye-labs";
        _repo = _configuration.GetValue<string>("DevMenu:GitHubRepo") ?? "watchtower";

        // Initialize GitHub client
        _client = new GitHubClient(new ProductHeaderValue("WatchTower"));
        IsAuthenticated = false;

        _logger.LogInformation("GitHubReleaseService initialized for {Owner}/{Repo}", _owner, _repo);
    }

    public void SetAuthToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _client.Credentials = Credentials.Anonymous;
            IsAuthenticated = false;
            _logger.LogInformation("GitHub authentication cleared");
        }
        else
        {
            _client.Credentials = new Credentials(token);
            IsAuthenticated = true;
            _logger.LogInformation("GitHub authentication token set");
        }

        // Clear cache when authentication changes
        InvalidateCache();
    }

    public async Task<IReadOnlyList<ReleaseInfo>> GetReleasesAsync(CancellationToken ct = default)
    {
        // Check cache first
        await _cacheLock.WaitAsync(ct);
        try
        {
            if (_cachedReleases != null && _cacheExpiration.HasValue && DateTime.UtcNow < _cacheExpiration.Value)
            {
                _logger.LogDebug("Returning cached releases");
                return _cachedReleases;
            }
        }
        finally
        {
            _cacheLock.Release();
        }

        try
        {
            _logger.LogInformation("Fetching releases from GitHub for {Owner}/{Repo}", _owner, _repo);
            
            var releases = await _client.Repository.Release.GetAll(_owner, _repo);
            
            var releaseInfoList = releases
                .Where(r => r.Assets.Count > 0) // Only include releases with assets
                .Select(r =>
                {
                    // Get the first asset (or primary asset)
                    var asset = r.Assets.FirstOrDefault();
                    return asset != null
                        ? new ReleaseInfo(
                            r.TagName,
                            r.Name ?? r.TagName,
                            r.CreatedAt,
                            asset.BrowserDownloadUrl,
                            asset.Size,
                            r.Prerelease)
                        : null;
                })
                .Where(r => r != null)
                .Cast<ReleaseInfo>()
                .ToList();

            _logger.LogInformation("Successfully fetched {Count} releases", releaseInfoList.Count);

            // Update cache
            await _cacheLock.WaitAsync(ct);
            try
            {
                _cachedReleases = releaseInfoList;
                _cacheExpiration = DateTime.UtcNow.AddMinutes(CacheDurationMinutes);
            }
            finally
            {
                _cacheLock.Release();
            }

            return releaseInfoList;
        }
        catch (ApiException ex)
        {
            _logger.LogWarning(ex, "GitHub API error while fetching releases: {Message}", ex.Message);
            return Array.Empty<ReleaseInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error while fetching releases: {Message}", ex.Message);
            return Array.Empty<ReleaseInfo>();
        }
    }

    public async Task<IReadOnlyList<PullRequestBuildInfo>> GetPullRequestBuildsAsync(CancellationToken ct = default)
    {
        if (!IsAuthenticated)
        {
            _logger.LogWarning("Cannot fetch PR artifacts without authentication");
            return Array.Empty<PullRequestBuildInfo>();
        }

        try
        {
            _logger.LogInformation("Fetching PR artifacts from GitHub for {Owner}/{Repo}", _owner, _repo);

            // Get open pull requests
            var pullRequests = await _client.PullRequest.GetAllForRepository(
                _owner,
                _repo,
                new PullRequestRequest { State = ItemStateFilter.Open });

            var prBuilds = new List<PullRequestBuildInfo>();

            // For each open PR, get workflow run artifacts
            foreach (var pr in pullRequests)
            {
                try
                {
                    // Get workflow runs for this PR
                    var workflowRuns = await _client.Actions.Workflows.Runs.List(
                        _owner,
                        _repo,
                        new WorkflowRunsRequest
                        {
                            Branch = pr.Head.Ref
                        });

                    // Get artifacts from successful runs
                    foreach (var run in workflowRuns.WorkflowRuns.Where(r => r.Status == "completed" && r.Conclusion == "success"))
                    {
                        var artifacts = await _client.Actions.Artifacts.ListWorkflowArtifacts(_owner, _repo, run.Id);

                        foreach (var artifact in artifacts.Artifacts)
                        {
                            prBuilds.Add(new PullRequestBuildInfo(
                                pr.Number,
                                pr.Title,
                                pr.Head.Ref,
                                pr.User.Login,
                                artifact.CreatedAt,
                                artifact.Id,
                                artifact.ArchiveDownloadUrl,
                                artifact.SizeInBytes));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error fetching artifacts for PR #{Number}: {Message}", pr.Number, ex.Message);
                }
            }

            _logger.LogInformation("Successfully fetched {Count} PR build artifacts", prBuilds.Count);
            return prBuilds;
        }
        catch (ApiException ex)
        {
            _logger.LogWarning(ex, "GitHub API error while fetching PR artifacts: {Message}", ex.Message);
            return Array.Empty<PullRequestBuildInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error while fetching PR artifacts: {Message}", ex.Message);
            return Array.Empty<PullRequestBuildInfo>();
        }
    }

    public async Task<Stream> DownloadAssetAsync(string downloadUrl, IProgress<double>? progress, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Downloading asset from {Url}", downloadUrl);

            var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            var memoryStream = new MemoryStream();
            
            await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
            
            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
            {
                await memoryStream.WriteAsync(buffer, 0, bytesRead, ct);
                totalRead += bytesRead;

                if (totalBytes > 0 && progress != null)
                {
                    var progressPercentage = (double)totalRead / totalBytes;
                    progress.Report(progressPercentage);
                }
            }

            memoryStream.Position = 0;
            _logger.LogInformation("Successfully downloaded asset ({Bytes} bytes)", totalRead);
            
            return memoryStream;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error downloading asset from {Url}: {Message}", downloadUrl, ex.Message);
            throw;
        }
    }

    private void InvalidateCache()
    {
        // Use a timeout to avoid potential deadlocks
        if (_cacheLock.Wait(TimeSpan.FromSeconds(5)))
        {
            try
            {
                _cachedReleases = null;
                _cacheExpiration = null;
                _logger.LogDebug("Release cache invalidated");
            }
            finally
            {
                _cacheLock.Release();
            }
        }
        else
        {
            _logger.LogWarning("Failed to acquire cache lock for invalidation within timeout");
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _cacheLock?.Dispose();
    }
}
