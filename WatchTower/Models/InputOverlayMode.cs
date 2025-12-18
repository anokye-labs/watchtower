namespace WatchTower.Models;

/// <summary>
/// Defines the type of input overlay to display.
/// </summary>
public enum InputOverlayMode
{
    /// <summary>
    /// No overlay is shown.
    /// </summary>
    None,
    
    /// <summary>
    /// Rich-text input overlay is shown.
    /// </summary>
    RichText,
    
    /// <summary>
    /// Voice-based input overlay is shown.
    /// </summary>
    Voice
}
