using AdaptiveCards.Rendering;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Styling;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using WatchTower.Models;

namespace WatchTower.Services;

/// <summary>
/// Service for managing adaptive card theming with embedded JSON theme files.
/// </summary>
public class AdaptiveCardThemeService : IAdaptiveCardThemeService
{
    private readonly ILogger<AdaptiveCardThemeService> _logger;
    private readonly IUserPreferencesService _userPreferencesService;

    private AdaptiveHostConfig? _darkHostConfigOriginal;
    private AdaptiveHostConfig? _lightHostConfigOriginal;
    private AdaptiveHostConfig? _darkHostConfig;
    private AdaptiveHostConfig? _lightHostConfig;
    private AdaptiveHostConfig? _currentHostConfig;
    private bool _disposed;

    private const string DarkThemePath = "avares://WatchTower/Assets/Themes/ancestral-futurism-dark.json";
    private const string LightThemePath = "avares://WatchTower/Assets/Themes/ancestral-futurism-light.json";

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    public ThemeMode CurrentThemeMode { get; private set; }
    public ThemeMode ResolvedTheme { get; private set; }

    public AdaptiveCardThemeService(
        ILogger<AdaptiveCardThemeService> logger,
        IUserPreferencesService userPreferencesService)
    {
        _logger = logger;
        _userPreferencesService = userPreferencesService;

        // Load theme configs from embedded resources
        LoadThemeConfigs();

        // Initialize with user's preferred theme
        CurrentThemeMode = _userPreferencesService.GetThemeMode();
        ResolveAndApplyTheme();

        // Subscribe to system theme changes
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnSystemThemeChanged;
        }
    }

    public AdaptiveHostConfig GetHostConfig()
    {
        var config = _currentHostConfig ?? CreateFallbackHostConfig();
        var bgColor = config.ContainerStyles?.Default?.BackgroundColor ?? "null";
        var fgColor = config.ContainerStyles?.Default?.ForegroundColors?.Default?.Default ?? "null";
        _logger.LogDebug("GetHostConfig called - BG: {BgColor}, FG: {FgColor}", bgColor, fgColor);
        return config;
    }

    public void SetTheme(ThemeMode themeMode)
    {
        if (CurrentThemeMode == themeMode)
            return;

        CurrentThemeMode = themeMode;
        _userPreferencesService.SetThemeMode(themeMode);
        ResolveAndApplyTheme();

        _logger.LogInformation("Theme set to {ThemeMode} (resolved: {ResolvedTheme})", themeMode, ResolvedTheme);
    }

    public void CycleTheme()
    {
        var nextTheme = CurrentThemeMode switch
        {
            ThemeMode.Dark => ThemeMode.Light,
            ThemeMode.Light => ThemeMode.System,
            ThemeMode.System => ThemeMode.Dark,
            _ => ThemeMode.Dark
        };

        SetTheme(nextTheme);
    }

    public void RefreshHostConfig()
    {
        ResolveAndApplyTheme();
    }

    private void LoadThemeConfigs()
    {
        _logger.LogInformation("Loading theme configurations...");
        _darkHostConfigOriginal = LoadHostConfigFromResource(DarkThemePath, applyFontOverrides: false);
        _lightHostConfigOriginal = LoadHostConfigFromResource(LightThemePath, applyFontOverrides: false);

        if (_darkHostConfigOriginal == null)
        {
            _logger.LogWarning("Failed to load dark theme, using fallback");
            _darkHostConfigOriginal = CreateFallbackHostConfig();
        }
        else
        {
            _logger.LogInformation("Dark theme loaded successfully");
        }

        if (_lightHostConfigOriginal == null)
        {
            _logger.LogWarning("Failed to load light theme, using fallback");
            _lightHostConfigOriginal = CreateFallbackHostConfig();
        }
        else
        {
            _logger.LogInformation("Light theme loaded successfully");
        }

        // Create working copies with font overrides applied
        _darkHostConfig = CloneHostConfig(_darkHostConfigOriginal);
        _lightHostConfig = CloneHostConfig(_lightHostConfigOriginal);

        if (_darkHostConfig != null)
        {
            ApplyFontOverrides(_darkHostConfig);
        }
        if (_lightHostConfig != null)
        {
            ApplyFontOverrides(_lightHostConfig);
        }
    }

    private AdaptiveHostConfig? LoadHostConfigFromResource(string resourcePath, bool applyFontOverrides = true)
    {
        try
        {
            var uri = new Uri(resourcePath);
            using var stream = AssetLoader.Open(uri);
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            var hostConfig = AdaptiveHostConfig.FromJson(json);

            // Log the loaded background color to verify it's correct
            var bgColor = hostConfig.ContainerStyles?.Default?.BackgroundColor;
            _logger.LogInformation("Loaded theme from {Path}, background color: {BackgroundColor}", resourcePath, bgColor ?? "null");

            // Apply font overrides if configured
            if (applyFontOverrides)
            {
                ApplyFontOverrides(hostConfig);
            }

            return hostConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load theme from {Path}", resourcePath);
            return null;
        }
    }

    private void ApplyFontOverrides(AdaptiveHostConfig hostConfig)
    {
        var fontOverrides = _userPreferencesService.GetFontOverrides();
        if (fontOverrides == null)
            return;

        if (!string.IsNullOrWhiteSpace(fontOverrides.DefaultFontFamily))
        {
            // Use the non-deprecated FontTypes.Default.FontFamily
            hostConfig.FontTypes.Default.FontFamily = fontOverrides.DefaultFontFamily;
            _logger.LogDebug("Applied default font override: {FontFamily}", fontOverrides.DefaultFontFamily);
        }

        if (!string.IsNullOrWhiteSpace(fontOverrides.MonospaceFontFamily))
        {
            // Apply monospace font family
            hostConfig.FontTypes.Monospace.FontFamily = fontOverrides.MonospaceFontFamily;
            _logger.LogDebug("Applied monospace font override: {FontFamily}", fontOverrides.MonospaceFontFamily);
        }
    }

    private AdaptiveHostConfig CloneHostConfig(AdaptiveHostConfig original)
    {
        // Create a fresh copy by serializing and deserializing using Newtonsoft.Json
        // to match the format expected by AdaptiveHostConfig.FromJson
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(original);
        var clone = AdaptiveHostConfig.FromJson(json);
        return clone ?? original;
    }

    private void ResolveAndApplyTheme()
    {
        ResolvedTheme = ResolveTheme(CurrentThemeMode);

        // Recreate working copies from originals to avoid font override accumulation
        _darkHostConfig = CloneHostConfig(_darkHostConfigOriginal ?? CreateFallbackHostConfig());
        _lightHostConfig = CloneHostConfig(_lightHostConfigOriginal ?? CreateFallbackHostConfig());

        // Apply current font overrides to both configs
        if (_darkHostConfig != null)
        {
            ApplyFontOverrides(_darkHostConfig);
        }
        if (_lightHostConfig != null)
        {
            ApplyFontOverrides(_lightHostConfig);
        }

        _currentHostConfig = ResolvedTheme switch
        {
            ThemeMode.Light => _lightHostConfig ?? CreateFallbackHostConfig(),
            _ => _darkHostConfig ?? CreateFallbackHostConfig()
        };

        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(
            CurrentThemeMode,
            ResolvedTheme,
            _currentHostConfig ?? CreateFallbackHostConfig()));
    }

    private ThemeMode ResolveTheme(ThemeMode themeMode)
    {
        if (themeMode != ThemeMode.System)
            return themeMode;

        // Detect system theme
        if (Application.Current != null)
        {
            var actualTheme = Application.Current.ActualThemeVariant;
            if (actualTheme == ThemeVariant.Light)
                return ThemeMode.Light;
        }

        // Default to dark if system theme can't be detected
        return ThemeMode.Dark;
    }

    private void OnSystemThemeChanged(object? sender, EventArgs e)
    {
        if (CurrentThemeMode == ThemeMode.System)
        {
            _logger.LogDebug("System theme changed, refreshing host config");
            ResolveAndApplyTheme();
        }
    }

    private static AdaptiveHostConfig CreateFallbackHostConfig()
    {
        // Create a minimal fallback config if JSON loading fails
        var config = new AdaptiveHostConfig
        {
            SupportsInteractivity = true
        };
        config.FontTypes.Default.FontFamily = "Inter, Segoe UI, -apple-system, BlinkMacSystemFont, sans-serif";
        return config;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        // Unsubscribe from system theme changes
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnSystemThemeChanged;
        }

        _disposed = true;
    }
}
