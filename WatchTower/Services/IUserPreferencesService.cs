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
    /// Gets whether the user has seen the welcome screen.
    /// </summary>
    bool GetHasSeenWelcomeScreen();

    /// <summary>
    /// Sets whether the user has seen the welcome screen and persists it.
    /// </summary>
    /// <param name="hasSeenWelcomeScreen">Whether the user has seen the welcome screen.</param>
    void SetHasSeenWelcomeScreen(bool hasSeenWelcomeScreen);

    /// <summary>
    /// Gets whether to show the welcome screen on startup.
    /// </summary>
    bool GetShowWelcomeOnStartup();

    /// <summary>
    /// Sets whether to show the welcome screen on startup and persists it.
    /// </summary>
    /// <param name="showWelcomeOnStartup">Whether to show the welcome screen on startup.</param>
    void SetShowWelcomeOnStartup(bool showWelcomeOnStartup);

    /// <summary>
    /// Gets the date when the application was first run, or null if not set.
    /// FirstRunDate is automatically set on first load and is read-only thereafter.
    /// </summary>
    DateTime? GetFirstRunDate();

    /// <summary>
    /// Event raised when preferences change.
    /// </summary>
    event EventHandler<UserPreferences>? PreferencesChanged;
}
