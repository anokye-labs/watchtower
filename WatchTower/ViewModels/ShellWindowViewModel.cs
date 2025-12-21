using System;
using System.ComponentModel;
using Avalonia.Threading;
using WatchTower.Services;

namespace WatchTower.ViewModels;

/// <summary>
/// ViewModel for the shell window that hosts both splash and main content.
/// Manages the transition between splash screen and main application.
/// </summary>
public class ShellWindowViewModel : ViewModelBase, IStartupLogger
{
    private object? _currentContent;
    private bool _isInSplashMode = true;
    private bool _isAnimating;
    private readonly SplashWindowViewModel _splashViewModel;

    public ShellWindowViewModel(SplashWindowViewModel splashViewModel)
    {
        _splashViewModel = splashViewModel ?? throw new ArgumentNullException(nameof(splashViewModel));
        _currentContent = _splashViewModel;
        
        // Forward exit request from splash
        _splashViewModel.ExitRequested += OnSplashExitRequested;
    }

    /// <summary>
    /// Gets or sets the current content to display (SplashViewModel or MainViewModel).
    /// </summary>
    public object? CurrentContent
    {
        get => _currentContent;
        set => SetProperty(ref _currentContent, value);
    }

    /// <summary>
    /// Gets whether the window is currently in splash mode.
    /// </summary>
    public bool IsInSplashMode
    {
        get => _isInSplashMode;
        set => SetProperty(ref _isInSplashMode, value);
    }

    /// <summary>
    /// Gets whether the window is currently animating.
    /// </summary>
    public bool IsAnimating
    {
        get => _isAnimating;
        set => SetProperty(ref _isAnimating, value);
    }

    /// <summary>
    /// Gets the splash view model for direct access.
    /// </summary>
    public SplashWindowViewModel SplashViewModel => _splashViewModel;

    /// <summary>
    /// Event raised when the user requests to exit from the splash screen.
    /// </summary>
    public event EventHandler? ExitRequested;

    private void OnSplashExitRequested(object? sender, EventArgs e)
    {
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Transitions from splash content to main content.
    /// </summary>
    /// <param name="mainViewModel">The main window view model to display.</param>
    public void TransitionToMainContent(MainWindowViewModel mainViewModel)
    {
        if (mainViewModel == null)
        {
            throw new ArgumentNullException(nameof(mainViewModel));
        }

        Dispatcher.UIThread.Post(() =>
        {
            CurrentContent = mainViewModel;
            IsInSplashMode = false;
        });
    }

    /// <summary>
    /// Marks that the expansion animation is starting.
    /// </summary>
    public void BeginExpansionAnimation()
    {
        IsAnimating = true;
    }

    /// <summary>
    /// Marks that the expansion animation has completed.
    /// </summary>
    public void EndExpansionAnimation()
    {
        IsAnimating = false;
    }

    // IStartupLogger implementation - forward to splash view model
    public void Info(string message) => _splashViewModel.Info(message);
    public void Warn(string message) => _splashViewModel.Warn(message);
    public void Error(string message, Exception? exception = null) => _splashViewModel.Error(message, exception);

    public void Cleanup()
    {
        _splashViewModel.ExitRequested -= OnSplashExitRequested;
        _splashViewModel.Cleanup();
    }
}
