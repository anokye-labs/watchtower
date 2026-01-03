using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WatchTower.Models;

namespace WatchTower.Services;

/// <summary>
/// Service for managing user preferences that persist to a JSON file.
/// </summary>
public class UserPreferencesService : IUserPreferencesService
{
    private readonly ILogger<UserPreferencesService> _logger;
    private readonly string _preferencesFilePath;
    private UserPreferences _preferences;
    private readonly object _lock = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public event EventHandler<UserPreferences>? PreferencesChanged;

    public UserPreferencesService(ILogger<UserPreferencesService> logger)
    {
        _logger = logger;
        _preferencesFilePath = GetPreferencesFilePath();
        _preferences = LoadPreferences();
    }

    public UserPreferences GetPreferences()
    {
        lock (_lock)
        {
            // Return a defensive copy to avoid exposing the internal mutable instance.
            var json = JsonSerializer.Serialize(_preferences, JsonOptions);
            var copy = JsonSerializer.Deserialize<UserPreferences>(json, JsonOptions);
            return copy ?? new UserPreferences();
        }
    }

    public void SavePreferences(UserPreferences preferences)
    {
        lock (_lock)
        {
            _preferences = preferences;
            PersistPreferences();
        }
        PreferencesChanged?.Invoke(this, preferences);
    }

    public ThemeMode GetThemeMode()
    {
        lock (_lock)
        {
            return _preferences.ThemeMode;
        }
    }

    public void SetThemeMode(ThemeMode themeMode)
    {
        lock (_lock)
        {
            if (_preferences.ThemeMode == themeMode)
                return;

            _preferences.ThemeMode = themeMode;
            PersistPreferences();
        }
        _logger.LogInformation("Theme mode changed to {ThemeMode}", themeMode);
        PreferencesChanged?.Invoke(this, _preferences);
    }

    public FontOverrides? GetFontOverrides()
    {
        lock (_lock)
        {
            var fontOverrides = _preferences.FontOverrides;
            if (fontOverrides == null)
            {
                return null;
            }

            // Return a defensive copy to avoid exposing internal mutable state outside the lock.
            var json = JsonSerializer.Serialize(fontOverrides, JsonOptions);
            return JsonSerializer.Deserialize<FontOverrides>(json, JsonOptions);
        }
    }

    public void SetFontOverrides(FontOverrides? fontOverrides)
    {
        lock (_lock)
        {
            _preferences.FontOverrides = fontOverrides;
            PersistPreferences();
        }
        _logger.LogInformation("Font overrides updated");
        PreferencesChanged?.Invoke(this, _preferences);
    }

    public bool IsFirstRun()
    {
        lock (_lock)
        {
            return _preferences.IsFirstRun;
        }
    }

    public void MarkFirstRunComplete()
    {
        lock (_lock)
        {
            if (!_preferences.IsFirstRun)
                return;

            _preferences.IsFirstRun = false;
            PersistPreferences();
        }
        _logger.LogInformation("First run marked as complete");
        PreferencesChanged?.Invoke(this, _preferences);
    }

    public bool HasSeenWelcomeScreen()
    {
        lock (_lock)
        {
            return _preferences.HasSeenWelcomeScreen;
        }
    }

    public void MarkWelcomeScreenSeen()
    {
        lock (_lock)
        {
            if (_preferences.HasSeenWelcomeScreen)
                return;

            _preferences.HasSeenWelcomeScreen = true;
            _preferences.WelcomeScreenDismissedDate = DateTime.UtcNow;
            PersistPreferences();
        }
        _logger.LogInformation("Welcome screen marked as seen");
        PreferencesChanged?.Invoke(this, _preferences);
    }

    public WindowPositionPreferences? GetWindowPosition()
    {
        lock (_lock)
        {
            var windowPosition = _preferences.WindowPosition;
            if (windowPosition == null)
            {
                return null;
            }

            // Return a defensive copy to avoid exposing internal mutable state outside the lock.
            return windowPosition.Clone();
        }
    }

    public void SetWindowPosition(WindowPositionPreferences? windowPosition)
    {
        lock (_lock)
        {
            _preferences.WindowPosition = windowPosition;
            PersistPreferences();
        }
        _logger.LogDebug("Window position preferences updated");
        PreferencesChanged?.Invoke(this, _preferences);
    }

    private static string GetPreferencesFilePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var watchTowerPath = Path.Combine(appDataPath, "WatchTower");
        Directory.CreateDirectory(watchTowerPath);
        return Path.Combine(watchTowerPath, "user-preferences.json");
    }

    private UserPreferences LoadPreferences()
    {
        try
        {
            if (File.Exists(_preferencesFilePath))
            {
                var json = File.ReadAllText(_preferencesFilePath);
                var preferences = JsonSerializer.Deserialize<UserPreferences>(json, JsonOptions);
                if (preferences != null)
                {
                    _logger.LogDebug("Loaded user preferences from {Path}", _preferencesFilePath);
                    return preferences;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load user preferences from {Path}, using defaults", _preferencesFilePath);
        }

        _logger.LogDebug("Using default user preferences");
        return new UserPreferences();
    }

    private void PersistPreferences()
    {
        try
        {
            var json = JsonSerializer.Serialize(_preferences, JsonOptions);
            File.WriteAllText(_preferencesFilePath, json);
            _logger.LogDebug("Saved user preferences to {Path}", _preferencesFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user preferences to {Path}", _preferencesFilePath);
        }
    }
}
