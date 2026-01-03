using System;
using System.Text.Json.Serialization;

namespace WatchTower.Models;

/// <summary>
/// User preferences that persist across application sessions.
/// </summary>
public class UserPreferences
{
    /// <summary>
    /// The selected theme mode for adaptive cards.
    /// </summary>
    [JsonPropertyName("themeMode")]
    public ThemeMode ThemeMode { get; set; } = ThemeMode.System;

    /// <summary>
    /// Optional font family overrides for adaptive card rendering.
    /// </summary>
    [JsonPropertyName("fontOverrides")]
    public FontOverrides? FontOverrides { get; set; }

    /// <summary>
    /// Indicates whether this is the first time the application is being run.
    /// </summary>
    [JsonPropertyName("isFirstRun")]
    public bool IsFirstRun { get; set; } = true;

    /// <summary>
    /// Indicates whether the user has seen the welcome screen.
    /// </summary>
    [JsonPropertyName("hasSeenWelcomeScreen")]
    public bool HasSeenWelcomeScreen { get; set; } = false;

    /// <summary>
    /// The date and time when the user dismissed the welcome screen, if ever.
    /// </summary>
    [JsonPropertyName("welcomeScreenDismissedDate")]
    public DateTime? WelcomeScreenDismissedDate { get; set; }

    /// <summary>
    /// Window position and display preferences.
    /// </summary>
    [JsonPropertyName("windowPosition")]
    public WindowPositionPreferences? WindowPosition { get; set; }
}

/// <summary>
/// Font family overrides for adaptive card rendering.
/// </summary>
public class FontOverrides
{
    /// <summary>
    /// Primary font family for body text.
    /// </summary>
    [JsonPropertyName("defaultFontFamily")]
    public string? DefaultFontFamily { get; set; }

    /// <summary>
    /// Font family for monospace/code text.
    /// </summary>
    [JsonPropertyName("monospaceFontFamily")]
    public string? MonospaceFontFamily { get; set; }
}

/// <summary>
/// Window position and display preferences.
/// </summary>
public class WindowPositionPreferences
{
    /// <summary>
    /// X position of the window in logical coordinates.
    /// </summary>
    [JsonPropertyName("x")]
    public double X { get; set; }

    /// <summary>
    /// Y position of the window in logical coordinates.
    /// </summary>
    [JsonPropertyName("y")]
    public double Y { get; set; }

    /// <summary>
    /// Width of the window in logical pixels.
    /// </summary>
    [JsonPropertyName("width")]
    public double Width { get; set; }

    /// <summary>
    /// Height of the window in logical pixels.
    /// </summary>
    [JsonPropertyName("height")]
    public double Height { get; set; }

    /// <summary>
    /// Bounds of the display where the window was shown, in physical pixels, used for display identification. Window position coordinates above are in logical pixels.
    /// </summary>
    [JsonPropertyName("displayBounds")]
    public DisplayBounds? DisplayBounds { get; set; }

    /// <summary>
    /// Creates a deep copy of this instance.
    /// </summary>
    public WindowPositionPreferences Clone()
    {
        return new WindowPositionPreferences
        {
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
            DisplayBounds = DisplayBounds?.Clone()
        };
    }
}

/// <summary>
/// Display identification information.
/// </summary>
public class DisplayBounds
{
    /// <summary>
    /// X coordinate of display in physical pixels.
    /// </summary>
    [JsonPropertyName("x")]
    public int X { get; set; }

    /// <summary>
    /// Y coordinate of display in physical pixels.
    /// </summary>
    [JsonPropertyName("y")]
    public int Y { get; set; }

    /// <summary>
    /// Width of display in physical pixels.
    /// </summary>
    [JsonPropertyName("width")]
    public int Width { get; set; }

    /// <summary>
    /// Height of display in physical pixels.
    /// </summary>
    [JsonPropertyName("height")]
    public int Height { get; set; }

    /// <summary>
    /// Creates a deep copy of this instance.
    /// </summary>
    public DisplayBounds Clone()
    {
        return new DisplayBounds
        {
            X = X,
            Y = Y,
            Width = Width,
            Height = Height
        };
    }
}
