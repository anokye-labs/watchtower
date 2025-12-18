using System.Collections.Generic;

namespace WatchTower.Models;

/// <summary>
/// Represents the current state of a game controller.
/// </summary>
public class GameControllerState
{
    /// <summary>
    /// Gets or sets the controller index/ID.
    /// </summary>
    public int ControllerId { get; set; }
    
    /// <summary>
    /// Gets or sets the controller name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets whether the controller is connected.
    /// </summary>
    public bool IsConnected { get; set; }
    
    /// <summary>
    /// Gets or sets the button states.
    /// Key: Button enum, Value: true if pressed, false if released.
    /// </summary>
    public Dictionary<GameControllerButton, bool> ButtonStates { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the left stick X-axis value (-1.0 to 1.0).
    /// </summary>
    public float LeftStickX { get; set; }
    
    /// <summary>
    /// Gets or sets the left stick Y-axis value (-1.0 to 1.0).
    /// </summary>
    public float LeftStickY { get; set; }
    
    /// <summary>
    /// Gets or sets the right stick X-axis value (-1.0 to 1.0).
    /// </summary>
    public float RightStickX { get; set; }
    
    /// <summary>
    /// Gets or sets the right stick Y-axis value (-1.0 to 1.0).
    /// </summary>
    public float RightStickY { get; set; }
    
    /// <summary>
    /// Gets or sets the left trigger value (0.0 to 1.0).
    /// </summary>
    public float LeftTrigger { get; set; }
    
    /// <summary>
    /// Gets or sets the right trigger value (0.0 to 1.0).
    /// </summary>
    public float RightTrigger { get; set; }
}
