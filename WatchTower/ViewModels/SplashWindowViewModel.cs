using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Threading;
using WatchTower.Services;

namespace WatchTower.ViewModels;

/// <summary>
/// ViewModel for the splash screen window.
/// Implements IStartupLogger to capture and display startup diagnostics.
/// </summary>
public class SplashWindowViewModel : ViewModelBase, IStartupLogger
{
    private readonly DispatcherTimer _timer;
    private readonly DateTime _startTime;
    private string _elapsedTime = "00:00";
    private bool _isDiagnosticsVisible;
    private bool _isStartupComplete;
    private bool _isStartupFailed;
    private bool _isSlowStartup;
    private string _statusMessage = "Loading...";
    private readonly int _hangThresholdSeconds;

    public SplashWindowViewModel(int hangThresholdSeconds = 30)
    {
        _hangThresholdSeconds = hangThresholdSeconds;
        _startTime = DateTime.Now;
        
        DiagnosticMessages = new ObservableCollection<string>();
        
        // Timer for elapsed time and hang detection
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();
        
        // Commands
        ToggleDiagnosticsCommand = new RelayCommand(ToggleDiagnostics);
        ExitCommand = new RelayCommand(Exit);
        
        Info("Application startup initiated");
    }

    #region Properties

    /// <summary>
    /// Elapsed time display (MM:SS format).
    /// </summary>
    public string ElapsedTime
    {
        get => _elapsedTime;
        private set => SetProperty(ref _elapsedTime, value);
    }

    /// <summary>
    /// Whether the diagnostics panel is visible.
    /// </summary>
    public bool IsDiagnosticsVisible
    {
        get => _isDiagnosticsVisible;
        set => SetProperty(ref _isDiagnosticsVisible, value);
    }

    /// <summary>
    /// Whether startup has completed successfully.
    /// </summary>
    public bool IsStartupComplete
    {
        get => _isStartupComplete;
        private set => SetProperty(ref _isStartupComplete, value);
    }

    /// <summary>
    /// Whether startup has failed.
    /// </summary>
    public bool IsStartupFailed
    {
        get => _isStartupFailed;
        private set => SetProperty(ref _isStartupFailed, value);
    }

    /// <summary>
    /// Whether startup is taking longer than expected.
    /// </summary>
    public bool IsSlowStartup
    {
        get => _isSlowStartup;
        private set => SetProperty(ref _isSlowStartup, value);
    }

    /// <summary>
    /// Current status message displayed on the splash screen.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// Collection of diagnostic messages for the diagnostics panel.
    /// </summary>
    public ObservableCollection<string> DiagnosticMessages { get; }

    #endregion

    #region Commands

    public ICommand ToggleDiagnosticsCommand { get; }
    public ICommand ExitCommand { get; }

    #endregion

    #region IStartupLogger Implementation

    public void Info(string message)
    {
        var timestamped = $"[{DateTime.Now:HH:mm:ss.fff}] INFO: {message}";
        Dispatcher.UIThread.Post(() =>
        {
            DiagnosticMessages.Add(timestamped);
            TrimDiagnosticMessages();
        });
    }

    public void Warn(string message)
    {
        var timestamped = $"[{DateTime.Now:HH:mm:ss.fff}] WARN: {message}";
        Dispatcher.UIThread.Post(() =>
        {
            DiagnosticMessages.Add(timestamped);
            TrimDiagnosticMessages();
        });
    }

    public void Error(string message, Exception? ex = null)
    {
        var errorDetails = ex != null ? $"{message} - {ex.GetType().Name}: {ex.Message}" : message;
        var timestamped = $"[{DateTime.Now:HH:mm:ss.fff}] ERROR: {errorDetails}";
        Dispatcher.UIThread.Post(() =>
        {
            DiagnosticMessages.Add(timestamped);
            if (ex != null && !string.IsNullOrEmpty(ex.StackTrace))
            {
                var stackLines = ex.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                if (stackLines.Length > 0)
                {
                    DiagnosticMessages.Add($"  Stack: {stackLines[0].Trim()}");
                }
            }
            TrimDiagnosticMessages();
        });
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Marks the startup as successfully completed.
    /// </summary>
    public void MarkStartupComplete()
    {
        Dispatcher.UIThread.Post(() =>
        {
            _timer.Stop();
            IsStartupComplete = true;
            StatusMessage = "Startup complete!";
            Info("Startup completed successfully");
        });
    }

    /// <summary>
    /// Marks the startup as failed and shows the diagnostics.
    /// </summary>
    public void MarkStartupFailed()
    {
        Dispatcher.UIThread.Post(() =>
        {
            _timer.Stop();
            IsStartupFailed = true;
            IsSlowStartup = false;
            StatusMessage = "Startup failed";
            IsDiagnosticsVisible = true; // Auto-show diagnostics on failure
        });
    }

    /// <summary>
    /// Stops the timer when the window is closing.
    /// </summary>
    public void Cleanup()
    {
        _timer.Stop();
    }

    #endregion

    #region Private Methods

    private void OnTimerTick(object? sender, EventArgs e)
    {
        var elapsed = DateTime.Now - _startTime;
        
        // Update elapsed time display
        ElapsedTime = elapsed.ToString(elapsed.TotalHours >= 1 ? @"hh\:mm\:ss" : @"mm\:ss");
        
        // Check for slow startup
        if (!IsSlowStartup && elapsed.TotalSeconds >= _hangThresholdSeconds)
        {
            IsSlowStartup = true;
            StatusMessage = "Startup is taking longer than expected...";
            Warn($"Startup has exceeded {_hangThresholdSeconds} seconds");
        }
    }

    private void ToggleDiagnostics()
    {
        IsDiagnosticsVisible = !IsDiagnosticsVisible;
    }

    private void Exit()
    {
        // Signal exit request
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    private void TrimDiagnosticMessages()
    {
        // Keep only the last 500 messages to avoid memory issues
        while (DiagnosticMessages.Count > 500)
        {
            DiagnosticMessages.RemoveAt(0);
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// Event raised when the user requests to exit the application.
    /// </summary>
    public event EventHandler? ExitRequested;

    #endregion
}
