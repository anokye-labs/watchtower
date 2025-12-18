using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using WatchTower.Models;

namespace WatchTower.Services;

/// <summary>
/// Base implementation of game controller service with polling support.
/// This is a foundational implementation that can be extended with platform-specific backends.
/// Currently provides a mock implementation for development and testing.
/// </summary>
public class GameControllerService : IGameControllerService
{
    private readonly ILogger<GameControllerService> _logger;
    private readonly Dictionary<int, GameControllerState> _controllerStates = new();
    private readonly Dictionary<int, Dictionary<GameControllerButton, bool>> _previousButtonStates = new();
    private Timer? _updateTimer;
    private bool _initialized;
    private bool _disposed;

    public event EventHandler<GameControllerButtonEventArgs>? ButtonPressed;
    public event EventHandler<GameControllerButtonEventArgs>? ButtonReleased;
    public event EventHandler<GameControllerEventArgs>? ControllerConnected;
    public event EventHandler<GameControllerEventArgs>? ControllerDisconnected;

    public bool IsInitialized => _initialized;

    public IReadOnlyList<GameControllerState> ConnectedControllers => 
        _controllerStates.Values.Where(s => s.IsConnected).ToList();

    public GameControllerService(ILogger<GameControllerService> logger)
    {
        _logger = logger;
    }

    public bool Initialize()
    {
        if (_initialized)
        {
            _logger.LogWarning("GameControllerService already initialized");
            return true;
        }

        try
        {
            _logger.LogInformation("Initializing game controller service");
            
            // TODO: Add platform-specific controller detection
            // For now, this is a mock implementation that logs availability
            _logger.LogInformation("Game controller service initialized (mock implementation)");
            _logger.LogInformation("To enable full game controller support, platform-specific backend implementation is required");
            _logger.LogInformation("Supported actions: Button mapping, D-pad navigation, trigger support");

            _initialized = true;
            
            // Start update timer (30 Hz polling - adequate for mock implementation)
            _updateTimer = new Timer(OnTimerUpdate, null, TimeSpan.FromMilliseconds(33), TimeSpan.FromMilliseconds(33));
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during GameControllerService initialization");
            return false;
        }
    }

    public void Update()
    {
        if (!_initialized || _disposed)
            return;

        try
        {
            // Platform-specific controller polling would go here
            // For now, this is a no-op in the mock implementation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating game controller states");
        }
    }

    public GameControllerState? GetControllerState(int controllerId)
    {
        return _controllerStates.TryGetValue(controllerId, out var state) && state.IsConnected 
            ? state 
            : null;
    }

    private void OnTimerUpdate(object? state)
    {
        Update();
    }

    /// <summary>
    /// Simulates a button press for testing purposes.
    /// In a full implementation, this would be called by the platform-specific backend.
    /// </summary>
    internal void SimulateButtonPress(int controllerId, GameControllerButton button)
    {
        if (!_controllerStates.ContainsKey(controllerId))
        {
            // Auto-create a mock controller for testing
            AddMockController(controllerId);
        }

        var state = _controllerStates[controllerId];
        state.ButtonStates[button] = true;
        
        bool wasPrevPressed = _previousButtonStates[controllerId].TryGetValue(button, out var prev) && prev;
        if (!wasPrevPressed)
        {
            _logger.LogDebug("Button pressed: {Button} on controller {Id}", button, controllerId);
            ButtonPressed?.Invoke(this, new GameControllerButtonEventArgs(controllerId, button));
            _previousButtonStates[controllerId][button] = true;
        }
    }

    /// <summary>
    /// Simulates a button release for testing purposes.
    /// In a full implementation, this would be called by the platform-specific backend.
    /// </summary>
    internal void SimulateButtonRelease(int controllerId, GameControllerButton button)
    {
        if (!_controllerStates.ContainsKey(controllerId))
            return;

        var state = _controllerStates[controllerId];
        state.ButtonStates[button] = false;
        
        bool wasPrevPressed = _previousButtonStates[controllerId].TryGetValue(button, out var prev) && prev;
        if (wasPrevPressed)
        {
            _logger.LogDebug("Button released: {Button} on controller {Id}", button, controllerId);
            ButtonReleased?.Invoke(this, new GameControllerButtonEventArgs(controllerId, button));
            _previousButtonStates[controllerId][button] = false;
        }
    }

    private void AddMockController(int controllerId)
    {
        string name = $"Mock Controller {controllerId}";
        _controllerStates[controllerId] = new GameControllerState
        {
            ControllerId = controllerId,
            Name = name,
            IsConnected = true
        };
        _previousButtonStates[controllerId] = new Dictionary<GameControllerButton, bool>();

        _logger.LogInformation("Mock controller connected: {Name} (ID: {Id})", name, controllerId);
        ControllerConnected?.Invoke(this, new GameControllerEventArgs(controllerId, name));
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _updateTimer?.Dispose();
        _updateTimer = null;

        // Disconnect all controllers
        foreach (var kvp in _controllerStates.ToList())
        {
            var state = kvp.Value;
            state.IsConnected = false;
            _logger.LogInformation("Controller disconnected: {Name} (ID: {Id})", state.Name, state.ControllerId);
            ControllerDisconnected?.Invoke(this, new GameControllerEventArgs(state.ControllerId, state.Name));
        }

        _controllerStates.Clear();
        _previousButtonStates.Clear();

        if (_initialized)
        {
            _logger.LogInformation("GameControllerService disposed");
        }
    }
}
