using System;

namespace WatchTower.Services;

/// <summary>
/// Event arguments for feature flag change notifications.
/// Provides information about which flag changed and its old and new values.
/// </summary>
public class FeatureFlagChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the key identifier of the feature flag that changed.
    /// </summary>
    public string FlagKey { get; }

    /// <summary>
    /// Gets the previous value of the feature flag before the change.
    /// </summary>
    public object? OldValue { get; }

    /// <summary>
    /// Gets the new value of the feature flag after the change.
    /// </summary>
    public object? NewValue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureFlagChangedEventArgs"/> class.
    /// </summary>
    /// <param name="flagKey">The key identifier of the feature flag that changed.</param>
    /// <param name="oldValue">The previous value of the feature flag.</param>
    /// <param name="newValue">The new value of the feature flag.</param>
    /// <exception cref="ArgumentNullException">Thrown when flagKey is null.</exception>
    /// <exception cref="ArgumentException">Thrown when flagKey is empty or whitespace.</exception>
    public FeatureFlagChangedEventArgs(string flagKey, object? oldValue, object? newValue)
    {
        ArgumentNullException.ThrowIfNull(flagKey);
        if (string.IsNullOrWhiteSpace(flagKey))
            throw new ArgumentException("Flag key cannot be empty or whitespace.", nameof(flagKey));

        FlagKey = flagKey;
        OldValue = oldValue;
        NewValue = newValue;
    }
}

/// <summary>
/// Service for managing feature flags that control application behavior.
/// Provides access to feature flag values and notifications when flags change.
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Determines whether a feature flag is enabled.
    /// </summary>
    /// <param name="flagKey">The unique identifier for the feature flag.</param>
    /// <returns>True if the feature flag is enabled; otherwise, false.</returns>
    bool IsEnabled(string flagKey);

    /// <summary>
    /// Gets the value of a feature flag with a specified default value.
    /// </summary>
    /// <typeparam name="T">The type of the feature flag value.</typeparam>
    /// <param name="flagKey">The unique identifier for the feature flag.</param>
    /// <param name="defaultValue">The default value to return if the flag is not found or cannot be converted to type T.</param>
    /// <returns>The value of the feature flag, or the default value if not found or invalid.</returns>
    T GetValue<T>(string flagKey, T defaultValue);

    /// <summary>
    /// Reloads all feature flags from the underlying data source.
    /// This method can be called to refresh the feature flag state, potentially triggering FlagChanged events.
    /// </summary>
    void Reload();

    /// <summary>
    /// Event raised when a feature flag value changes.
    /// Subscribers can use this to respond to dynamic feature flag updates.
    /// </summary>
    event EventHandler<FeatureFlagChangedEventArgs>? FlagChanged;
}

