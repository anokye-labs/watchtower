using System.Windows.Input;
using WatchTower.Models;

namespace WatchTower.ViewModels;

/// <summary>
/// ViewModel for the main application window.
/// Manages the input overlay state and visibility.
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    private InputOverlayMode _currentInputMode = InputOverlayMode.None;
    private string _inputText = string.Empty;

    /// <summary>
    /// Gets or sets the current input overlay mode.
    /// </summary>
    public InputOverlayMode CurrentInputMode
    {
        get => _currentInputMode;
        set
        {
            if (SetProperty(ref _currentInputMode, value))
            {
                OnPropertyChanged(nameof(IsOverlayVisible));
                OnPropertyChanged(nameof(IsRichTextMode));
                OnPropertyChanged(nameof(IsVoiceMode));
            }
        }
    }

    /// <summary>
    /// Gets or sets the text entered in the rich-text input.
    /// </summary>
    public string InputText
    {
        get => _inputText;
        set => SetProperty(ref _inputText, value);
    }

    /// <summary>
    /// Gets whether any overlay is currently visible.
    /// </summary>
    public bool IsOverlayVisible => CurrentInputMode != InputOverlayMode.None;

    /// <summary>
    /// Gets whether the rich-text input mode is active.
    /// </summary>
    public bool IsRichTextMode => CurrentInputMode == InputOverlayMode.RichText;

    /// <summary>
    /// Gets whether the voice input mode is active.
    /// </summary>
    public bool IsVoiceMode => CurrentInputMode == InputOverlayMode.Voice;

    /// <summary>
    /// Command to show the rich-text input overlay.
    /// </summary>
    public ICommand ShowRichTextInputCommand { get; }

    /// <summary>
    /// Command to show the voice input overlay.
    /// </summary>
    public ICommand ShowVoiceInputCommand { get; }

    /// <summary>
    /// Command to close the input overlay.
    /// </summary>
    public ICommand CloseOverlayCommand { get; }

    /// <summary>
    /// Command to submit the input.
    /// </summary>
    public ICommand SubmitInputCommand { get; }

    public MainWindowViewModel()
    {
        ShowRichTextInputCommand = new RelayCommand(ShowRichTextInput);
        ShowVoiceInputCommand = new RelayCommand(ShowVoiceInput);
        CloseOverlayCommand = new RelayCommand(CloseOverlay);
        SubmitInputCommand = new RelayCommand(SubmitInput);
    }

    private void ShowRichTextInput()
    {
        InputText = string.Empty;
        CurrentInputMode = InputOverlayMode.RichText;
    }

    private void ShowVoiceInput()
    {
        CurrentInputMode = InputOverlayMode.Voice;
    }

    private void CloseOverlay()
    {
        CurrentInputMode = InputOverlayMode.None;
        InputText = string.Empty;
    }

    private void SubmitInput()
    {
        // TODO: Process the input (will be implemented in future features)
        // For now, just close the overlay after submission
        CloseOverlay();
    }
}
