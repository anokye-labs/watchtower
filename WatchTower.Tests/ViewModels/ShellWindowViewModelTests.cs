using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using WatchTower.ViewModels;
using WatchTower.Services;
using System;
using System.Collections.Generic;

namespace WatchTower.Tests.ViewModels;

/// <summary>
/// Tests for ShellWindowViewModel, focusing on windowed mode configuration loading.
/// </summary>
public class ShellWindowViewModelTests
{
    private Mock<IConfiguration> CreateMockConfiguration(Dictionary<string, string> configValues)
    {
        var mockConfig = new Mock<IConfiguration>();
        
        foreach (var kvp in configValues)
        {
            mockConfig.Setup(c => c[kvp.Key]).Returns(kvp.Value);
        }
        
        return mockConfig;
    }

    [Fact]
    public void LoadWindowedModeConfiguration_WithValidConfig_LoadsCorrectly()
    {
        // Arrange
        var mockSplashViewModel = new Mock<SplashWindowViewModel>(30);
        var mockFrameSliceService = new Mock<IFrameSliceService>();
        var viewModel = new ShellWindowViewModel(mockSplashViewModel.Object, mockFrameSliceService.Object);
        
        var config = CreateMockConfiguration(new Dictionary<string, string>
        {
            ["Frame:EnableWindowedMode"] = "true",
            ["Frame:InitialWidth"] = "1600",
            ["Frame:InitialHeight"] = "900"
        });

        // Act
        viewModel.LoadWindowedModeConfiguration(config.Object);

        // Assert
        Assert.True(viewModel.IsWindowedModeEnabled);
        Assert.Equal(1600, viewModel.InitialWindowWidth);
        Assert.Equal(900, viewModel.InitialWindowHeight);
    }

    [Fact]
    public void LoadWindowedModeConfiguration_WithMissingValues_UsesDefaults()
    {
        // Arrange
        var mockSplashViewModel = new Mock<SplashWindowViewModel>(30);
        var mockFrameSliceService = new Mock<IFrameSliceService>();
        var viewModel = new ShellWindowViewModel(mockSplashViewModel.Object, mockFrameSliceService.Object);
        
        var config = CreateMockConfiguration(new Dictionary<string, string>());

        // Act
        viewModel.LoadWindowedModeConfiguration(config.Object);

        // Assert - should use default values
        Assert.False(viewModel.IsWindowedModeEnabled); // Default is false
        Assert.Equal(1280, viewModel.InitialWindowWidth); // Default
        Assert.Equal(720, viewModel.InitialWindowHeight); // Default
    }

    [Fact]
    public void LoadWindowedModeConfiguration_WithInvalidWidth_ThrowsException()
    {
        // Arrange
        var mockSplashViewModel = new Mock<SplashWindowViewModel>(30);
        var mockFrameSliceService = new Mock<IFrameSliceService>();
        var viewModel = new ShellWindowViewModel(mockSplashViewModel.Object, mockFrameSliceService.Object);
        
        var config = CreateMockConfiguration(new Dictionary<string, string>
        {
            ["Frame:InitialWidth"] = "200" // Below minimum of 400
        });

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            viewModel.LoadWindowedModeConfiguration(config.Object));
        Assert.Contains("Frame:InitialWidth must be between 400 and 4000", exception.Message);
    }

    [Fact]
    public void LoadWindowedModeConfiguration_WithInvalidHeight_ThrowsException()
    {
        // Arrange
        var mockSplashViewModel = new Mock<SplashWindowViewModel>(30);
        var mockFrameSliceService = new Mock<IFrameSliceService>();
        var viewModel = new ShellWindowViewModel(mockSplashViewModel.Object, mockFrameSliceService.Object);
        
        var config = CreateMockConfiguration(new Dictionary<string, string>
        {
            ["Frame:InitialHeight"] = "5000" // Above maximum of 4000
        });

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            viewModel.LoadWindowedModeConfiguration(config.Object));
        Assert.Contains("Frame:InitialHeight must be between 300 and 4000", exception.Message);
    }

    [Fact]
    public void LoadWindowedModeConfiguration_WithEmptyStrings_UsesDefaults()
    {
        // Arrange
        var mockSplashViewModel = new Mock<SplashWindowViewModel>(30);
        var mockFrameSliceService = new Mock<IFrameSliceService>();
        var viewModel = new ShellWindowViewModel(mockSplashViewModel.Object, mockFrameSliceService.Object);
        
        var config = CreateMockConfiguration(new Dictionary<string, string>
        {
            ["Frame:EnableWindowedMode"] = "",
            ["Frame:InitialWidth"] = "",
            ["Frame:InitialHeight"] = ""
        });

        // Act
        viewModel.LoadWindowedModeConfiguration(config.Object);

        // Assert - should use defaults for empty strings
        Assert.False(viewModel.IsWindowedModeEnabled);
        Assert.Equal(1280, viewModel.InitialWindowWidth);
        Assert.Equal(720, viewModel.InitialWindowHeight);
    }

    [Fact]
    public void LoadWindowedModeConfiguration_WithInvalidFormat_UsesDefaults()
    {
        // Arrange
        var mockSplashViewModel = new Mock<SplashWindowViewModel>(30);
        var mockFrameSliceService = new Mock<IFrameSliceService>();
        var viewModel = new ShellWindowViewModel(mockSplashViewModel.Object, mockFrameSliceService.Object);
        
        var config = CreateMockConfiguration(new Dictionary<string, string>
        {
            ["Frame:EnableWindowedMode"] = "not-a-boolean",
            ["Frame:InitialWidth"] = "not-a-number",
            ["Frame:InitialHeight"] = "also-not-a-number"
        });

        // Act
        viewModel.LoadWindowedModeConfiguration(config.Object);

        // Assert - TryParse fails, should use defaults
        Assert.False(viewModel.IsWindowedModeEnabled);
        Assert.Equal(1280, viewModel.InitialWindowWidth);
        Assert.Equal(720, viewModel.InitialWindowHeight);
    }

    [Fact]
    public void LoadWindowedModeConfiguration_CalledMultipleTimes_OnlyLoadsOnce()
    {
        // Arrange
        var mockSplashViewModel = new Mock<SplashWindowViewModel>(30);
        var mockFrameSliceService = new Mock<IFrameSliceService>();
        var viewModel = new ShellWindowViewModel(mockSplashViewModel.Object, mockFrameSliceService.Object);
        
        var firstConfig = CreateMockConfiguration(new Dictionary<string, string>
        {
            ["Frame:EnableWindowedMode"] = "true",
            ["Frame:InitialWidth"] = "1600",
            ["Frame:InitialHeight"] = "900"
        });
        
        var secondConfig = CreateMockConfiguration(new Dictionary<string, string>
        {
            ["Frame:EnableWindowedMode"] = "false",
            ["Frame:InitialWidth"] = "800",
            ["Frame:InitialHeight"] = "600"
        });

        // Act
        viewModel.LoadWindowedModeConfiguration(firstConfig.Object);
        viewModel.LoadWindowedModeConfiguration(secondConfig.Object); // Should be ignored

        // Assert - first config should still be in effect
        Assert.True(viewModel.IsWindowedModeEnabled);
        Assert.Equal(1600, viewModel.InitialWindowWidth);
        Assert.Equal(900, viewModel.InitialWindowHeight);
    }

    [Fact]
    public void LoadWindowedModeConfiguration_WithNullConfig_ReturnsEarly()
    {
        // Arrange
        var mockSplashViewModel = new Mock<SplashWindowViewModel>(30);
        var mockFrameSliceService = new Mock<IFrameSliceService>();
        var viewModel = new ShellWindowViewModel(mockSplashViewModel.Object, mockFrameSliceService.Object);

        // Act & Assert - should not throw
        viewModel.LoadWindowedModeConfiguration(null!);
        
        // Verify defaults are still in place
        Assert.False(viewModel.IsWindowedModeEnabled);
        Assert.Equal(1280, viewModel.InitialWindowWidth);
        Assert.Equal(720, viewModel.InitialWindowHeight);
    }

    [Fact]
    public void LoadWindowedModeConfiguration_WithBoundaryValues_AcceptsValid()
    {
        // Arrange
        var mockSplashViewModel = new Mock<SplashWindowViewModel>(30);
        var mockFrameSliceService = new Mock<IFrameSliceService>();
        var viewModel = new ShellWindowViewModel(mockSplashViewModel.Object, mockFrameSliceService.Object);
        
        var config = CreateMockConfiguration(new Dictionary<string, string>
        {
            ["Frame:InitialWidth"] = "400",  // Minimum valid
            ["Frame:InitialHeight"] = "300"  // Minimum valid
        });

        // Act
        viewModel.LoadWindowedModeConfiguration(config.Object);

        // Assert - boundary values should be accepted
        Assert.Equal(400, viewModel.InitialWindowWidth);
        Assert.Equal(300, viewModel.InitialWindowHeight);
    }

    [Fact]
    public void LoadWindowedModeConfiguration_WithMaxBoundaryValues_AcceptsValid()
    {
        // Arrange
        var mockSplashViewModel = new Mock<SplashWindowViewModel>(30);
        var mockFrameSliceService = new Mock<IFrameSliceService>();
        var viewModel = new ShellWindowViewModel(mockSplashViewModel.Object, mockFrameSliceService.Object);
        
        var config = CreateMockConfiguration(new Dictionary<string, string>
        {
            ["Frame:InitialWidth"] = "4000",  // Maximum valid
            ["Frame:InitialHeight"] = "4000"  // Maximum valid
        });

        // Act
        viewModel.LoadWindowedModeConfiguration(config.Object);

        // Assert - boundary values should be accepted
        Assert.Equal(4000, viewModel.InitialWindowWidth);
        Assert.Equal(4000, viewModel.InitialWindowHeight);
    }
}
