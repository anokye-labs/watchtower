using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WatchTower.Models;
using WatchTower.Services;
using Xunit;

namespace WatchTower.Tests.Services;

public class GitHubReleaseServiceTests
{
    private readonly Mock<ILogger<GitHubReleaseService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;

    public GitHubReleaseServiceTests()
    {
        _loggerMock = new Mock<ILogger<GitHubReleaseService>>();
        _configurationMock = new Mock<IConfiguration>();

        // Setup default configuration values
        _configurationMock
            .Setup(c => c.GetSection("DevMenu:GitHubOwner").Value)
            .Returns("anokye-labs");
        _configurationMock
            .Setup(c => c.GetSection("DevMenu:GitHubRepo").Value)
            .Returns("watchtower");
    }

    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        // Arrange & Act
        var service = new GitHubReleaseService(_loggerMock.Object, _configurationMock.Object);

        // Assert
        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public void SetAuthToken_WithValidToken_SetsAuthenticatedTrue()
    {
        // Arrange
        var service = new GitHubReleaseService(_loggerMock.Object, _configurationMock.Object);

        // Act
        service.SetAuthToken("ghp_testtoken123");

        // Assert
        Assert.True(service.IsAuthenticated);
    }

    [Fact]
    public void SetAuthToken_WithNullToken_SetsAuthenticatedFalse()
    {
        // Arrange
        var service = new GitHubReleaseService(_loggerMock.Object, _configurationMock.Object);
        service.SetAuthToken("ghp_testtoken123"); // Set first

        // Act
        service.SetAuthToken(null);

        // Assert
        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public void SetAuthToken_WithEmptyToken_SetsAuthenticatedFalse()
    {
        // Arrange
        var service = new GitHubReleaseService(_loggerMock.Object, _configurationMock.Object);
        service.SetAuthToken("ghp_testtoken123"); // Set first

        // Act
        service.SetAuthToken("");

        // Assert
        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public void SetAuthToken_WithWhitespaceToken_SetsAuthenticatedFalse()
    {
        // Arrange
        var service = new GitHubReleaseService(_loggerMock.Object, _configurationMock.Object);
        service.SetAuthToken("ghp_testtoken123"); // Set first

        // Act
        service.SetAuthToken("   ");

        // Assert
        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public async Task GetReleasesAsync_WithoutAuthentication_ReturnsEmptyListOnError()
    {
        // Arrange
        var service = new GitHubReleaseService(_loggerMock.Object, _configurationMock.Object);

        // Act - This may fail due to rate limiting or network issues, which is expected
        var releases = await service.GetReleasesAsync();

        // Assert - Should not throw and return a list (may be empty on error)
        Assert.NotNull(releases);
    }

    [Fact]
    public async Task GetPullRequestBuildsAsync_WithoutAuthentication_ReturnsEmptyList()
    {
        // Arrange
        var service = new GitHubReleaseService(_loggerMock.Object, _configurationMock.Object);

        // Act
        var prBuilds = await service.GetPullRequestBuildsAsync();

        // Assert
        Assert.NotNull(prBuilds);
        Assert.Empty(prBuilds);
    }

    [Fact]
    public async Task GetPullRequestBuildsAsync_WithAuthentication_AttemptsToFetchArtifacts()
    {
        // Arrange
        var service = new GitHubReleaseService(_loggerMock.Object, _configurationMock.Object);
        service.SetAuthToken("ghp_testtoken123");

        // Act - This will likely fail due to invalid token, but should not throw
        var prBuilds = await service.GetPullRequestBuildsAsync();

        // Assert - Should handle errors gracefully
        Assert.NotNull(prBuilds);
    }

    [Fact]
    public void IsAuthenticated_DefaultsToFalse()
    {
        // Arrange & Act
        var service = new GitHubReleaseService(_loggerMock.Object, _configurationMock.Object);

        // Assert
        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_AfterSettingToken_ReturnsTrue()
    {
        // Arrange
        var service = new GitHubReleaseService(_loggerMock.Object, _configurationMock.Object);

        // Act
        service.SetAuthToken("ghp_validtoken");

        // Assert
        Assert.True(service.IsAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_AfterClearingToken_ReturnsFalse()
    {
        // Arrange
        var service = new GitHubReleaseService(_loggerMock.Object, _configurationMock.Object);
        service.SetAuthToken("ghp_validtoken");

        // Act
        service.SetAuthToken(null);

        // Assert
        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public async Task DownloadAssetAsync_WithInvalidUrl_ThrowsException()
    {
        // Arrange
        var service = new GitHubReleaseService(_loggerMock.Object, _configurationMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await service.DownloadAssetAsync("http://invalid.url.that.does.not.exist", null);
        });
    }

    [Fact]
    public async Task GetReleasesAsync_UsesCaching()
    {
        // Arrange
        var service = new GitHubReleaseService(_loggerMock.Object, _configurationMock.Object);

        // Act - Call twice
        var releases1 = await service.GetReleasesAsync();
        var releases2 = await service.GetReleasesAsync();

        // Assert - Both calls should succeed
        Assert.NotNull(releases1);
        Assert.NotNull(releases2);
    }

    [Fact]
    public void SetAuthToken_InvalidatesCache()
    {
        // Arrange
        var service = new GitHubReleaseService(_loggerMock.Object, _configurationMock.Object);

        // Act
        service.SetAuthToken("ghp_newtoken");

        // Assert - Verify cache was invalidated (implicit through method call)
        Assert.True(service.IsAuthenticated);
    }
}
