using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WatchTower.Models;
using WatchTower.Services;

namespace WatchTower.ViewModels;

/// <summary>
/// ViewModel for the main window, demonstrating game controller integration.
/// </summary>
public class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IGameControllerService _gameControllerService;
    private string _statusText = "Game Controller Status: Initializing...";
    private string _lastButtonPressed = "None";
    private int _buttonPressCount = 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string StatusText
    {
        get => _statusText;
        set
        {
            if (_statusText != value)
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }
    }

    public string LastButtonPressed
    {
        get => _lastButtonPressed;
        set
        {
            if (_lastButtonPressed != value)
            {
                _lastButtonPressed = value;
                OnPropertyChanged();
            }
        }
    }

    public int ButtonPressCount
    {
        get => _buttonPressCount;
        set
        {
            if (_buttonPressCount != value)
            {
                _buttonPressCount = value;
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<string> ControllerEvents { get; } = new();

    public MainWindowViewModel(IGameControllerService gameControllerService)
    {
        _gameControllerService = gameControllerService;
        
        // Subscribe to controller events
        _gameControllerService.ButtonPressed += OnButtonPressed;
        _gameControllerService.ButtonReleased += OnButtonReleased;
        _gameControllerService.ControllerConnected += OnControllerConnected;
        _gameControllerService.ControllerDisconnected += OnControllerDisconnected;

        UpdateStatus();
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

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
