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
