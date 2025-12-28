namespace WatchTower.Models;

/// <summary>
/// Specifies the theme mode for adaptive card rendering.
/// </summary>
public enum ThemeMode
{
    /// <summary>
    /// Dark theme with void black background and light text.
    /// </summary>
    Dark,

    /// <summary>
    /// Light theme with light background and dark text.
    /// </summary>
    Light,

    /// <summary>
    /// Automatically detect theme from system settings.
    /// </summary>
    System
}
