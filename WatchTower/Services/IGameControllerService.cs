using System;
using System.Collections.Generic;
using WatchTower.Models;

namespace WatchTower.Services;

/// <summary>
/// Interface for game controller input service.
/// Provides game controller support for navigation and input.
/// </summary>
public interface IGameControllerService : IDisposable
{
    /// <summary>
    /// Event raised when a controller button is pressed.
    /// </summary>
    event EventHandler<GameControllerButtonEventArgs>? ButtonPressed;
    
    /// <summary>
    /// Event raised when a controller button is released.
    /// </summary>
    event EventHandler<GameControllerButtonEventArgs>? ButtonReleased;
    
    /// <summary>
    /// Event raised when a controller is connected.
    /// </summary>
    event EventHandler<GameControllerEventArgs>? ControllerConnected;
    
    /// <summary>
    /// Event raised when a controller is disconnected.
    /// </summary>
    event EventHandler<GameControllerEventArgs>? ControllerDisconnected;
    
    /// <summary>
    /// Gets whether the service is initialized and running.
    /// </summary>
    bool IsInitialized { get; }
    
    /// <summary>
    /// Gets the list of connected controllers.
    /// </summary>
    IReadOnlyList<GameControllerState> ConnectedControllers { get; }
    
    /// <summary>
    /// Initializes the game controller service.
    /// </summary>
    /// <returns>True if initialization succeeded, false otherwise.</returns>
    bool Initialize();
    
    /// <summary>
    /// Updates the controller states. Should be called regularly (e.g., in update loop).
    /// </summary>
    void Update();
    
    /// <summary>
    /// Gets the state of a specific controller.
    /// </summary>
    /// <param name="controllerId">The controller index.</param>
    /// <returns>The controller state, or null if not connected.</returns>
    GameControllerState? GetControllerState(int controllerId);
}

/// <summary>
/// Event arguments for game controller button events.
/// </summary>
public class GameControllerButtonEventArgs : EventArgs
{
    public int ControllerId { get; }
    public GameControllerButton Button { get; }
    
    public GameControllerButtonEventArgs(int controllerId, GameControllerButton button)
    {
        ControllerId = controllerId;
        Button = button;
    }
}

/// <summary>
/// Event arguments for game controller connection events.
/// </summary>
public class GameControllerEventArgs : EventArgs
{
    public int ControllerId { get; }
    public string ControllerName { get; }
    
    public GameControllerEventArgs(int controllerId, string controllerName)
    {
        ControllerId = controllerId;
        ControllerName = controllerName;
    }
}
