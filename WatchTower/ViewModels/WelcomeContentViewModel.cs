using System;
using System.Windows.Input;
using WatchTower.Services;

namespace WatchTower.ViewModels;

/// <summary>
/// ViewModel for the welcome screen shown to first-time users.
/// Displays feature discovery content and keyboard shortcuts.
/// </summary>
public class WelcomeContentViewModel : ViewModelBase
{
    private readonly IUserPreferencesService _userPreferencesService;
    private bool _dontShowAgain;

    public WelcomeContentViewModel(IUserPreferencesService userPreferencesService)
    {
        _userPreferencesService = userPreferencesService ?? throw new ArgumentNullException(nameof(userPreferencesService));
        
        DismissCommand = new RelayCommand(OnDismiss);
    }

    /// <summary>
    /// Gets or sets whether the user has checked "Don't show again".
    /// </summary>
    public bool DontShowAgain
    {
        get => _dontShowAgain;
        set => SetProperty(ref _dontShowAgain, value);
    }

    /// <summary>
    /// Command to dismiss the welcome screen.
    /// </summary>
    public ICommand DismissCommand { get; }

    /// <summary>
    /// Event raised when the welcome screen should be dismissed.
    /// </summary>
    public event EventHandler? WelcomeDismissed;

    private void OnDismiss()
    {
        if (DontShowAgain)
        {
            _userPreferencesService.MarkWelcomeScreenSeen();
        }
        
        WelcomeDismissed?.Invoke(this, EventArgs.Empty);
    }
}
