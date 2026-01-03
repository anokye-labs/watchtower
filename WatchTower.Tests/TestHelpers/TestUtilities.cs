using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using WatchTower.Models;
using AdaptiveCards;

namespace WatchTower.Tests.TestHelpers;

/// <summary>
/// Common utilities and test data builders for tests.
/// </summary>
public static class TestUtilities
{
    /// <summary>
    /// Creates a temporary file path. Caller is responsible for cleanup.
    /// </summary>
    public static string CreateTempFilePath(string extension = ".json")
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"watchtower-test-{Guid.NewGuid()}{extension}");
        return tempPath;
    }
    
    /// <summary>
    /// Creates a temporary directory path.
    /// </summary>
    public static string CreateTempDirectory()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"watchtower-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempPath);
        return tempPath;
    }
    
    /// <summary>
    /// Asserts that a PropertyChanged event was raised for the specified property.
    /// </summary>
    public static bool WasPropertyChangedRaised(INotifyPropertyChanged obj, string propertyName, Action action)
    {
        var wasRaised = false;
        PropertyChangedEventHandler handler = (sender, args) =>
        {
            if (args.PropertyName == propertyName)
                wasRaised = true;
        };
        
        obj.PropertyChanged += handler;
        try
        {
            action();
            return wasRaised;
        }
        finally
        {
            obj.PropertyChanged -= handler;
        }
    }
    
    /// <summary>
    /// Creates a valid sample Adaptive Card for testing.
    /// </summary>
    public static AdaptiveCard CreateSampleAdaptiveCard()
    {
        var card = new AdaptiveCard("1.5")
        {
            Body = new List<AdaptiveElement>
            {
                new AdaptiveTextBlock
                {
                    Text = "Test Card",
                    Size = AdaptiveTextSize.Large,
                    Weight = AdaptiveTextWeight.Bolder
                },
                new AdaptiveTextBlock
                {
                    Text = "This is a test card for unit testing.",
                    Wrap = true
                }
            },
            Actions = new List<AdaptiveAction>
            {
                new AdaptiveSubmitAction
                {
                    Title = "Submit",
                    Data = "test-data"
                }
            }
        };
        
        return card;
    }
    
    /// <summary>
    /// Creates a valid Adaptive Card JSON string for testing.
    /// </summary>
    public static string CreateSampleAdaptiveCardJson()
    {
        return @"{
            ""type"": ""AdaptiveCard"",
            ""version"": ""1.5"",
            ""body"": [
                {
                    ""type"": ""TextBlock"",
                    ""text"": ""Test Card"",
                    ""size"": ""Large"",
                    ""weight"": ""Bolder""
                }
            ],
            ""actions"": [
                {
                    ""type"": ""Action.Submit"",
                    ""title"": ""Submit"",
                    ""data"": ""test-data""
                }
            ]
        }";
    }
    
    /// <summary>
    /// Creates an invalid Adaptive Card JSON string for testing error handling.
    /// </summary>
    public static string CreateInvalidAdaptiveCardJson()
    {
        return @"{
            ""type"": ""AdaptiveCard"",
            ""version"": ""99.99"",
            ""invalid"": true
        }";
    }
    
    /// <summary>
    /// Creates a sample UserPreferences object for testing.
    /// </summary>
    public static UserPreferences CreateSampleUserPreferences(ThemeMode themeMode = ThemeMode.Dark)
    {
        return new UserPreferences
        {
            ThemeMode = themeMode,
            FontOverrides = new FontOverrides
            {
                DefaultFontFamily = "Segoe UI",
                MonospaceFontFamily = "Consolas"
            }
        };
    }
    
    /// <summary>
    /// Creates a sample GameControllerState for testing.
    /// </summary>
    public static GameControllerState CreateSampleGameControllerState(bool aPressed = false, bool bPressed = false)
    {
        return new GameControllerState
        {
            IsConnected = true,
            ButtonStates = new Dictionary<GameControllerButton, bool>
            {
                { GameControllerButton.A, aPressed },
                { GameControllerButton.B, bPressed },
                { GameControllerButton.X, false },
                { GameControllerButton.Y, false }
            },
            LeftStickX = 0.0f,
            LeftStickY = 0.0f,
            RightStickX = 0.0f,
            RightStickY = 0.0f
        };
    }
    
    /// <summary>
    /// Retries an action up to maxAttempts times, useful for testing async/event scenarios.
    /// </summary>
    public static async Task<T> RetryAsync<T>(Func<Task<T>> action, int maxAttempts = 3, int delayMs = 100)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                return await action();
            }
            catch when (i < maxAttempts - 1)
            {
                await Task.Delay(delayMs);
            }
        }
        
        return await action(); // Let the exception propagate on final attempt
    }
    
    /// <summary>
    /// Waits for a condition to become true, useful for testing async operations.
    /// </summary>
    public static async Task<bool> WaitForConditionAsync(Func<bool> condition, int timeoutMs = 1000, int pollIntervalMs = 50)
    {
        var startTime = DateTime.UtcNow;
        while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
        {
            if (condition())
                return true;
            
            await Task.Delay(pollIntervalMs);
        }
        
        return condition(); // Check one final time
    }
}
