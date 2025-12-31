using System;

namespace WatchTower.Models;

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
    public FeatureFlagChangedEventArgs(string flagKey, object? oldValue, object? newValue)
    {
        FlagKey = flagKey;
        OldValue = oldValue;
        NewValue = newValue;
    }
}
