using System;
using System.Collections.ObjectModel;
using AdaptiveCards;
using Microsoft.Extensions.Logging;
using WatchTower.Models;
using WatchTower.Services;

namespace WatchTower.ViewModels;

/// <summary>
/// ViewModel for the main window, demonstrating game controller integration and Adaptive Card display.
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

        UpdateStatus();
        
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
}
