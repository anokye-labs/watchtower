using System;
using Xunit;
using WatchTower.Models;
using WatchTower.Services;

namespace WatchTower.Tests.Services;

/// <summary>
/// Tests for the IFeatureFlagService interface contract.
/// </summary>
public class FeatureFlagServiceInterfaceTests
{
    [Fact]
    public void FeatureFlagChangedEventArgs_Constructor_InitializesProperties()
    {
        // Arrange
        const string flagKey = "test-flag";
        object oldValue = true;
        object newValue = false;

        // Act
        var eventArgs = new FeatureFlagChangedEventArgs(flagKey, oldValue, newValue);

        // Assert
        Assert.Equal(flagKey, eventArgs.FlagKey);
        Assert.Equal(oldValue, eventArgs.OldValue);
        Assert.Equal(newValue, eventArgs.NewValue);
    }

    [Fact]
    public void FeatureFlagChangedEventArgs_Constructor_HandlesNullValues()
    {
        // Arrange
        const string flagKey = "test-flag";

        // Act
        var eventArgs = new FeatureFlagChangedEventArgs(flagKey, null, null);

        // Assert
        Assert.Equal(flagKey, eventArgs.FlagKey);
        Assert.Null(eventArgs.OldValue);
        Assert.Null(eventArgs.NewValue);
    }

    [Fact]
    public void FeatureFlagChangedEventArgs_InheritsFromEventArgs()
    {
        // Arrange
        var eventArgs = new FeatureFlagChangedEventArgs("test", null, null);

        // Assert
        Assert.IsAssignableFrom<EventArgs>(eventArgs);
    }
}
