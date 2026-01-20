using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WatchTower.Models;
using WatchTower.Services;
using WatchTower.Utilities;

namespace WatchTower.ViewModels;

/// <summary>
/// ViewModel for the Developer Build Menu.
/// Manages build list, authentication state, and download operations.
/// </summary>
public class DevBuildMenuViewModel : ViewModelBase, IDisposable
{
    private readonly IGitHubReleaseService _gitHubService;
    private readonly ICredentialStorageService _credentialService;
    private readonly IBuildCacheService _cacheService;
    private readonly ILogger<DevBuildMenuViewModel> _logger;
    private readonly SubscriptionManager _subscriptions = new();
    private CancellationTokenSource? _downloadCts;
    private bool _disposed;
    
    // Observable Properties
    private BuildListItem? _selectedBuild;
    private bool _isAuthenticated;
    private bool _isLoading;
    private bool _isDownloading;
    private double _downloadProgress;
    private string _downloadSpeed = string.Empty;
    private string _statusMessage = string.Empty;
    
    /// <summary>
    /// Collection of available builds.
    /// </summary>
    public ObservableCollection<BuildListItem> Builds { get; } = new();
    
    /// <summary>
    /// Currently selected build.
    /// </summary>
    public BuildListItem? SelectedBuild
    {
        get => _selectedBuild;
        set
        {
            if (SetProperty(ref _selectedBuild, value))
                LaunchBuildCommand.RaiseCanExecuteChanged();
        }
    }
    
    /// <summary>
    /// Whether the user is authenticated with GitHub.
    /// </summary>
    public bool IsAuthenticated
    {
        get => _isAuthenticated;
        private set
        {
            if (SetProperty(ref _isAuthenticated, value))
                AuthenticateCommand.RaiseCanExecuteChanged();
        }
    }
    
    /// <summary>
    /// Whether the build list is being loaded.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
                RefreshCommand.RaiseCanExecuteChanged();
        }
    }
    
    /// <summary>
    /// Whether a build is currently being downloaded.
    /// </summary>
    public bool IsDownloading
    {
        get => _isDownloading;
        private set
        {
            if (SetProperty(ref _isDownloading, value))
            {
                LaunchBuildCommand.RaiseCanExecuteChanged();
                ClearCacheCommand.RaiseCanExecuteChanged();
            }
        }
    }
    
    /// <summary>
    /// Download progress (0-100).
    /// </summary>
    public double DownloadProgress
    {
        get => _downloadProgress;
        private set => SetProperty(ref _downloadProgress, value);
    }
    
    /// <summary>
    /// Formatted download speed.
    /// </summary>
    public string DownloadSpeed
    {
        get => _downloadSpeed;
        private set => SetProperty(ref _downloadSpeed, value);
    }
    
    /// <summary>
    /// Status message for user feedback.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }
    
    // Commands
    public RelayCommand AuthenticateCommand { get; }
    public RelayCommand LaunchBuildCommand { get; }
    public RelayCommand RefreshCommand { get; }
    public RelayCommand ClearCacheCommand { get; }
    public RelayCommand CancelCommand { get; }
    
    // Events
    /// <summary>Request to close the menu window.</summary>
    public event Action? RequestClose;
    
    /// <summary>Request token input from the View layer.</summary>
    public event Func<Task<string?>>? RequestTokenInput;
    
    public DevBuildMenuViewModel(
        IGitHubReleaseService gitHubService,
        ICredentialStorageService credentialService,
        IBuildCacheService cacheService,
        ILogger<DevBuildMenuViewModel> logger)
    {
        _gitHubService = gitHubService;
        _credentialService = credentialService;
        _cacheService = cacheService;
        _logger = logger;
        
        // Initialize commands
        AuthenticateCommand = new RelayCommand(Authenticate, () => !IsAuthenticated);
        LaunchBuildCommand = new RelayCommand(LaunchBuild, () => SelectedBuild != null && !IsDownloading);
        RefreshCommand = new RelayCommand(Refresh, () => !IsLoading);
        ClearCacheCommand = new RelayCommand(ClearCache, () => !IsDownloading);
        CancelCommand = new RelayCommand(Cancel);
    }
    
    /// <summary>
    /// Initializes the ViewModel by checking for stored credentials and loading builds.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Check for stored credentials
        var token = await _credentialService.GetTokenAsync("github");
        if (!string.IsNullOrEmpty(token))
        {
            _gitHubService.SetAuthToken(token);
            IsAuthenticated = true;
        }
        
        // Load initial build list
        await RefreshBuildsAsync();
    }
    
    private void Authenticate()
    {
        Task.Run(async () =>
        {
            try
            {
                // 1. Request token from View
                var token = RequestTokenInput != null 
                    ? await RequestTokenInput.Invoke() 
                    : null;
                
                if (string.IsNullOrWhiteSpace(token)) return;
                
                // 2. Validate token against GitHub API
                StatusMessage = "Validating token...";
                var isValid = await _gitHubService.ValidateTokenAsync(token);
                
                if (!isValid)
                {
                    StatusMessage = "Invalid token. Please try again.";
                    return;
                }
                
                // 3. Store in credential storage
                await _credentialService.StoreTokenAsync("github", token);
                _gitHubService.SetAuthToken(token);
                
                // 4. Update state
                IsAuthenticated = true;
                StatusMessage = "Authenticated successfully!";
                
                // 5. Refresh to show PR builds
                await RefreshBuildsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Authentication failed");
                StatusMessage = "Authentication failed. Please try again.";
            }
        });
    }
    
    private void LaunchBuild()
    {
        // Capture the selected build reference before Task.Run to ensure thread safety
        var build = SelectedBuild;
        if (build == null) return;
        
        // Set IsDownloading synchronously to prevent race conditions from rapid clicks
        // This ensures the CanExecute guard blocks subsequent calls immediately
        IsDownloading = true;
        _downloadCts = new CancellationTokenSource();
        
        Task.Run(async () =>
        {
            try
            {
                string exePath;
                
                // Check cache first
                if (await _cacheService.IsBuildCachedAsync(build.Id))
                {
                    exePath = await _cacheService.GetCachedBuildPathAsync(build.Id) 
                        ?? throw new InvalidOperationException("Cached build not found");
                    StatusMessage = "Launching cached build...";
                }
                else
                {
                    // Download with progress
                    StatusMessage = $"Downloading {build.DisplayName}...";
                    
                    var progress = new Progress<DownloadProgress>(p =>
                    {
                        DownloadProgress = p.PercentComplete;
                        DownloadSpeed = FormatSpeed(p.BytesPerSecond);
                    });
                    
                    exePath = await _cacheService.DownloadAndCacheBuildAsync(
                        build.Id,
                        build.DownloadUrl,
                        progress,
                        _downloadCts.Token);
                    
                    build.IsCached = true;
                }
                
                // Launch in parallel process
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true
                });
                
                StatusMessage = "Build launched!";
                RequestClose?.Invoke();
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Download cancelled.";
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Network error downloading build {BuildId}", build.Id);
                StatusMessage = "Network error. Check your connection.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to launch build {BuildId}", build.Id);
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsDownloading = false;
                DownloadProgress = 0;
                DownloadSpeed = string.Empty;
                _downloadCts?.Dispose();
                _downloadCts = null;
            }
        });
    }
    
    private void Refresh()
    {
        Task.Run(RefreshBuildsAsync);
    }
    
    private async Task RefreshBuildsAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading builds...";
        
        try
        {
            // Clear builds on UI thread to avoid cross-thread collection modification
            await Dispatcher.UIThread.InvokeAsync(() => Builds.Clear());
            
            // Get releases (always available)
            var releases = await _gitHubService.GetReleasesAsync();
            foreach (var release in releases)
            {
                var isCached = await _cacheService.IsBuildCachedAsync($"release-{release.TagName}");
                var item = new BuildListItem
                {
                    Id = $"release-{release.TagName}",
                    DisplayName = release.Name,
                    Type = BuildType.Release,
                    CreatedAt = release.CreatedAt,
                    Author = "Release",
                    DownloadUrl = release.AssetDownloadUrl,
                    IsCached = isCached
                };
                // Add to collection on UI thread
                await Dispatcher.UIThread.InvokeAsync(() => Builds.Add(item));
            }
            
            // Get PR builds (only if authenticated)
            if (IsAuthenticated)
            {
                var prBuilds = await _gitHubService.GetPullRequestBuildsAsync();
                foreach (var pr in prBuilds)
                {
                    var isCached = await _cacheService.IsBuildCachedAsync($"pr-{pr.PullRequestNumber}");
                    var item = new BuildListItem
                    {
                        Id = $"pr-{pr.PullRequestNumber}",
                        DisplayName = $"PR #{pr.PullRequestNumber}: {pr.Title}",
                        Type = BuildType.PullRequest,
                        CreatedAt = pr.CreatedAt,
                        Author = pr.Author,
                        DownloadUrl = pr.ArtifactDownloadUrl,
                        IsCached = isCached
                    };
                    // Add to collection on UI thread
                    await Dispatcher.UIThread.InvokeAsync(() => Builds.Add(item));
                }
            }
            
            StatusMessage = $"Found {Builds.Count} builds.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh build list");
            StatusMessage = "Failed to load builds. Try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private void ClearCache()
    {
        Task.Run(async () =>
        {
            try
            {
                StatusMessage = "Clearing cache...";
                await _cacheService.ClearAllCacheAsync();
                
                // Update cached status for all builds
                foreach (var build in Builds)
                {
                    build.IsCached = false;
                }
                
                StatusMessage = "Cache cleared successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clear cache");
                StatusMessage = "Failed to clear cache.";
            }
        });
    }
    
    private void Cancel()
    {
        _downloadCts?.Cancel();
        RequestClose?.Invoke();
    }
    
    private static string FormatSpeed(double bytesPerSecond)
    {
        return bytesPerSecond switch
        {
            >= 1_000_000 => $"{bytesPerSecond / 1_000_000:F1} MB/s",
            >= 1_000 => $"{bytesPerSecond / 1_000:F1} KB/s",
            _ => $"{bytesPerSecond:F0} B/s"
        };
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        
        _downloadCts?.Cancel();
        _downloadCts?.Dispose();
        _subscriptions.Dispose();
        
        _disposed = true;
    }
}
