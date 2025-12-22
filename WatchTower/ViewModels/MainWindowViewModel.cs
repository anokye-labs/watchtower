using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using AdaptiveCards;
using Microsoft.Extensions.Logging;
using WatchTower.Models;
using WatchTower.Services;

namespace WatchTower.ViewModels;

/// <summary>
/// ViewModel for the main application window.
/// Manages the input overlay state, game controller integration, and Adaptive Card display.
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    private readonly IGameControllerService _gameControllerService;
    private readonly IAdaptiveCardService _cardService;
    private readonly ILogger<MainWindowViewModel> _logger;
    private string _statusText = "Game Controller Status: Initializing...";
    private string _lastButtonPressed = "None";
    private int _buttonPressCount = 0;
    private AdaptiveCard? _currentCard;
    private InputOverlayMode _currentInputMode = InputOverlayMode.None;
    private string _inputText = string.Empty;
    private AdaptiveHostConfig? _hostConfig;

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string LastButtonPressed
    {
        get => _lastButtonPressed;
        set => SetProperty(ref _lastButtonPressed, value);
    }

    public int ButtonPressCount
    {
        get => _buttonPressCount;
        set => SetProperty(ref _buttonPressCount, value);
    }

    public ObservableCollection<string> ControllerEvents { get; } = new();

    /// <summary>
    /// Gets or sets the current Adaptive Card to display.
    /// </summary>
    public AdaptiveCard? CurrentCard
    {
        get => _currentCard;
        set => SetProperty(ref _currentCard, value);
    }

    /// <summary>
    /// Gets or sets the HostConfig for theming the Adaptive Card.
    /// </summary>
    public AdaptiveHostConfig? HostConfig
    {
        get => _hostConfig;
        set => SetProperty(ref _hostConfig, value);
    }

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
                OnPropertyChanged(nameof(IsEventLogVisible));
                OnPropertyChanged(nameof(IsInputOverlayVisible));
            }
        }
    }

    /// <summary>
    /// Gets or sets the text entered in the rich-text input.
    /// </summary>
    public string InputText
    {
        get => _inputText;
        set
        {
            if (SetProperty(ref _inputText, value))
            {
                // Notify Submit command that CanExecute may have changed
                SubmitInputCommand.RaiseCanExecuteChanged();
            }
        }
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
    /// Gets whether the event log overlay is visible.
    /// </summary>
    public bool IsEventLogVisible => CurrentInputMode == InputOverlayMode.EventLog;

    /// <summary>
    /// Gets whether the input overlay (Rich Text or Voice) is visible.
    /// Used to control the bottom-sliding panel visibility.
    /// </summary>
    public bool IsInputOverlayVisible => CurrentInputMode == InputOverlayMode.RichText || CurrentInputMode == InputOverlayMode.Voice;

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
    public RelayCommand SubmitInputCommand { get; }

    /// <summary>
    /// Command to toggle the event log overlay.
    /// </summary>
    public ICommand ToggleEventLogCommand { get; }

    public MainWindowViewModel(
        IGameControllerService gameControllerService, 
        IAdaptiveCardService cardService, 
        ILogger<MainWindowViewModel> logger)
    {
        _gameControllerService = gameControllerService;
        _cardService = cardService;
        _logger = logger;
        
        // Subscribe to controller events
        _gameControllerService.ButtonPressed += OnButtonPressed;
        _gameControllerService.ButtonReleased += OnButtonReleased;
        _gameControllerService.ControllerConnected += OnControllerConnected;
        _gameControllerService.ControllerDisconnected += OnControllerDisconnected;

        // Initialize commands
        ShowRichTextInputCommand = new RelayCommand(ShowRichTextInput);
        ShowVoiceInputCommand = new RelayCommand(ShowVoiceInput);
        CloseOverlayCommand = new RelayCommand(CloseOverlay);
        SubmitInputCommand = new RelayCommand(SubmitInput, CanSubmitInput);
        ToggleEventLogCommand = new RelayCommand(ToggleEventLog);

        UpdateStatus();
        
        // Configure dark theme for Adaptive Cards
        HostConfig = _cardService.CreateDarkHostConfig();
        
        // Load the sample card on initialization
        LoadSampleCard();
    }

    private void OnButtonPressed(object? sender, GameControllerButtonEventArgs e)
    {
        LastButtonPressed = e.Button.ToString();
        ButtonPressCount++;
        AddEvent($"Button Pressed: {e.Button} on Controller {e.ControllerId}");
    }

    private void OnButtonReleased(object? sender, GameControllerButtonEventArgs e)
    {
        AddEvent($"Button Released: {e.Button} on Controller {e.ControllerId}");
    }

    private void OnControllerConnected(object? sender, GameControllerEventArgs e)
    {
        AddEvent($"Controller Connected: {e.ControllerName} (ID: {e.ControllerId})");
        UpdateStatus();
    }

    private void OnControllerDisconnected(object? sender, GameControllerEventArgs e)
    {
        AddEvent($"Controller Disconnected: {e.ControllerName} (ID: {e.ControllerId})");
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        int controllerCount = _gameControllerService.ConnectedControllers.Count;
        StatusText = controllerCount > 0
            ? $"Game Controllers Connected: {controllerCount}"
            : "Game Controller Status: No controllers connected (mock mode)";
    }

    private void AddEvent(string eventText)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        ControllerEvents.Insert(0, $"[{timestamp}] {eventText}");
        
        // Keep only last 20 events
        while (ControllerEvents.Count > 20)
        {
            ControllerEvents.RemoveAt(ControllerEvents.Count - 1);
        }
    }

    /// <summary>
    /// Loads a sample Adaptive Card for display.
    /// </summary>
    private void LoadSampleCard()
    {
        _logger.LogInformation("Loading sample Adaptive Card");
        CurrentCard = _cardService.CreateSampleCard();
    }

    /// <summary>
    /// Loads an Adaptive Card from JSON string.
    /// </summary>
    /// <param name="cardJson">The JSON representation of the card.</param>
    public void LoadCardFromJson(string cardJson)
    {
        _logger.LogInformation("Loading Adaptive Card from JSON");
        var card = _cardService.LoadCardFromJson(cardJson);
        if (card != null)
        {
            CurrentCard = card;
        }
        else
        {
            _logger.LogWarning("Failed to load card from JSON");
        }
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
        var previousMode = CurrentInputMode;
        CurrentInputMode = InputOverlayMode.None;
        
        // Only clear input text for input modes (not EventLog)
        if (previousMode == InputOverlayMode.RichText || previousMode == InputOverlayMode.Voice)
        {
            InputText = string.Empty;
        }
    }

    private bool CanSubmitInput()
    {
        // Only allow submission if InputText is not empty (for Rich Text mode)
        return !string.IsNullOrWhiteSpace(InputText);
    }

    private void SubmitInput()
    {
        // TODO: Process the input (will be implemented in future features)
        // For now, just close the overlay after submission
        CloseOverlay();
    }

    private void ToggleEventLog()
    {
        CurrentInputMode = CurrentInputMode == InputOverlayMode.EventLog
            ? InputOverlayMode.None
            : InputOverlayMode.EventLog;
    }
}
