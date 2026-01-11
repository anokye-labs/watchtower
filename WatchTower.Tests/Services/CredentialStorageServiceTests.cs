using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using WatchTower.Services;
using Xunit;

namespace WatchTower.Tests.Services;

[SupportedOSPlatform("windows5.1.2600")]
public class CredentialStorageServiceTests : IDisposable
{
    private readonly CredentialStorageService _service;
    private readonly Mock<ILogger<CredentialStorageService>> _loggerMock;
    private const string TestKey = "test-credential-" + nameof(CredentialStorageServiceTests);

    public CredentialStorageServiceTests()
    {
        _loggerMock = new Mock<ILogger<CredentialStorageService>>();
        _service = new CredentialStorageService(_loggerMock.Object);
    }

    public void Dispose()
    {
        // Clean up test credentials
        _service.DeleteTokenAsync(TestKey).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task StoreAndRetrieve_RoundTrips()
    {
        // Arrange
        var testValue = "test-token-value-" + Guid.NewGuid();

        // Act
        await _service.StoreTokenAsync(TestKey, testValue);
        var retrieved = await _service.GetTokenAsync(TestKey);

        // Assert
        Assert.Equal(testValue, retrieved);
    }

    [Fact]
    public async Task GetToken_ReturnsNull_WhenNotFound()
    {
        var result = await _service.GetTokenAsync("nonexistent-key-12345");
        Assert.Null(result);
    }

    [Fact]
    public async Task HasToken_ReturnsTrue_AfterStore()
    {
        await _service.StoreTokenAsync(TestKey, "value");
        Assert.True(await _service.HasTokenAsync(TestKey));
    }

    [Fact]
    public async Task HasToken_ReturnsFalse_AfterDelete()
    {
        await _service.StoreTokenAsync(TestKey, "value");
        await _service.DeleteTokenAsync(TestKey);
        Assert.False(await _service.HasTokenAsync(TestKey));
    }

    [Fact]
    public async Task DeleteToken_Succeeds_WhenNotExists()
    {
        // Should not throw
        await _service.DeleteTokenAsync("nonexistent-key-67890");
    }

    [Fact]
    public async Task StoreToken_ThrowsArgumentException_WhenKeyEmpty()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.StoreTokenAsync("", "value"));
    }

    [Fact]
    public async Task StoreToken_ThrowsArgumentException_WhenTokenEmpty()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.StoreTokenAsync("key", ""));
    }

    [Fact]
    public async Task GetToken_ThrowsArgumentException_WhenKeyEmpty()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.GetTokenAsync(""));
    }

    [Fact]
    public async Task DeleteToken_ThrowsArgumentException_WhenKeyEmpty()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.DeleteTokenAsync(""));
    }

    [Fact]
    public async Task HasToken_ThrowsArgumentException_WhenKeyEmpty()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.HasTokenAsync(""));
    }

    [Fact]
    public async Task StoreToken_OverwritesExistingValue()
    {
        // Arrange
        var firstValue = "first-value-" + Guid.NewGuid();
        var secondValue = "second-value-" + Guid.NewGuid();

        // Act
        await _service.StoreTokenAsync(TestKey, firstValue);
        await _service.StoreTokenAsync(TestKey, secondValue);
        var retrieved = await _service.GetTokenAsync(TestKey);

        // Assert
        Assert.Equal(secondValue, retrieved);
    }

    [Fact]
    public async Task CredentialPersistsAcrossServiceInstances()
    {
        // Arrange
        var testValue = "persist-test-" + Guid.NewGuid();
        var service1 = new CredentialStorageService(_loggerMock.Object);

        // Act
        await service1.StoreTokenAsync(TestKey, testValue);

        // Create a new service instance to simulate app restart
        var service2 = new CredentialStorageService(_loggerMock.Object);
        var retrieved = await service2.GetTokenAsync(TestKey);

        // Assert
        Assert.Equal(testValue, retrieved);
    }

    [Fact]
    public async Task HasToken_ReturnsFalse_ForNonexistentKey()
    {
        var result = await _service.HasTokenAsync("definitely-does-not-exist-" + Guid.NewGuid());
        Assert.False(result);
    }

    [Fact]
    public async Task GetToken_ReturnsNull_AfterDelete()
    {
        // Arrange
        var testValue = "delete-test-" + Guid.NewGuid();
        await _service.StoreTokenAsync(TestKey, testValue);

        // Act
        await _service.DeleteTokenAsync(TestKey);
        var retrieved = await _service.GetTokenAsync(TestKey);

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task StoreToken_HandlesSpecialCharacters()
    {
        // Arrange
        var testValue = "Special!@#$%^&*()_+-=[]{}|;:',.<>?/~`\" " + Guid.NewGuid();

        // Act
        await _service.StoreTokenAsync(TestKey, testValue);
        var retrieved = await _service.GetTokenAsync(TestKey);

        // Assert
        Assert.Equal(testValue, retrieved);
    }

    [Fact]
    public async Task StoreToken_HandlesLongTokens()
    {
        // Arrange
        var longToken = new string('X', 2048) + Guid.NewGuid().ToString();

        // Act
        await _service.StoreTokenAsync(TestKey, longToken);
        var retrieved = await _service.GetTokenAsync(TestKey);

        // Assert
        Assert.Equal(longToken, retrieved);
    }
}
