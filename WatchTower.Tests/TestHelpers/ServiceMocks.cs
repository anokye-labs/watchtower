using Moq;
using Microsoft.Extensions.Logging;
using WatchTower.Services;

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
        return mock;
    }
    
    // Add more mock factories as needed
}
