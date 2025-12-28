using System;
using AdaptiveCards.Rendering;
using WatchTower.Models;

namespace WatchTower.Services;

/// <summary>
/// Service for managing adaptive card theming with support for dark/light/system themes.
/// </summary>
public interface IAdaptiveCardThemeService
{
    /// <summary>
    /// Gets the current AdaptiveHostConfig based on the active theme.
    /// </summary>
    AdaptiveHostConfig GetHostConfig();

    /// <summary>
    /// Gets the current theme mode setting.
    /// </summary>
    ThemeMode CurrentThemeMode { get; }

    /// <summary>
    /// Gets the resolved theme (Dark or Light) after resolving System theme.
    /// </summary>
    ThemeMode ResolvedTheme { get; }

    /// <summary>
    /// Sets the theme mode and updates the host config accordingly.
    /// </summary>
    /// <param name="themeMode">The theme mode to set.</param>
    void SetTheme(ThemeMode themeMode);

    /// <summary>
    /// Cycles to the next theme mode (Dark -> Light -> System -> Dark).
    /// </summary>
    void CycleTheme();

    /// <summary>
    /// Refreshes the host config, useful when system theme changes.
    /// </summary>
    void RefreshHostConfig();

    /// <summary>
    /// Event raised when the theme changes.
    /// </summary>
    event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
}

/// <summary>
/// Event arguments for theme change events.
/// </summary>
public class ThemeChangedEventArgs : EventArgs
{
    /// <summary>
    /// The new theme mode setting.
    /// </summary>
    public ThemeMode ThemeMode { get; }

    /// <summary>
    /// The resolved theme (Dark or Light) after resolving System theme.
    /// </summary>
    public ThemeMode ResolvedTheme { get; }

    /// <summary>
    /// The updated host config for the new theme.
    /// </summary>
    public AdaptiveHostConfig HostConfig { get; }

    public ThemeChangedEventArgs(ThemeMode themeMode, ThemeMode resolvedTheme, AdaptiveHostConfig hostConfig)
    {
        ThemeMode = themeMode;
        ResolvedTheme = resolvedTheme;
        HostConfig = hostConfig;
    }
}
