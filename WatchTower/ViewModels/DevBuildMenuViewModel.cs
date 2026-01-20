using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using WatchTower.Models;

namespace WatchTower.ViewModels;

/// <summary>
/// PLACEHOLDER ViewModel for DevBuildMenuWindow.
/// This is a minimal stub to allow the View to compile.
/// The full implementation is tracked in issue #218.
/// </summary>
public class DevBuildMenuViewModel : ViewModelBase
{
    
    /// <summary>
    /// Event raised when the ViewModel requests the window to close.
    /// </summary>
    public event Action? RequestClose;
    
    /// <summary>
    /// Event raised when the ViewModel needs token input from the user.
    /// Returns the entered token or null if cancelled.
    /// </summary>
    public event Func<string, Task<string?>>? RequestTokenInput;

    // Properties required by the View
    public ObservableCollection<BuildInfo> Builds { get; } = new();
    public BuildInfo? SelectedBuild { get; set; }
    public bool IsAuthenticated { get; set; }
    public string? AuthenticatedUser { get; set; }
    public bool ShowTokenInput { get; set; }
    public string? TokenInput { get; set; }
    public bool IsDownloading { get; set; }
    public double DownloadProgress { get; set; }
    public string? DownloadSpeed { get; set; }
    public string? StatusMessage { get; set; }
    public string? CacheSize { get; set; }
    public bool HasCachedBuilds { get; set; }
    public bool CanLaunchBuild { get; set; }

    // Commands required by the View
    public ICommand? AuthenticateCommand { get; }
    public ICommand? CancelTokenInputCommand { get; }
    public ICommand? SubmitTokenCommand { get; }
    public ICommand? ClearCacheCommand { get; }
    public ICommand? RefreshCommand { get; }
    public ICommand? CancelCommand { get; }
    public ICommand? LaunchBuildCommand { get; }
}
