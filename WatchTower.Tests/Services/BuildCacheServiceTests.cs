using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WatchTower.Models;
using WatchTower.Services;
using Xunit;

namespace WatchTower.Tests.Services;

public class BuildCacheServiceTests : IDisposable
{
    private readonly Mock<ILogger<BuildCacheService>> _loggerMock;
    private readonly string _originalHome;
    private readonly string _originalLocalAppData;
    private readonly string _tempDir;
    private readonly string _testCacheRoot;

    public BuildCacheServiceTests()
    {
        _loggerMock = new Mock<ILogger<BuildCacheService>>();
        
        // Create a temporary directory for test cache
        _tempDir = Path.Combine(Path.GetTempPath(), $"WatchTowerTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        
        // Override HOME for Linux/Mac and LOCALAPPDATA for Windows
        _originalHome = Environment.GetEnvironmentVariable("HOME") ?? "";
        _originalLocalAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? "";
        Environment.SetEnvironmentVariable("HOME", _tempDir);
        Environment.SetEnvironmentVariable("LOCALAPPDATA", _tempDir);
        
        // On Linux, LocalApplicationData = $HOME/.local/share
        // We need to ensure .local/share exists for Environment.GetFolderPath to work
        var localSharePath = Path.Combine(_tempDir, ".local", "share");
        Directory.CreateDirectory(localSharePath);
        
        _testCacheRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WatchTower",
            "DevMenuBuilds");
    }

    public void Dispose()
    {
        // Restore original environment variables
        Environment.SetEnvironmentVariable("HOME", _originalHome);
        Environment.SetEnvironmentVariable("LOCALAPPDATA", _originalLocalAppData);
        
        // Clean up test directories
        try
        {
            if (!string.IsNullOrEmpty(_tempDir) && 
                _tempDir.Contains("WatchTowerTests_") && 
                Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public void Constructor_CreatesBaseCacheDirectory()
    {
        // Arrange & Act
        _ = new BuildCacheService(_loggerMock.Object);

        // Assert
        Assert.True(Directory.Exists(_testCacheRoot));
    }

    [Fact]
    public void Constructor_CreatesManifestFile()
    {
        // Arrange & Act
        _ = new BuildCacheService(_loggerMock.Object);

        // Assert
        var manifestPath = Path.Combine(_testCacheRoot, "manifest.json");
        
        // Debug output
        if (!File.Exists(manifestPath))
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            throw new Exception($"Manifest not found. Expected: {manifestPath}, LocalAppData: {localAppData}, TestCacheRoot: {_testCacheRoot}, Exists: {Directory.Exists(_testCacheRoot)}");
        }
        
        Assert.True(File.Exists(manifestPath));
    }

    [Fact]
    public async Task IsBuildCachedAsync_ReturnsFalse_WhenBuildNotCached()
    {
        // Arrange
        var service = new BuildCacheService(_loggerMock.Object);

        // Act
        var isCached = await service.IsBuildCachedAsync("release-v1.0.0");

        // Assert
        Assert.False(isCached);
    }

    [Fact]
    public async Task GetCachedBuildPathAsync_ReturnsNull_WhenBuildNotCached()
    {
        // Arrange
        var service = new BuildCacheService(_loggerMock.Object);

        // Act
        var path = await service.GetCachedBuildPathAsync("release-v1.0.0");

        // Assert
        Assert.Null(path);
    }

    [Fact]
    public void GetCacheSizeBytes_ReturnsZero_WhenNoBuildsCached()
    {
        // Arrange
        var service = new BuildCacheService(_loggerMock.Object);

        // Act
        var size = service.GetCacheSizeBytes();

        // Assert
        Assert.Equal(0, size);
    }

    [Fact]
    public void GetCachedBuilds_ReturnsEmptyList_WhenNoBuildsCached()
    {
        // Arrange
        var service = new BuildCacheService(_loggerMock.Object);

        // Act
        var builds = service.GetCachedBuilds();

        // Assert
        Assert.NotNull(builds);
        Assert.Empty(builds);
    }

    [Fact]
    public async Task ClearCacheAsync_RemovesAllBuilds()
    {
        // Arrange
        // Create a fake cached build directory first
        var buildPath = Path.Combine(_testCacheRoot, "releases", "v1.0.0");
        Directory.CreateDirectory(buildPath);
        var exePath = Path.Combine(buildPath, "WatchTower.exe");
        File.WriteAllText(exePath, "fake exe");
        
        // Manually add to manifest
        var manifestPath = Path.Combine(_testCacheRoot, "manifest.json");
        var manifestJson = @"{
            ""version"": 1,
            ""builds"": [
                {
                    ""buildId"": ""release-v1.0.0"",
                    ""displayName"": ""v1.0.0"",
                    ""localPath"": ""releases/v1.0.0"",
                    ""downloadedAt"": ""2026-01-10T12:00:00Z"",
                    ""sizeBytes"": 100,
                    ""type"": ""Release""
                }
            ]
        }";
        File.WriteAllText(manifestPath, manifestJson);
        
        // Create service instance to load the manifest
        var service = new BuildCacheService(_loggerMock.Object);

        // Act
        await service.ClearCacheAsync();

        // Assert
        var builds = service.GetCachedBuilds();
        Assert.Empty(builds);
        Assert.False(Directory.Exists(buildPath));
    }

    [Fact]
    public async Task CleanOldBuildsAsync_RemovesBuildsOlderThanMaxAge()
    {
        // Arrange
        // Create fake cached build directories
        var oldBuildPath = Path.Combine(_testCacheRoot, "releases", "v1.0.0");
        var newBuildPath = Path.Combine(_testCacheRoot, "releases", "v2.0.0");
        Directory.CreateDirectory(oldBuildPath);
        Directory.CreateDirectory(newBuildPath);
        File.WriteAllText(Path.Combine(oldBuildPath, "WatchTower.exe"), "fake exe");
        File.WriteAllText(Path.Combine(newBuildPath, "WatchTower.exe"), "fake exe");
        
        // Manually create manifest with old and new builds
        var manifestPath = Path.Combine(_testCacheRoot, "manifest.json");
        var oldDate = DateTimeOffset.UtcNow.AddDays(-10).ToString("o");
        var newDate = DateTimeOffset.UtcNow.AddDays(-3).ToString("o");
        var manifestJson = $@"{{
            ""version"": 1,
            ""builds"": [
                {{
                    ""buildId"": ""release-v1.0.0"",
                    ""displayName"": ""v1.0.0"",
                    ""localPath"": ""releases/v1.0.0"",
                    ""downloadedAt"": ""{oldDate}"",
                    ""sizeBytes"": 100,
                    ""type"": ""Release""
                }},
                {{
                    ""buildId"": ""release-v2.0.0"",
                    ""displayName"": ""v2.0.0"",
                    ""localPath"": ""releases/v2.0.0"",
                    ""downloadedAt"": ""{newDate}"",
                    ""sizeBytes"": 100,
                    ""type"": ""Release""
                }}
            ]
        }}";
        File.WriteAllText(manifestPath, manifestJson);
        
        // Create service instance to load the manifest
        var service = new BuildCacheService(_loggerMock.Object);

        // Act
        await service.CleanOldBuildsAsync(TimeSpan.FromDays(7));

        // Assert
        var builds = service.GetCachedBuilds();
        Assert.Single(builds);
        Assert.Equal("release-v2.0.0", builds[0].BuildId);
        Assert.False(Directory.Exists(oldBuildPath));
        Assert.True(Directory.Exists(newBuildPath));
    }

    [Fact]
    public async Task IsBuildCachedAsync_ReturnsTrue_AfterManualBuildCreation()
    {
        // Arrange
        // Create a fake cached build
        var buildPath = Path.Combine(_testCacheRoot, "releases", "v1.0.0");
        Directory.CreateDirectory(buildPath);
        File.WriteAllText(Path.Combine(buildPath, "WatchTower.exe"), "fake exe");
        
        // Manually add to manifest
        var manifestPath = Path.Combine(_testCacheRoot, "manifest.json");
        var manifestJson = @"{
            ""version"": 1,
            ""builds"": [
                {
                    ""buildId"": ""release-v1.0.0"",
                    ""displayName"": ""v1.0.0"",
                    ""localPath"": ""releases/v1.0.0"",
                    ""downloadedAt"": ""2026-01-10T12:00:00Z"",
                    ""sizeBytes"": 100,
                    ""type"": ""Release""
                }
            ]
        }";
        File.WriteAllText(manifestPath, manifestJson);
        
        // Create service to load manifest
        var service = new BuildCacheService(_loggerMock.Object);

        // Act
        var isCached = await service.IsBuildCachedAsync("release-v1.0.0");

        // Assert
        Assert.True(isCached);
    }

    [Fact]
    public async Task GetCachedBuildPathAsync_ReturnsExecutablePath_WhenBuildCached()
    {
        // Arrange
        // Create a fake cached build
        var buildPath = Path.Combine(_testCacheRoot, "releases", "v1.0.0");
        Directory.CreateDirectory(buildPath);
        var exePath = Path.Combine(buildPath, "WatchTower.exe");
        File.WriteAllText(exePath, "fake exe");
        
        // Manually add to manifest
        var manifestPath = Path.Combine(_testCacheRoot, "manifest.json");
        var manifestJson = @"{
            ""version"": 1,
            ""builds"": [
                {
                    ""buildId"": ""release-v1.0.0"",
                    ""displayName"": ""v1.0.0"",
                    ""localPath"": ""releases/v1.0.0"",
                    ""downloadedAt"": ""2026-01-10T12:00:00Z"",
                    ""sizeBytes"": 100,
                    ""type"": ""Release""
                }
            ]
        }";
        File.WriteAllText(manifestPath, manifestJson);
        
        // Create service to load manifest
        var service = new BuildCacheService(_loggerMock.Object);

        // Act
        var path = await service.GetCachedBuildPathAsync("release-v1.0.0");

        // Assert
        Assert.NotNull(path);
        Assert.Equal(exePath, path);
    }

    [Fact]
    public void GetCacheSizeBytes_ReturnsTotalSize_WhenBuildsAreCached()
    {
        // Arrange
        // Ensure cache directory exists
        Directory.CreateDirectory(_testCacheRoot);
        
        // Manually create manifest with builds
        var manifestPath = Path.Combine(_testCacheRoot, "manifest.json");
        var manifestJson = @"{
            ""version"": 1,
            ""builds"": [
                {
                    ""buildId"": ""release-v1.0.0"",
                    ""displayName"": ""v1.0.0"",
                    ""localPath"": ""releases/v1.0.0"",
                    ""downloadedAt"": ""2026-01-10T12:00:00Z"",
                    ""sizeBytes"": 1000,
                    ""type"": ""Release""
                },
                {
                    ""buildId"": ""release-v2.0.0"",
                    ""displayName"": ""v2.0.0"",
                    ""localPath"": ""releases/v2.0.0"",
                    ""downloadedAt"": ""2026-01-11T12:00:00Z"",
                    ""sizeBytes"": 2000,
                    ""type"": ""Release""
                }
            ]
        }";
        File.WriteAllText(manifestPath, manifestJson);
        
        // Create service to load manifest
        var service = new BuildCacheService(_loggerMock.Object);

        // Act
        var size = service.GetCacheSizeBytes();

        // Assert
        Assert.Equal(3000, size);
    }

    [Fact]
    public void GetCachedBuilds_ReturnsAllBuilds_WhenBuildsAreCached()
    {
        // Arrange
        // Ensure cache directory exists
        Directory.CreateDirectory(_testCacheRoot);
        
        // Manually create manifest with builds
        var manifestPath = Path.Combine(_testCacheRoot, "manifest.json");
        var manifestJson = @"{
            ""version"": 1,
            ""builds"": [
                {
                    ""buildId"": ""release-v1.0.0"",
                    ""displayName"": ""v1.0.0"",
                    ""localPath"": ""releases/v1.0.0"",
                    ""downloadedAt"": ""2026-01-10T12:00:00Z"",
                    ""sizeBytes"": 1000,
                    ""type"": ""Release""
                },
                {
                    ""buildId"": ""pr-123"",
                    ""displayName"": ""PR #123"",
                    ""localPath"": ""pull-requests/pr-123"",
                    ""downloadedAt"": ""2026-01-11T12:00:00Z"",
                    ""sizeBytes"": 2000,
                    ""type"": ""PullRequest""
                }
            ]
        }";
        File.WriteAllText(manifestPath, manifestJson);
        
        // Create service to load manifest
        var service = new BuildCacheService(_loggerMock.Object);

        // Act
        var builds = service.GetCachedBuilds();

        // Assert
        Assert.Equal(2, builds.Count);
        Assert.Contains(builds, b => b.BuildId == "release-v1.0.0" && b.Type == BuildType.Release);
        Assert.Contains(builds, b => b.BuildId == "pr-123" && b.Type == BuildType.PullRequest);
    }

    [Fact]
    public async Task GetCachedBuildPathAsync_ReturnsNull_WhenDirectoryDoesNotExist()
    {
        // Arrange
        // Ensure cache directory exists
        Directory.CreateDirectory(_testCacheRoot);
        
        // Manually create manifest with a build (but don't create the directory)
        var manifestPath = Path.Combine(_testCacheRoot, "manifest.json");
        var manifestJson = @"{
            ""version"": 1,
            ""builds"": [
                {
                    ""buildId"": ""release-v1.0.0"",
                    ""displayName"": ""v1.0.0"",
                    ""localPath"": ""releases/v1.0.0"",
                    ""downloadedAt"": ""2026-01-10T12:00:00Z"",
                    ""sizeBytes"": 1000,
                    ""type"": ""Release""
                }
            ]
        }";
        File.WriteAllText(manifestPath, manifestJson);
        
        // Create service to load manifest
        var service = new BuildCacheService(_loggerMock.Object);

        // Act
        var path = await service.GetCachedBuildPathAsync("release-v1.0.0");

        // Assert
        Assert.Null(path);
        
        // Verify build was removed from manifest
        var builds = service.GetCachedBuilds();
        Assert.Empty(builds);
    }
}
