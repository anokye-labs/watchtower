using AdaptiveCards.Rendering;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using WatchTower.Models;
using WatchTower.Services;

namespace WatchTower.Tests.TestHelpers;

/// <summary>
/// Reusable mock configurations for service tests.
/// </summary>
public static class ServiceMocks
{
    /// <summary>
    /// Creates a mock ILogger that doesn't record calls by default.
    /// </summary>
    public static Mock<ILogger<T>> CreateLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }
    
    /// <summary>
    /// Creates a mock IGameControllerService with common default setup.
    /// </summary>
    public static Mock<IGameControllerService> CreateGameControllerService(bool isInitialized = true)
    {
        var mock = new Mock<IGameControllerService>();
        mock.Setup(s => s.IsInitialized).Returns(isInitialized);
        mock.Setup(s => s.ConnectedControllers).Returns(new List<GameControllerState>());
        return mock;
    }
    
    /// <summary>
    /// Creates a mock IAdaptiveCardService with common default setup.
    /// </summary>
    public static Mock<IAdaptiveCardService> CreateAdaptiveCardService()
    {
        var mock = new Mock<IAdaptiveCardService>();
        mock.Setup(s => s.CreateSampleCard()).Returns(new AdaptiveCard("1.5"));
        return mock;
    }
    
    /// <summary>
    /// Creates a mock IAdaptiveCardThemeService with common default setup.
    /// </summary>
    public static Mock<IAdaptiveCardThemeService> CreateAdaptiveCardThemeService()
    {
        var mock = new Mock<IAdaptiveCardThemeService>();
        mock.Setup(s => s.GetHostConfig()).Returns(new AdaptiveHostConfig());
        mock.Setup(s => s.CurrentThemeMode).Returns(ThemeMode.Dark);
        mock.Setup(s => s.ResolvedTheme).Returns(ThemeMode.Dark);
        return mock;
    }
    
    /// <summary>
    /// Creates a mock IUserPreferencesService with common default setup.
    /// </summary>
    public static Mock<IUserPreferencesService> CreateUserPreferencesService()
    {
        var mock = new Mock<IUserPreferencesService>();
        mock.Setup(s => s.GetPreferences()).Returns(new UserPreferences());
        mock.Setup(s => s.GetThemeMode()).Returns(ThemeMode.Dark);
        return mock;
    }
    
    /// <summary>
    /// Creates a mock IVoiceOrchestrationService with common default setup.
    /// </summary>
    public static Mock<IVoiceOrchestrationService> CreateVoiceOrchestrationService(bool isInitialized = true)
    {
        var mock = new Mock<IVoiceOrchestrationService>();
        mock.Setup(s => s.IsInitialized).Returns(isInitialized);
        mock.Setup(s => s.State).Returns(new VoiceState { IsInitialized = isInitialized });
        return mock;
    }
    
    /// <summary>
    /// Creates a mock IVoiceRecognitionService with common default setup.
    /// </summary>
    public static Mock<IVoiceRecognitionService> CreateVoiceRecognitionService()
    {
        var mock = new Mock<IVoiceRecognitionService>();
        mock.Setup(s => s.IsInitialized).Returns(true);
        return mock;
    }
    
    /// <summary>
    /// Creates a mock ITextToSpeechService with common default setup.
    /// </summary>
    public static Mock<ITextToSpeechService> CreateTextToSpeechService()
    {
        var mock = new Mock<ITextToSpeechService>();
        mock.Setup(s => s.IsInitialized).Returns(true);
        return mock;
    }
    
    /// <summary>
    /// Creates a mock IFrameSliceService with common default setup.
    /// </summary>
    public static Mock<IFrameSliceService> CreateFrameSliceService()
    {
        var mock = new Mock<IFrameSliceService>();
        return mock;
    }
    
    /// <summary>
    /// Creates a mock IStartupOrchestrator with common default setup.
    /// </summary>
    public static Mock<IStartupOrchestrator> CreateStartupOrchestrator()
    {
        var mock = new Mock<IStartupOrchestrator>();
        return mock;
    }
}
