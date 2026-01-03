using Moq;
using Microsoft.Extensions.Logging;
using WatchTower.Services;
using AdaptiveCards.Rendering;
using WatchTower.Models;
using System.Collections.Generic;

namespace WatchTower.Tests.TestHelpers;

/// <summary>
/// Reusable mock configurations for service tests.
/// </summary>
public static class ServiceMocks
{
    public static Mock<ILogger<T>> CreateLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }

    public static Mock<IGameControllerService> CreateGameControllerService()
    {
        var mock = new Mock<IGameControllerService>();
        mock.Setup(s => s.IsInitialized).Returns(true);
        mock.Setup(s => s.ConnectedControllers).Returns(new List<GameControllerState>());
        return mock;
    }

    public static Mock<IAdaptiveCardService> CreateAdaptiveCardService()
    {
        var mock = new Mock<IAdaptiveCardService>();
        // Setup basic methods if needed
        return mock;
    }

    public static Mock<IAdaptiveCardThemeService> CreateAdaptiveCardThemeService()
    {
        var mock = new Mock<IAdaptiveCardThemeService>();
        mock.Setup(s => s.CurrentThemeMode).Returns(ThemeMode.Dark);
        mock.Setup(s => s.GetHostConfig()).Returns(new AdaptiveHostConfig());
        return mock;
    }

    public static Mock<IVoiceOrchestrationService> CreateVoiceOrchestrationService()
    {
        var mock = new Mock<IVoiceOrchestrationService>();
        mock.Setup(s => s.IsInitialized).Returns(true);
        mock.Setup(s => s.State).Returns(new VoiceState());
        return mock;
    }
}
