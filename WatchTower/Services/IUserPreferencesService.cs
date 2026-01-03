using System;
using WatchTower.Models;

namespace WatchTower.Services;

/// <summary>
/// Service for managing user preferences that persist across application sessions.
/// </summary>
public interface IUserPreferencesService
{
    /// <summary>
    /// Gets the current user preferences.
    /// </summary>
    UserPreferences GetPreferences();

    /// <summary>
    /// Saves the user preferences to persistent storage.
    /// </summary>
    /// <param name="preferences">The preferences to save.</param>
    void SavePreferences(UserPreferences preferences);

    /// <summary>
    /// Gets the current theme mode preference.
    /// </summary>
    ThemeMode GetThemeMode();

    /// <summary>
    /// Sets the theme mode preference and persists it.
    /// </summary>
    /// <param name="themeMode">The theme mode to set.</param>
    void SetThemeMode(ThemeMode themeMode);

    /// <summary>
    /// Gets the font overrides if configured.
    /// </summary>
    FontOverrides? GetFontOverrides();

    /// <summary>
    /// Sets the font overrides and persists them.
    /// </summary>
    /// <param name="fontOverrides">The font overrides to set, or null to clear.</param>
    void SetFontOverrides(FontOverrides? fontOverrides);

    /// <summary>
    /// Gets the window position preferences if configured.
    /// </summary>
    WindowPositionPreferences? GetWindowPosition();

    /// <summary>
    /// Sets the window position preferences and persists them.
    /// </summary>
    /// <param name="windowPosition">The window position preferences to set, or null to clear.</param>
    void SetWindowPosition(WindowPositionPreferences? windowPosition);

    /// <summary>
    /// Event raised when preferences change.
    /// </summary>
    event EventHandler<UserPreferences>? PreferencesChanged;
}
