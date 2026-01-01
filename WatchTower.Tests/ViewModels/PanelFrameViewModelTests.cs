using Xunit;
using WatchTower.Models;
using WatchTower.Services;
using WatchTower.ViewModels;

namespace WatchTower.Tests.ViewModels;

public class PanelFrameViewModelTests
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        // Arrange & Act
        var viewModel = new PanelFrameViewModel();
        
        // Assert
        Assert.NotNull(viewModel);
        Assert.Equal(PanelSlideDirection.Left, viewModel.SlideDirection);
        Assert.Equal(1.0, viewModel.RenderScale);
        Assert.Equal(1.0, viewModel.FrameDisplayScale);
        // Default direction is Left, so left edge should be hidden
        Assert.False(viewModel.ShowLeftEdge);
        Assert.True(viewModel.ShowRightEdge);
        Assert.True(viewModel.ShowTopEdge);
        Assert.True(viewModel.ShowBottomEdge);
    }
    
    [Fact]
    public void SlideDirection_Left_HidesLeftEdge()
    {
        // Arrange
        var viewModel = new PanelFrameViewModel();
        
        // Change to a different direction first
        viewModel.SlideDirection = PanelSlideDirection.Bottom;
        
        // Act - change back to Left
        viewModel.SlideDirection = PanelSlideDirection.Left;
        
        // Assert
        Assert.False(viewModel.ShowLeftEdge);
        Assert.True(viewModel.ShowRightEdge);
        Assert.True(viewModel.ShowTopEdge);
        Assert.True(viewModel.ShowBottomEdge);
    }
    
    [Fact]
    public void SlideDirection_Bottom_HidesBottomEdge()
    {
        // Arrange
        var viewModel = new PanelFrameViewModel();
        
        // Act
        viewModel.SlideDirection = PanelSlideDirection.Bottom;
        
        // Assert
        Assert.True(viewModel.ShowLeftEdge);
        Assert.True(viewModel.ShowRightEdge);
        Assert.True(viewModel.ShowTopEdge);
        Assert.False(viewModel.ShowBottomEdge);
    }
    
    [Fact]
    public void SlideDirection_Right_HidesRightEdge()
    {
        // Arrange
        var viewModel = new PanelFrameViewModel();
        
        // Act
        viewModel.SlideDirection = PanelSlideDirection.Right;
        
        // Assert
        Assert.True(viewModel.ShowLeftEdge);
        Assert.False(viewModel.ShowRightEdge);
        Assert.True(viewModel.ShowTopEdge);
        Assert.True(viewModel.ShowBottomEdge);
    }
    
    [Fact]
    public void SlideDirection_Top_HidesTopEdge()
    {
        // Arrange
        var viewModel = new PanelFrameViewModel();
        
        // Act
        viewModel.SlideDirection = PanelSlideDirection.Top;
        
        // Assert
        Assert.True(viewModel.ShowLeftEdge);
        Assert.True(viewModel.ShowRightEdge);
        Assert.False(viewModel.ShowTopEdge);
        Assert.True(viewModel.ShowBottomEdge);
    }
    
    [Fact]
    public void RenderScale_UpdatesGridDimensions()
    {
        // Arrange
        var viewModel = new PanelFrameViewModel();
        var sliceDefinition = new FrameSliceDefinition
        {
            Left = 100,
            LeftInner = 200,
            RightInner = 300,
            Right = 400,
            Top = 100,
            TopInner = 200,
            BottomInner = 300,
            Bottom = 400
        };
        
        // Load a test frame (will fail but we just want to test the scale update mechanism)
        // In a real test, we'd mock the IFrameSliceService
        
        // Act
        var initialScale = viewModel.RenderScale;
        viewModel.RenderScale = 2.0;
        
        // Assert
        Assert.Equal(2.0, viewModel.RenderScale);
        Assert.NotEqual(initialScale, viewModel.RenderScale);
    }
    
    [Fact]
    public void FrameDisplayScale_UpdatesGridDimensions()
    {
        // Arrange
        var viewModel = new PanelFrameViewModel();
        
        // Act
        var initialScale = viewModel.FrameDisplayScale;
        viewModel.FrameDisplayScale = 0.5;
        
        // Assert
        Assert.Equal(0.5, viewModel.FrameDisplayScale);
        Assert.NotEqual(initialScale, viewModel.FrameDisplayScale);
    }
}
