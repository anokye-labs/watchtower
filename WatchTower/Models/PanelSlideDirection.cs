namespace WatchTower.Models;

/// <summary>
/// Defines the direction from which a panel slides in/out.
/// Used to determine which edge of the frame should be visible.
/// </summary>
public enum PanelSlideDirection
{
    /// <summary>
    /// Panel slides from the left edge (right edge of frame visible).
    /// </summary>
    Left,
    
    /// <summary>
    /// Panel slides from the bottom edge (top edge of frame visible).
    /// </summary>
    Bottom,
    
    /// <summary>
    /// Panel slides from the right edge (left edge of frame visible).
    /// </summary>
    Right,
    
    /// <summary>
    /// Panel slides from the top edge (bottom edge of frame visible).
    /// </summary>
    Top
}
