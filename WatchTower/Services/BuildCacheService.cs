using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using WatchTower.Models;

namespace WatchTower.Services;

/// <summary>
/// Service for managing build downloads with progress reporting and local caching.
/// </summary>
public class BuildCacheService : IBuildCacheService
{
    private readonly ILogger<BuildCacheService> _logger;
    private readonly string _cacheRootPath;
    private readonly string _manifestPath;
    private readonly object _lock = new();
    private readonly object _downloadLock = new();
    private readonly Dictionary<string, SemaphoreSlim> _downloadSemaphores = new();
    private readonly BuildManifest _manifest;

    // Static HttpClient to avoid socket exhaustion
    private static readonly HttpClient _httpClient = new()
    {
        // Use infinite timeout for potentially large/long-running build downloads.
        // Cancellation should be controlled via the provided CancellationToken.
        Timeout = Timeout.InfiniteTimeSpan
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public BuildCacheService(ILogger<BuildCacheService> logger)
    {
        _logger = logger;
        _cacheRootPath = GetCacheRootPath();
        _manifestPath = Path.Combine(_cacheRootPath, "manifest.json");
        
        // Ensure cache directory exists
        Directory.CreateDirectory(_cacheRootPath);
        
        // Load or create manifest
        _manifest = LoadManifest();
        
        // If manifest was newly created (file didn't exist), save it
        if (!File.Exists(_manifestPath))
        {
            SaveManifest();
        }
    }

    public Task<string?> GetCachedBuildPathAsync(string buildId)
    {
        string? exePath = null;
        
        lock (_lock)
        {
            var build = _manifest.Builds.FirstOrDefault(b => b.BuildId == buildId);
            if (build == null)
            {
                return Task.FromResult<string?>(null);
            }

            var fullPath = Path.Combine(_cacheRootPath, build.LocalPath);
            if (!Directory.Exists(fullPath))
            {
                // Build is in manifest but directory doesn't exist - remove from manifest
                _logger.LogWarning("Build {BuildId} is in manifest but directory doesn't exist. Removing from manifest.", buildId);
                _manifest.Builds.Remove(build);
                SaveManifest();
                return Task.FromResult<string?>(null);
            }
            
            // Store the path to find executable outside the lock
            exePath = fullPath;
        }
        
        // Find the executable outside the lock to avoid I/O operations inside the lock
        if (exePath != null)
        {
            var exe = FindExecutable(exePath);
            return Task.FromResult<string?>(exe);
        }
        
        return Task.FromResult<string?>(null);
    }

    public Task<bool> IsBuildCachedAsync(string buildId)
    {
        lock (_lock)
        {
            var build = _manifest.Builds.FirstOrDefault(b => b.BuildId == buildId);
            if (build == null)
            {
                return Task.FromResult(false);
            }

            var fullPath = Path.Combine(_cacheRootPath, build.LocalPath);
            return Task.FromResult(Directory.Exists(fullPath));
        }
    }

    public async Task<string> DownloadAndCacheBuildAsync(
        string buildId,
        string downloadUrl,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(buildId))
        {
            throw new ArgumentException("Build ID must be a non-empty, non-whitespace string.", nameof(buildId));
        }

        if (string.IsNullOrWhiteSpace(downloadUrl))
        {
            throw new ArgumentException("Download URL must be a non-empty, non-whitespace string.", nameof(downloadUrl));
        }

        if (!Uri.TryCreate(downloadUrl, UriKind.Absolute, out var parsedUri) ||
            (parsedUri.Scheme != Uri.UriSchemeHttp && parsedUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Download URL must be a valid absolute HTTP or HTTPS URL.", nameof(downloadUrl));
        }
        
        _logger.LogInformation("Starting download for build {BuildId} from {Url}", buildId, downloadUrl);

        // Use a semaphore to ensure only one download per buildId at a time
        SemaphoreSlim semaphore;
        lock (_downloadLock)
        {
            if (!_downloadSemaphores.TryGetValue(buildId, out semaphore!))
            {
                semaphore = new SemaphoreSlim(1, 1);
                _downloadSemaphores[buildId] = semaphore;
            }
        }

        await semaphore.WaitAsync(ct);
        try
        {
            // Create temp file for download
            var tempFile = Path.Combine(Path.GetTempPath(), $"watchtower-build-{Guid.NewGuid()}.zip");
            
            try
            {
                // Download with progress reporting
                await DownloadFileAsync(downloadUrl, tempFile, progress, ct);
                
                // Determine build type and display name from buildId
                var (buildType, displayName, relativePath) = ParseBuildId(buildId);
                
                // Extract ZIP to cache folder
                var extractPath = Path.Combine(_cacheRootPath, relativePath);
                Directory.CreateDirectory(extractPath);
                
                _logger.LogInformation("Extracting build to {Path}", extractPath);
                
                // Extract with zip slip protection
                ExtractZipSafely(tempFile, extractPath);
                
                // Find executable in extracted files
                var exePath = FindExecutable(extractPath);
                if (exePath == null)
                {
                    var sampleFiles = Directory.EnumerateFiles(extractPath, "*", SearchOption.AllDirectories)
                        .Select(Path.GetFileName)
                        .Where(name => !string.IsNullOrEmpty(name))
                        .Take(10)
                        .ToList();

                    var fileSummary = sampleFiles.Count == 0
                        ? "No files were found in the extraction directory."
                        : $"Sample files in extraction directory: {string.Join(", ", sampleFiles)}";

                    var message =
                        $"No executable was found in extracted build content. " +
                        $"BuildId: {buildId}, ExtractionPath: {extractPath}, SearchPattern: \"*.exe\". " +
                        fileSummary;

                    _logger.LogWarning(message);
                    throw new InvalidOperationException(message);
                }
                
                // Calculate build size
                var sizeBytes = CalculateDirectorySize(extractPath);
                
                // Update manifest
                lock (_lock)
                {
                    // Remove existing entry if present
                    var existing = _manifest.Builds.FirstOrDefault(b => b.BuildId == buildId);
                    if (existing != null)
                    {
                        _manifest.Builds.Remove(existing);
                    }
                    
                    // Add new entry
                    var buildInfo = new BuildInfo(
                        buildId,
                        displayName,
                        relativePath,
                        DateTimeOffset.UtcNow,
                        sizeBytes,
                        buildType);
                        
                    _manifest.Builds.Add(buildInfo);
                    SaveManifest();
                }
                
                _logger.LogInformation("Successfully cached build {BuildId} at {Path}", buildId, exePath);
                return exePath;
            }
            catch (OperationCanceledException)
            {
                // Clean up partial temp file on cancellation
                if (File.Exists(tempFile))
                {
                    try
                    {
                        File.Delete(tempFile);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temp file {TempFile} after cancellation", tempFile);
                    }
                }
                throw;
            }
            finally
            {
                // Delete temp file
                if (File.Exists(tempFile))
                {
                    try
                    {
                        File.Delete(tempFile);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temp file {TempFile}", tempFile);
                    }
                }
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    public Task ClearCacheAsync()
    {
        _logger.LogInformation("Clearing all cached builds");
        
        List<(string BuildId, string FullPath)> buildsToDelete;
        
        lock (_lock)
        {
            // Collect paths to delete while holding the lock
            buildsToDelete = _manifest.Builds
                .Select(b => (b.BuildId, FullPath: Path.Combine(_cacheRootPath, b.LocalPath)))
                .ToList();
        }
        
        // Delete directories outside the lock
        foreach (var (buildId, fullPath) in buildsToDelete)
        {
            if (Directory.Exists(fullPath))
            {
                try
                {
                    Directory.Delete(fullPath, recursive: true);
                    _logger.LogDebug("Deleted build directory {Path}", fullPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete build directory {Path}", fullPath);
                }
            }
        }
        
        // Clear manifest with lock
        lock (_lock)
        {
            _manifest.Builds.Clear();
            SaveManifest();
        }
        
        return Task.CompletedTask;
    }

    public Task CleanOldBuildsAsync(TimeSpan maxAge)
    {
        var cutoff = DateTimeOffset.UtcNow - maxAge;
        _logger.LogInformation("Cleaning builds older than {Cutoff} (maxAge: {MaxAge})", cutoff, maxAge);
        
        List<(BuildInfo Build, string FullPath)> oldBuildsToDelete;
        
        lock (_lock)
        {
            // Collect old builds to delete while holding the lock
            oldBuildsToDelete = _manifest.Builds
                .Where(b => b.DownloadedAt < cutoff)
                .Select(b => (Build: b, FullPath: Path.Combine(_cacheRootPath, b.LocalPath)))
                .ToList();
        }
        
        // Delete directories outside the lock
        foreach (var (build, fullPath) in oldBuildsToDelete)
        {
            if (Directory.Exists(fullPath))
            {
                try
                {
                    Directory.Delete(fullPath, recursive: true);
                    _logger.LogInformation("Deleted old build {BuildId} from {Date}", build.BuildId, build.DownloadedAt);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old build directory {Path}", fullPath);
                }
            }
        }
        
        // Remove from manifest with lock
        if (oldBuildsToDelete.Count > 0)
        {
            lock (_lock)
            {
                foreach (var (build, _) in oldBuildsToDelete)
                {
                    _manifest.Builds.Remove(build);
                }
                SaveManifest();
            }
        }
        
        return Task.CompletedTask;
    }

    public long GetCacheSizeBytes()
    {
        lock (_lock)
        {
            return _manifest.Builds.Sum(b => b.SizeBytes);
        }
    }

    public IReadOnlyList<BuildInfo> GetCachedBuilds()
    {
        lock (_lock)
        {
            return _manifest.Builds.ToList().AsReadOnly();
        }
    }

    // Private helper methods

    private static string GetCacheRootPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "WatchTower", "DevMenuBuilds");
    }

    private BuildManifest LoadManifest()
    {
        try
        {
            if (File.Exists(_manifestPath))
            {
                var json = File.ReadAllText(_manifestPath);
                var manifest = JsonSerializer.Deserialize<BuildManifest>(json, JsonOptions);
                if (manifest != null)
                {
                    _logger.LogDebug("Loaded manifest with {Count} builds", manifest.Builds.Count);
                    return manifest;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load manifest from {Path}, creating new manifest", _manifestPath);
        }
        
        return new BuildManifest { Version = 1, Builds = new List<BuildInfo>() };
    }

    private void SaveManifest()
    {
        try
        {
            var json = JsonSerializer.Serialize(_manifest, JsonOptions);
            File.WriteAllText(_manifestPath, json);
            _logger.LogDebug("Saved manifest with {Count} builds", _manifest.Builds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save manifest to {Path}", _manifestPath);
            throw;
        }
    }

    private async Task DownloadFileAsync(
        string url,
        string destinationPath,
        IProgress<DownloadProgress>? progress,
        CancellationToken ct)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        
        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        var bytesReceived = 0L;
        
        // For calculating speed
        var speedSamples = new Queue<(DateTimeOffset Timestamp, long Bytes)>();
        var stopwatch = Stopwatch.StartNew();
        var lastReportTime = stopwatch.Elapsed;
        
        using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true);
        
        var buffer = new byte[8192];
        int bytesRead;
        
        while ((bytesRead = await contentStream.ReadAsync(buffer, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
            bytesReceived += bytesRead;
            
            // Report progress every 100ms minimum
            var currentTime = stopwatch.Elapsed;
            if (progress != null && (currentTime - lastReportTime).TotalMilliseconds >= 100)
            {
                // Update speed samples (keep last 5 samples for sliding window)
                speedSamples.Enqueue((DateTimeOffset.UtcNow, bytesReceived));
                while (speedSamples.Count > 5)
                {
                    speedSamples.Dequeue();
                }
                
                // Calculate speed
                var bytesPerSecond = CalculateSpeed(speedSamples);
                var percentComplete = totalBytes > 0 ? (bytesReceived * 100.0 / totalBytes) : 0;
                
                progress.Report(new DownloadProgress(bytesReceived, totalBytes, percentComplete, bytesPerSecond));
                lastReportTime = currentTime;
            }
        }
        
        // Final progress report
        if (progress != null)
        {
            var bytesPerSecond = CalculateSpeed(speedSamples);
            progress.Report(new DownloadProgress(bytesReceived, totalBytes, 100.0, bytesPerSecond));
        }
    }

    private static double CalculateSpeed(Queue<(DateTimeOffset Timestamp, long Bytes)> samples)
    {
        if (samples.Count < 2)
        {
            return 0;
        }
        
        var first = samples.First();
        var last = samples.Last();
        var elapsedSeconds = (last.Timestamp - first.Timestamp).TotalSeconds;
        
        if (elapsedSeconds <= 0)
        {
            return 0;
        }
        
        return (last.Bytes - first.Bytes) / elapsedSeconds;
    }

    private static (BuildType Type, string DisplayName, string RelativePath) ParseBuildId(string buildId)
    {
        // Parse buildId format: "release-v1.0.0" or "pr-123"
        if (buildId.StartsWith("release-", StringComparison.OrdinalIgnoreCase))
        {
            var version = buildId["release-".Length..];
            return (BuildType.Release, version, Path.Combine("releases", version));
        }
        else if (buildId.StartsWith("pr-", StringComparison.OrdinalIgnoreCase))
        {
            var prNumber = buildId["pr-".Length..];
            return (BuildType.PullRequest, $"PR #{prNumber}", Path.Combine("pull-requests", $"pr-{prNumber}"));
        }
        else
        {
            // Fallback: treat as release
            return (BuildType.Release, buildId, Path.Combine("releases", buildId));
        }
    }

    private void ExtractZipSafely(string zipPath, string extractPath)
    {
        var extractRootFullPath = Path.GetFullPath(extractPath);
        
        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.FullName))
            {
                continue;
            }

            var destinationPath = Path.Combine(extractPath, entry.FullName);
            var fullDestinationPath = Path.GetFullPath(destinationPath);

            // Prevent zip slip by ensuring the destination path is within the extract root
            if (!fullDestinationPath.StartsWith(extractRootFullPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(fullDestinationPath, extractRootFullPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"ZIP entry '{entry.FullName}' is trying to extract outside of the target directory.");
            }

            if (entry.FullName.EndsWith("/", StringComparison.Ordinal) || entry.FullName.EndsWith("\\", StringComparison.Ordinal))
            {
                Directory.CreateDirectory(fullDestinationPath);
                continue;
            }

            var directoryName = Path.GetDirectoryName(fullDestinationPath);
            if (!string.IsNullOrEmpty(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            entry.ExtractToFile(fullDestinationPath, overwrite: true);
        }
    }

    private string? FindExecutable(string directory)
    {
        try
        {
            // Look for WatchTower.exe
            var exeFiles = Directory.GetFiles(directory, "WatchTower.exe", SearchOption.AllDirectories);
            if (exeFiles.Length > 0)
            {
                return exeFiles[0];
            }
            
            // Fallback: any .exe file
            exeFiles = Directory.GetFiles(directory, "*.exe", SearchOption.AllDirectories);
            if (exeFiles.Length > 0)
            {
                return exeFiles[0];
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to find executable in {Directory}", directory);
        }
        
        return null;
    }

    private static long CalculateDirectorySize(string directory)
    {
        try
        {
            var directoryInfo = new DirectoryInfo(directory);
            return directoryInfo.GetFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
        }
        catch
        {
            return 0;
        }
    }

    // Internal manifest structure
    private class BuildManifest
    {
        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("builds")]
        public List<BuildInfo> Builds { get; set; } = new();
    }
}
