using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using AdaptiveCards;
using AdaptiveCards.Rendering;
using AdaptiveCards.Rendering.Avalonia;
using Avalonia.Controls;
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
    private readonly IAdaptiveCardThemeService _themeService;
    private readonly ILogger<MainWindowViewModel> _logger;
    private string _statusText = "Game Controller Status: Initializing...";
    private string _lastButtonPressed = "None";
    private int _buttonPressCount = 0;
    private AdaptiveCard? _currentCard;
    private AdaptiveHostConfig? _hostConfig;
    private Control? _renderedCardControl;
    private RenderedAdaptiveCard? _currentRenderedCard;
    private TypedEventHandler<RenderedAdaptiveCard, AdaptiveActionEventArgs>? _cardActionHandler;
    private InputOverlayMode _currentInputMode = InputOverlayMode.None;
    private string _inputText = string.Empty;
    private string? _renderError;

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
        set
        {
            if (SetProperty(ref _currentCard, value))
            {
                RenderCard();
            }
        }
    }

    /// <summary>
    /// Gets or sets the host config for adaptive card theming.
    /// </summary>
    public AdaptiveHostConfig? HostConfig
    {
        get => _hostConfig;
        set
        {
            if (SetProperty(ref _hostConfig, value))
            {
                RenderCard();
            }
        }
    }

    /// <summary>
    /// Gets the rendered card control for display.
    /// </summary>
    public Control? RenderedCardControl
    {
        get => _renderedCardControl;
        private set => SetProperty(ref _renderedCardControl, value);
    }

    /// <summary>
    /// Gets the error message if card rendering failed, or null if no error.
    /// </summary>
    public string? RenderError
    {
        get => _renderError;
        private set
        {
            if (SetProperty(ref _renderError, value))
            {
                OnPropertyChanged(nameof(HasRenderError));
            }
        }
    }

    /// <summary>
    /// Gets whether there is a render error.
    /// </summary>
    public bool HasRenderError => !string.IsNullOrEmpty(RenderError);

    /// <summary>
    /// Gets the current theme mode display name.
    /// </summary>
    public string CurrentThemeName => _themeService.CurrentThemeMode.ToString();

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

    /// <summary>
    /// Command to cycle through theme modes (Dark -> Light -> System).
    /// </summary>
    public ICommand ToggleThemeCommand { get; }

    /// <summary>
    /// Command to retry rendering the card after a failure.
    /// </summary>
    public ICommand RetryRenderCommand { get; }

    public MainWindowViewModel(
        IGameControllerService gameControllerService, 
        IAdaptiveCardService cardService,
        IAdaptiveCardThemeService themeService,
        ILogger<MainWindowViewModel> logger)
    {
        _gameControllerService = gameControllerService;
        _cardService = cardService;
        _themeService = themeService;
        _logger = logger;
        
        // Subscribe to controller events
        _gameControllerService.ButtonPressed += OnButtonPressed;
        _gameControllerService.ButtonReleased += OnButtonReleased;
        _gameControllerService.ControllerConnected += OnControllerConnected;
        _gameControllerService.ControllerDisconnected += OnControllerDisconnected;

        // Subscribe to theme changes
        _themeService.ThemeChanged += OnThemeChanged;

        // Subscribe to card action events
        _cardService.ActionInvoked += OnCardActionInvoked;
        _cardService.SubmitAction += OnCardSubmit;
        _cardService.OpenUrlAction += OnCardOpenUrl;
        _cardService.ExecuteAction += OnCardExecute;
        _cardService.ShowCardAction += OnCardShowCard;

        // Initialize commands
        ShowRichTextInputCommand = new RelayCommand(ShowRichTextInput);
        ShowVoiceInputCommand = new RelayCommand(ShowVoiceInput);
        CloseOverlayCommand = new RelayCommand(CloseOverlay);
        SubmitInputCommand = new RelayCommand(SubmitInput, CanSubmitInput);
        ToggleEventLogCommand = new RelayCommand(ToggleEventLog);
        ToggleThemeCommand = new RelayCommand(ToggleTheme);
        RetryRenderCommand = new RelayCommand(RetryRender);

        UpdateStatus();
        
        // Initialize host config BEFORE loading the card
        HostConfig = _themeService.GetHostConfig();
        _logger.LogInformation("HostConfig initialized: {IsNull}", HostConfig == null ? "NULL" : "NOT NULL");
        if (HostConfig != null)
        {
            var bgColor = HostConfig.ContainerStyles?.Default?.BackgroundColor ?? "null";
            _logger.LogInformation("HostConfig background color: {BgColor}", bgColor);
        }
        
        // Load the sample card (RenderCard will be called automatically)
        LoadSampleCard();
    }

    private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
    {
        HostConfig = e.HostConfig;
        OnPropertyChanged(nameof(CurrentThemeName));
        _logger.LogInformation("Theme changed to {ThemeMode} (resolved: {ResolvedTheme})", e.ThemeMode, e.ResolvedTheme);
        // RenderCard() will be called automatically by the HostConfig setter
    }

    /// <summary>
    /// Renders the current card using the AdaptiveCardRenderer with the current HostConfig.
    /// </summary>
    private void RenderCard()
    {
        if (CurrentCard == null)
        {
            _logger.LogDebug("RenderCard: CurrentCard is null, clearing rendered control");
            RenderedCardControl = null;
            _currentRenderedCard = null;
            RenderError = null;
            return;
        }

        if (HostConfig == null)
        {
            _logger.LogWarning("RenderCard: HostConfig is null, cannot render card");
            RenderedCardControl = null;
            _currentRenderedCard = null;
            RenderError = "Unable to render card: Host configuration is not available.";
            return;
        }

        try
        {
            var bgColor = HostConfig.ContainerStyles?.Default?.BackgroundColor ?? "null";
            var fgColor = HostConfig.ContainerStyles?.Default?.ForegroundColors?.Default?.Default ?? "null";
            _logger.LogInformation("Rendering card with HostConfig - BG: {BgColor}, FG: {FgColor}", bgColor, fgColor);

            // Unsubscribe from previous rendered card to avoid memory leak
            if (_currentRenderedCard != null && _cardActionHandler != null)
            {
                _currentRenderedCard.OnAction -= _cardActionHandler;
            }

            var renderer = new AdaptiveCardRenderer(HostConfig);
            var renderedCard = renderer.RenderCard(CurrentCard);
            
            // Wire up action handler with a reference we can unsubscribe
            _cardActionHandler = (sender, e) =>
            {
                // Convert JObject to Dictionary<string, object>
                var inputJson = renderedCard.UserInputs.AsJson();
                var inputDict = inputJson?.ToObject<System.Collections.Generic.Dictionary<string, object>>();
                _cardService.HandleAction(e.Action, inputDict);
            };
            renderedCard.OnAction += _cardActionHandler;
            _currentRenderedCard = renderedCard;

            RenderedCardControl = renderedCard.Control;
            RenderError = null; // Clear any previous error
            _logger.LogInformation("Card rendered successfully with {WarningCount} warnings", renderedCard.Warnings.Count);
            
            // Log any warnings
            foreach (var warning in renderedCard.Warnings)
            {
                _logger.LogWarning("Card render warning: {Message}", warning.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render adaptive card");
            RenderedCardControl = null;
            RenderError = "Failed to render card. Please check the logs for more details.";
        }
    }

    private void ToggleTheme()
    {
        _themeService.CycleTheme();
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

    private void RetryRender()
    {
        // Only retry if prerequisites are available; otherwise, report a clearer message.
        if (CurrentCard is null || HostConfig is null)
        {
            var reason = CurrentCard is null && HostConfig is null
                ? "card and configuration are still unavailable"
                : CurrentCard is null
                    ? "card is still unavailable"
                    : "configuration is still unavailable";

            _logger.LogWarning("Cannot retry render: {Reason}", reason);
            AddEvent($"Cannot retry: {reason}");
            return;
        }
        // Only retry if prerequisites are available; otherwise, report a clearer message.
        if (CurrentCard is null || HostConfig is null)
        {
            var reason = CurrentCard is null && HostConfig is null
                ? "card and configuration are still unavailable"
                : CurrentCard is null
                    ? "card is still unavailable"
                    : "configuration is still unavailable";

            _logger.LogWarning("Cannot retry render: {Reason}", reason);
            RenderError = $"Cannot retry: {reason}.";
            AddEvent($"Cannot retry: {reason}");
            return;
        }

        _logger.LogInformation("Retrying card render after error");
        RenderCard();
    }

    #region Card Action Handlers

    private void OnCardActionInvoked(object? sender, AdaptiveCardActionEventArgs e)
    {
        _logger.LogDebug("Card action invoked: {ActionType}", e.Action.GetType().Name);
        AddEvent($"Card Action: {e.Action.GetType().Name.Replace("Adaptive", "").Replace("Action", "")}");
    }

    private void OnCardSubmit(object? sender, AdaptiveCardSubmitEventArgs e)
    {
        _logger.LogInformation("Card submitted with {Count} inputs", e.InputValues.Count);
        
        foreach (var kvp in e.InputValues)
        {
            _logger.LogDebug("Input: {Key} = {Value}", kvp.Key, kvp.Value);
        }

        // TODO: Process submitted data based on card context
        AddEvent($"Card Submit: {e.InputValues.Count} input(s)");
    }

    private void OnCardOpenUrl(object? sender, AdaptiveCardActionEventArgs e)
    {
        if (e.Action is AdaptiveOpenUrlAction openUrlAction)
        {
            _logger.LogInformation("Opening URL: {Url}", openUrlAction.Url);
            
            // Open URL in default browser
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = openUrlAction.Url.ToString(),
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
                AddEvent($"Opened URL: {openUrlAction.Url}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open URL: {Url}", openUrlAction.Url);
                AddEvent($"Failed to open URL: {openUrlAction.Url}");
            }
        }
    }

    private void OnCardExecute(object? sender, AdaptiveCardActionEventArgs e)
    {
        if (e.Action is AdaptiveExecuteAction executeAction)
        {
            _logger.LogInformation("Executing action verb: {Verb}", executeAction.Verb);
            
            // TODO: Handle execute actions based on verb
            AddEvent($"Execute: {executeAction.Verb}");
        }
    }

    private void OnCardShowCard(object? sender, AdaptiveCardActionEventArgs e)
    {
        if (e.Action is AdaptiveShowCardAction showCardAction)
        {
            _logger.LogInformation("Show card action triggered");
            
            // ShowCard is typically handled inline by the renderer
            // This event is for custom handling if needed
            AddEvent("ShowCard triggered");
        }
    }

    #endregion
}
