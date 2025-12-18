using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Silk.NET.SDL;
using WatchTower.Models;
using GameControllerButton = WatchTower.Models.GameControllerButton;

namespace WatchTower.Services;

/// <summary>
/// SDL2-based game controller service with cross-platform hardware support.
/// Uses Silk.NET.SDL for gamepad polling on Windows, macOS, and Linux.
/// </summary>
public unsafe class GameControllerService : IGameControllerService
{
    private readonly ILogger<GameControllerService> _logger;
    private readonly IConfiguration _configuration;
    private readonly Sdl _sdl;
    private readonly Dictionary<int, IntPtr> _controllers = new();
    private readonly Dictionary<int, GameControllerState> _controllerStates = new();
    private readonly Dictionary<int, Dictionary<GameControllerButton, bool>> _previousButtonStates = new();
    private float _deadZone;
    private bool _initialized;
    private bool _disposed;

    public event EventHandler<GameControllerButtonEventArgs>? ButtonPressed;
    public event EventHandler<GameControllerButtonEventArgs>? ButtonReleased;
    public event EventHandler<GameControllerEventArgs>? ControllerConnected;
    public event EventHandler<GameControllerEventArgs>? ControllerDisconnected;

    public bool IsInitialized => _initialized;

    public IReadOnlyList<GameControllerState> ConnectedControllers => 
        _controllerStates.Values.Where(s => s.IsConnected).ToList();

    public GameControllerService(ILogger<GameControllerService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _sdl = Sdl.GetApi();
        _deadZone = _configuration.GetValue<float>("Gamepad:DeadZone", 0.15f);
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
            _logger.LogInformation("Initializing SDL2 game controller service");
            _logger.LogInformation("Dead zone threshold: {DeadZone:P0}", _deadZone);
            
            // Initialize SDL GameController subsystem
            if (_sdl.Init(Sdl.InitGamecontroller) < 0)
            {
                _logger.LogError("Failed to initialize SDL GameController: {Error}", GetSdlError());
                return false;
            }

            // Scan for connected game controllers
            ScanForControllers();

            _initialized = true;
            _logger.LogInformation("SDL2 game controller service initialized with {Count} gamepad(s)", _controllers.Count);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize SDL2 game controller service");
            return false;
        }
    }

    public void Update()
    {
        if (!_initialized || _disposed)
            return;

        try
        {
            // Process SDL events
            Event evt;
            while (_sdl.PollEvent(&evt) != 0)
            {
                ProcessSdlEvent(evt);
            }

            // Update controller states
            foreach (var kvp in _controllers.ToList())
            {
                UpdateControllerState(kvp.Key, kvp.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating gamepad states");
        }
    }

    public GameControllerState? GetControllerState(int controllerId)
    {
        return _controllerStates.TryGetValue(controllerId, out var state) && state.IsConnected 
            ? state 
            : null;
    }

    private void ScanForControllers()
    {
        int numJoysticks = _sdl.NumJoysticks();
        _logger.LogInformation("Scanning for game controllers, found {Count} joystick(s)", numJoysticks);

        for (int i = 0; i < numJoysticks; i++)
        {
            if (_sdl.IsGameController(i) != 0)
            {
                OpenController(i);
            }
        }
    }

    private void OpenController(int deviceIndex)
    {
        try
        {
            var controller = _sdl.GameControllerOpen(deviceIndex);
            if (controller == null)
            {
                _logger.LogWarning("Failed to open controller {Index}: {Error}", deviceIndex, GetSdlError());
                return;
            }

            var joystick = _sdl.GameControllerGetJoystick(controller);
            int instanceId = _sdl.JoystickInstanceID(joystick);
            string name = System.Text.Encoding.UTF8.GetString(
                System.Runtime.InteropServices.MemoryMarshal.CreateReadOnlySpanFromNullTerminated(
                    _sdl.GameControllerName(controller)));

            _controllers[instanceId] = (IntPtr)controller;
            _controllerStates[instanceId] = new GameControllerState
            {
                ControllerId = instanceId,
                Name = name,
                IsConnected = true
            };
            _previousButtonStates[instanceId] = new Dictionary<GameControllerButton, bool>();

            _logger.LogInformation("Controller connected: {Name} (ID: {Id})", name, instanceId);
            ControllerConnected?.Invoke(this, new GameControllerEventArgs(instanceId, name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening controller {Index}", deviceIndex);
        }
    }

    private void CloseController(int instanceId)
    {
        if (_controllers.TryGetValue(instanceId, out var controllerPtr))
        {
            var state = _controllerStates[instanceId];
            state.IsConnected = false;

            _sdl.GameControllerClose((GameController*)controllerPtr);
            _controllers.Remove(instanceId);
            _previousButtonStates.Remove(instanceId);

            _logger.LogInformation("Controller disconnected: {Name} (ID: {Id})", state.Name, instanceId);
            ControllerDisconnected?.Invoke(this, new GameControllerEventArgs(instanceId, state.Name));
        }
    }

    private void ProcessSdlEvent(Event evt)
    {
        switch ((EventType)evt.Type)
        {
            case EventType.Controllerdeviceadded:
                OpenController(evt.Cdevice.Which);
                break;

            case EventType.Controllerdeviceremoved:
                CloseController(evt.Cdevice.Which);
                break;
        }
    }

    private void UpdateControllerState(int instanceId, IntPtr controllerPtr)
    {
        if (!_controllerStates.TryGetValue(instanceId, out var state))
            return;

        var controller = (GameController*)controllerPtr;

        // Update button states
        var buttonMappings = new Dictionary<GameControllerButton, Silk.NET.SDL.GameControllerButton>
        {
            { GameControllerButton.A, Silk.NET.SDL.GameControllerButton.A },
            { GameControllerButton.B, Silk.NET.SDL.GameControllerButton.B },
            { GameControllerButton.X, Silk.NET.SDL.GameControllerButton.X },
            { GameControllerButton.Y, Silk.NET.SDL.GameControllerButton.Y },
            { GameControllerButton.Back, Silk.NET.SDL.GameControllerButton.Back },
            { GameControllerButton.Start, Silk.NET.SDL.GameControllerButton.Start },
            { GameControllerButton.LeftStick, Silk.NET.SDL.GameControllerButton.Leftstick },
            { GameControllerButton.RightStick, Silk.NET.SDL.GameControllerButton.Rightstick },
            { GameControllerButton.LeftShoulder, Silk.NET.SDL.GameControllerButton.Leftshoulder },
            { GameControllerButton.RightShoulder, Silk.NET.SDL.GameControllerButton.Rightshoulder },
            { GameControllerButton.DPadUp, Silk.NET.SDL.GameControllerButton.DpadUp },
            { GameControllerButton.DPadDown, Silk.NET.SDL.GameControllerButton.DpadDown },
            { GameControllerButton.DPadLeft, Silk.NET.SDL.GameControllerButton.DpadLeft },
            { GameControllerButton.DPadRight, Silk.NET.SDL.GameControllerButton.DpadRight }
        };

        foreach (var mapping in buttonMappings)
        {
            var button = mapping.Key;
            var sdlButton = mapping.Value;
            
            bool isPressed = _sdl.GameControllerGetButton(controller, sdlButton) == 1;
            state.ButtonStates[button] = isPressed;

            // Detect press/release events
            bool wasPrevPressed = _previousButtonStates[instanceId].TryGetValue(button, out var prev) && prev;
            
            if (isPressed && !wasPrevPressed)
            {
                _logger.LogDebug("Button pressed: {Button} on gamepad {Id}", button, instanceId);
                ButtonPressed?.Invoke(this, new GameControllerButtonEventArgs(instanceId, button));
            }
            else if (!isPressed && wasPrevPressed)
            {
                _logger.LogDebug("Button released: {Button} on gamepad {Id}", button, instanceId);
                ButtonReleased?.Invoke(this, new GameControllerButtonEventArgs(instanceId, button));
            }

            _previousButtonStates[instanceId][button] = isPressed;
        }

        // Update guide button
        var guidePressed = _sdl.GameControllerGetButton(controller, Silk.NET.SDL.GameControllerButton.Guide) == 1;
        state.ButtonStates[GameControllerButton.Guide] = guidePressed;
        bool wasGuidePrevPressed = _previousButtonStates[instanceId].TryGetValue(GameControllerButton.Guide, out var guidePrev) && guidePrev;
        if (guidePressed && !wasGuidePrevPressed)
        {
            ButtonPressed?.Invoke(this, new GameControllerButtonEventArgs(instanceId, GameControllerButton.Guide));
        }
        else if (!guidePressed && wasGuidePrevPressed)
        {
            ButtonReleased?.Invoke(this, new GameControllerButtonEventArgs(instanceId, GameControllerButton.Guide));
        }
        _previousButtonStates[instanceId][GameControllerButton.Guide] = guidePressed;

        // Update analog stick states with dead zone processing
        // Note: SDL Y-axis is inverted (positive = down), so we negate for standard convention (positive = up)
        short leftX = _sdl.GameControllerGetAxis(controller, GameControllerAxis.Leftx);
        short leftY = _sdl.GameControllerGetAxis(controller, GameControllerAxis.Lefty);
        short rightX = _sdl.GameControllerGetAxis(controller, GameControllerAxis.Rightx);
        short rightY = _sdl.GameControllerGetAxis(controller, GameControllerAxis.Righty);

        const float maxAxisValue = 32767f;
        var (lx, ly) = ApplyRadialDeadZone(leftX / maxAxisValue, -leftY / maxAxisValue);
        var (rx, ry) = ApplyRadialDeadZone(rightX / maxAxisValue, -rightY / maxAxisValue);

        state.LeftStickX = lx;
        state.LeftStickY = ly;
        state.RightStickX = rx;
        state.RightStickY = ry;

        // Update triggers (0-32767 range, normalize to 0.0-1.0)
        short leftTrigger = _sdl.GameControllerGetAxis(controller, GameControllerAxis.Triggerleft);
        short rightTrigger = _sdl.GameControllerGetAxis(controller, GameControllerAxis.Triggerright);

        state.LeftTrigger = Math.Max(0f, leftTrigger / maxAxisValue);
        state.RightTrigger = Math.Max(0f, rightTrigger / maxAxisValue);
    }

    /// <summary>
    /// Applies radial dead zone processing to analog stick input.
    /// Calculates magnitude, clips to dead zone, and rescales to full range.
    /// </summary>
    private (float x, float y) ApplyRadialDeadZone(float x, float y)
    {
        // Calculate magnitude
        float magnitude = MathF.Sqrt(x * x + y * y);

        // If below dead zone threshold, return zero
        if (magnitude < _deadZone)
        {
            return (0f, 0f);
        }

        // Rescale from dead zone edge to max range
        // Formula: (magnitude - deadZone) / (1.0 - deadZone)
        float normalizedMagnitude = (magnitude - _deadZone) / (1.0f - _deadZone);
        
        // Clamp to [0, 1] range
        normalizedMagnitude = Math.Min(normalizedMagnitude, 1.0f);

        // Scale the vector to maintain direction
        float scale = normalizedMagnitude / magnitude;
        
        return (x * scale, y * scale);
    }

    private string GetSdlError()
    {
        var errorPtr = _sdl.GetError();
        if (errorPtr == null) return "Unknown error";
        return System.Text.Encoding.UTF8.GetString(
            System.Runtime.InteropServices.MemoryMarshal.CreateReadOnlySpanFromNullTerminated(errorPtr));
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Close all controllers
        foreach (var kvp in _controllers.ToList())
        {
            CloseController(kvp.Key);
        }

        if (_initialized)
        {
            _sdl.QuitSubSystem(Sdl.InitGamecontroller);
            _logger.LogInformation("GameControllerService disposed");
        }

        _sdl.Dispose();
    }
}
