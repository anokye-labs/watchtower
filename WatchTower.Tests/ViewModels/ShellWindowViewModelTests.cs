using Xunit;
using WatchTower.ViewModels;
using WatchTower.Services;

namespace WatchTower.Tests.ViewModels;

public class ShellWindowViewModelTests
{
    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        // Arrange
        var splashViewModel = new SplashWindowViewModel();
        var frameSliceService = new FrameSliceService();
        
        // Act
        var viewModel = new ShellWindowViewModel(splashViewModel, frameSliceService);
        
        // Assert
        Assert.NotNull(viewModel);
        Assert.True(viewModel.IsInSplashMode);
        Assert.Equal(splashViewModel, viewModel.SplashViewModel);
        Assert.Equal(splashViewModel, viewModel.CurrentContent);
    }
    
    [Fact]
    public void EnableWindowedMode_DefaultValue_IsTrue()
    {
        // Arrange
        var splashViewModel = new SplashWindowViewModel();
        var viewModel = new ShellWindowViewModel(splashViewModel);
        
        // Act & Assert
        Assert.True(viewModel.EnableWindowedMode);
    }
    
    [Fact]
    public void EnableWindowedMode_CanBeSetToFalse()
    {
        // Arrange
        var splashViewModel = new SplashWindowViewModel();
        var viewModel = new ShellWindowViewModel(splashViewModel);
        
        // Act
        viewModel.EnableWindowedMode = false;
        
        // Assert
        Assert.False(viewModel.EnableWindowedMode);
    }
    
    [Fact]
    public void ResizeHandleSize_DefaultValue_Is8()
    {
        // Arrange
        var splashViewModel = new SplashWindowViewModel();
        var viewModel = new ShellWindowViewModel(splashViewModel);
        
        // Act & Assert
        Assert.Equal(8.0, viewModel.ResizeHandleSize);
    }
    
    [Fact]
    public void ResizeHandleSize_SetValue_IsClampedToMinimum4()
    {
        // Arrange
        var splashViewModel = new SplashWindowViewModel();
        var viewModel = new ShellWindowViewModel(splashViewModel);
        
        // Act - Test value below minimum
        viewModel.ResizeHandleSize = 2.0;
        
        // Assert
        Assert.Equal(4.0, viewModel.ResizeHandleSize);
        
        // Act - Test valid value
        viewModel.ResizeHandleSize = 12.0;
        
        // Assert
        Assert.Equal(12.0, viewModel.ResizeHandleSize);
    }
    
    [Fact]
    public void CurrentFrameSlices_InitiallyNull()
    {
        // Arrange
        var splashViewModel = new SplashWindowViewModel();
        var viewModel = new ShellWindowViewModel(splashViewModel);
        
        // Act & Assert
        Assert.Null(viewModel.CurrentFrameSlices);
    }
}
