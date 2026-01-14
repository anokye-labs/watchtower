using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WatchTower.Models;
using WatchTower.Services;
using WatchTower.Tests.TestHelpers;
using WatchTower.ViewModels;
using Xunit;

namespace WatchTower.Tests.ViewModels;

public class DevBuildMenuViewModelTests
{
    private readonly Mock<IGitHubReleaseService> _gitHubServiceMock;
    private readonly Mock<ICredentialStorageService> _credentialServiceMock;
    private readonly Mock<IBuildCacheService> _cacheServiceMock;
    private readonly Mock<ILogger<DevBuildMenuViewModel>> _loggerMock;

    public DevBuildMenuViewModelTests()
    {
        _gitHubServiceMock = ServiceMocks.CreateGitHubReleaseService();
        _credentialServiceMock = ServiceMocks.CreateCredentialStorageService();
        _cacheServiceMock = ServiceMocks.CreateBuildCacheService();
        _loggerMock = ServiceMocks.CreateLogger<DevBuildMenuViewModel>();
    }

    [Fact]
    public void Constructor_InitializesAllCommands()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.NotNull(viewModel.AuthenticateCommand);
        Assert.NotNull(viewModel.LaunchBuildCommand);
        Assert.NotNull(viewModel.RefreshCommand);
        Assert.NotNull(viewModel.ClearCacheCommand);
        Assert.NotNull(viewModel.CancelCommand);
    }

    [Fact]
    public void Constructor_InitializesBuildsCollection()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.NotNull(viewModel.Builds);
        Assert.Empty(viewModel.Builds);
    }

    [Fact]
    public async Task InitializeAsync_WithStoredCredentials_SetsAuthenticated()
    {
        // Arrange
        _credentialServiceMock
            .Setup(s => s.GetTokenAsync("github"))
            .ReturnsAsync("test-token");

        var viewModel = CreateViewModel();

        // Act
        await viewModel.InitializeAsync();

        // Assert
        Assert.True(viewModel.IsAuthenticated);
        _gitHubServiceMock.Verify(s => s.SetAuthToken("test-token"), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_WithoutStoredCredentials_RemainsUnauthenticated()
    {
        // Arrange
        _credentialServiceMock
            .Setup(s => s.GetTokenAsync("github"))
            .ReturnsAsync((string?)null);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.InitializeAsync();

        // Assert
        Assert.False(viewModel.IsAuthenticated);
    }

    [Fact]
    public async Task InitializeAsync_LoadsReleases()
    {
        // Arrange
        var releases = new List<ReleaseInfo>
        {
            new() { TagName = "v1.0", Name = "Release 1.0", CreatedAt = DateTimeOffset.Now, AssetDownloadUrl = "http://test.com/v1.0" },
            new() { TagName = "v2.0", Name = "Release 2.0", CreatedAt = DateTimeOffset.Now, AssetDownloadUrl = "http://test.com/v2.0" }
        };

        _gitHubServiceMock
            .Setup(s => s.GetReleasesAsync())
            .ReturnsAsync(releases);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.InitializeAsync();

        // Allow some time for async operations
        await Task.Delay(100);

        // Assert
        Assert.Equal(2, viewModel.Builds.Count);
        Assert.All(viewModel.Builds, b => Assert.Equal(BuildType.Release, b.Type));
    }

    [Fact]
    public void SelectedBuild_SetValue_TriggersLaunchCommandCanExecuteChanged()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var canExecuteChangedRaised = false;
        viewModel.LaunchBuildCommand.CanExecuteChanged += (s, e) => canExecuteChangedRaised = true;

        var build = new BuildListItem
        {
            Id = "test-1",
            DisplayName = "Test Build",
            Type = BuildType.Release,
            CreatedAt = DateTimeOffset.Now,
            Author = "Test",
            DownloadUrl = "http://test.com"
        };

        // Act
        viewModel.SelectedBuild = build;

        // Assert
        Assert.True(canExecuteChangedRaised);
        Assert.Equal(build, viewModel.SelectedBuild);
    }

    [Fact]
    public void LaunchBuildCommand_WhenSelectedBuildIsNull_CannotExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        Assert.False(viewModel.LaunchBuildCommand.CanExecute(null));
    }

    [Fact]
    public void LaunchBuildCommand_WhenSelectedBuildIsSet_CanExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedBuild = new BuildListItem
        {
            Id = "test-1",
            DisplayName = "Test Build",
            Type = BuildType.Release,
            CreatedAt = DateTimeOffset.Now,
            Author = "Test",
            DownloadUrl = "http://test.com"
        };

        // Act & Assert
        Assert.True(viewModel.LaunchBuildCommand.CanExecute(null));
    }

    [Fact]
    public void LaunchBuildCommand_WhenIsDownloading_CannotExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedBuild = new BuildListItem
        {
            Id = "test-1",
            DisplayName = "Test Build",
            Type = BuildType.Release,
            CreatedAt = DateTimeOffset.Now,
            Author = "Test",
            DownloadUrl = "http://test.com"
        };

        // Simulate downloading state through reflection
        typeof(DevBuildMenuViewModel)
            .GetProperty("IsDownloading")!
            .SetValue(viewModel, true);

        // Act & Assert
        Assert.False(viewModel.LaunchBuildCommand.CanExecute(null));
    }

    [Fact]
    public void AuthenticateCommand_WhenNotAuthenticated_CanExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        Assert.True(viewModel.AuthenticateCommand.CanExecute(null));
    }

    [Fact]
    public async Task AuthenticateCommand_WhenAuthenticated_CannotExecute()
    {
        // Arrange
        _credentialServiceMock
            .Setup(s => s.GetTokenAsync("github"))
            .ReturnsAsync("test-token");

        var viewModel = CreateViewModel();
        await viewModel.InitializeAsync();

        // Act & Assert
        Assert.False(viewModel.AuthenticateCommand.CanExecute(null));
    }

    [Fact]
    public void RefreshCommand_WhenNotLoading_CanExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        Assert.True(viewModel.RefreshCommand.CanExecute(null));
    }

    [Fact]
    public void RefreshCommand_WhenLoading_CannotExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Simulate loading state through reflection
        typeof(DevBuildMenuViewModel)
            .GetProperty("IsLoading")!
            .SetValue(viewModel, true);

        // Act & Assert
        Assert.False(viewModel.RefreshCommand.CanExecute(null));
    }

    [Fact]
    public void ClearCacheCommand_WhenNotDownloading_CanExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        Assert.True(viewModel.ClearCacheCommand.CanExecute(null));
    }

    [Fact]
    public void ClearCacheCommand_WhenDownloading_CannotExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Simulate downloading state through reflection
        typeof(DevBuildMenuViewModel)
            .GetProperty("IsDownloading")!
            .SetValue(viewModel, true);

        // Act & Assert
        Assert.False(viewModel.ClearCacheCommand.CanExecute(null));
    }

    [Fact]
    public void CancelCommand_AlwaysCanExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        Assert.True(viewModel.CancelCommand.CanExecute(null));
    }

    [Fact]
    public async Task Authenticate_WithValidToken_UpdatesIsAuthenticated()
    {
        // Arrange
        _gitHubServiceMock
            .Setup(s => s.ValidateTokenAsync("valid-token"))
            .ReturnsAsync(true);

        var viewModel = CreateViewModel();
        var tokenProvided = false;

        viewModel.RequestTokenInput += () =>
        {
            tokenProvided = true;
            return Task.FromResult<string?>("valid-token");
        };

        // Act
        viewModel.AuthenticateCommand.Execute(null);
        await Task.Delay(200); // Wait for async operation

        // Assert
        Assert.True(tokenProvided);
        Assert.True(viewModel.IsAuthenticated);
        _credentialServiceMock.Verify(s => s.StoreTokenAsync("github", "valid-token"), Times.Once);
        _gitHubServiceMock.Verify(s => s.SetAuthToken("valid-token"), Times.Once);
    }

    [Fact]
    public async Task Authenticate_WithInvalidToken_DoesNotUpdateIsAuthenticated()
    {
        // Arrange
        _gitHubServiceMock
            .Setup(s => s.ValidateTokenAsync("invalid-token"))
            .ReturnsAsync(false);

        var viewModel = CreateViewModel();
        viewModel.RequestTokenInput += () => Task.FromResult<string?>("invalid-token");

        // Act
        viewModel.AuthenticateCommand.Execute(null);
        await Task.Delay(200); // Wait for async operation

        // Assert
        Assert.False(viewModel.IsAuthenticated);
        Assert.Contains("Invalid token", viewModel.StatusMessage);
    }

    [Fact]
    public async Task ClearCache_UpdatesBuildCachedStatus()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var build = new BuildListItem
        {
            Id = "test-1",
            DisplayName = "Test Build",
            Type = BuildType.Release,
            CreatedAt = DateTimeOffset.Now,
            Author = "Test",
            DownloadUrl = "http://test.com",
            IsCached = true
        };
        viewModel.Builds.Add(build);

        // Act
        viewModel.ClearCacheCommand.Execute(null);
        await Task.Delay(100); // Wait for async operation

        // Assert
        Assert.False(build.IsCached);
        _cacheServiceMock.Verify(s => s.ClearAllCacheAsync(), Times.Once);
    }

    [Fact]
    public void Cancel_InvokesRequestClose()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var requestCloseInvoked = false;
        viewModel.RequestClose += () => requestCloseInvoked = true;

        // Act
        viewModel.CancelCommand.Execute(null);

        // Assert
        Assert.True(requestCloseInvoked);
    }

    [Fact]
    public void Dispose_DisposesResources()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.Dispose();

        // Assert - No exception should be thrown
        // Calling Dispose again should be safe (idempotent)
        viewModel.Dispose();
    }

    [Fact]
    public void BuildListItem_IsCachedChange_UpdatesStatus()
    {
        // Arrange
        var build = new BuildListItem
        {
            Id = "test-1",
            DisplayName = "Test Build",
            Type = BuildType.Release,
            CreatedAt = DateTimeOffset.Now,
            Author = "Test",
            DownloadUrl = "http://test.com"
        };

        Assert.Equal("Available", build.Status);

        // Act
        build.IsCached = true;

        // Assert
        Assert.Equal("Cached", build.Status);
    }

    [Fact]
    public void BuildListItem_TypeIcon_ReturnsCorrectIcon()
    {
        // Arrange & Act
        var releaseItem = new BuildListItem
        {
            Id = "release-1",
            DisplayName = "Release",
            Type = BuildType.Release,
            CreatedAt = DateTimeOffset.Now,
            Author = "Test",
            DownloadUrl = "http://test.com"
        };

        var prItem = new BuildListItem
        {
            Id = "pr-1",
            DisplayName = "PR",
            Type = BuildType.PullRequest,
            CreatedAt = DateTimeOffset.Now,
            Author = "Test",
            DownloadUrl = "http://test.com"
        };

        // Assert
        Assert.Equal("📦", releaseItem.TypeIcon);
        Assert.Equal("🔧", prItem.TypeIcon);
    }

    [Fact]
    public async Task RefreshBuildsAsync_LoadsBothReleasesAndPRBuilds_WhenAuthenticated()
    {
        // Arrange
        var releases = new List<ReleaseInfo>
        {
            new() { TagName = "v1.0", Name = "Release 1.0", CreatedAt = DateTimeOffset.Now, AssetDownloadUrl = "http://test.com/v1.0" }
        };

        var prBuilds = new List<PullRequestBuildInfo>
        {
            new() { PullRequestNumber = 123, Title = "Test PR", Author = "TestUser", CreatedAt = DateTimeOffset.Now, ArtifactDownloadUrl = "http://test.com/pr123" }
        };

        _gitHubServiceMock.Setup(s => s.GetReleasesAsync()).ReturnsAsync(releases);
        _gitHubServiceMock.Setup(s => s.GetPullRequestBuildsAsync()).ReturnsAsync(prBuilds);
        _credentialServiceMock.Setup(s => s.GetTokenAsync("github")).ReturnsAsync("token");

        var viewModel = CreateViewModel();
        await viewModel.InitializeAsync();

        // Wait for async operations
        await Task.Delay(200);

        // Assert
        Assert.Equal(2, viewModel.Builds.Count);
        Assert.Contains(viewModel.Builds, b => b.Type == BuildType.Release);
        Assert.Contains(viewModel.Builds, b => b.Type == BuildType.PullRequest);
    }

    [Fact]
    public async Task RefreshBuildsAsync_LoadsOnlyReleases_WhenNotAuthenticated()
    {
        // Arrange
        var releases = new List<ReleaseInfo>
        {
            new() { TagName = "v1.0", Name = "Release 1.0", CreatedAt = DateTimeOffset.Now, AssetDownloadUrl = "http://test.com/v1.0" }
        };

        _gitHubServiceMock.Setup(s => s.GetReleasesAsync()).ReturnsAsync(releases);
        _credentialServiceMock.Setup(s => s.GetTokenAsync("github")).ReturnsAsync((string?)null);

        var viewModel = CreateViewModel();
        await viewModel.InitializeAsync();

        // Wait for async operations
        await Task.Delay(200);

        // Assert
        Assert.Single(viewModel.Builds);
        Assert.All(viewModel.Builds, b => Assert.Equal(BuildType.Release, b.Type));
        _gitHubServiceMock.Verify(s => s.GetPullRequestBuildsAsync(), Times.Never);
    }

    private DevBuildMenuViewModel CreateViewModel()
    {
        return new DevBuildMenuViewModel(
            _gitHubServiceMock.Object,
            _credentialServiceMock.Object,
            _cacheServiceMock.Object,
            _loggerMock.Object);
    }
}
