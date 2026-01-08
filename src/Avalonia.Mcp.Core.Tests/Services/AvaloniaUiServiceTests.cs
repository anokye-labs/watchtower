using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Mcp.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Avalonia.Mcp.Core.Tests.Services;

public class AvaloniaUiServiceTests
{
    private readonly Mock<ILogger<AvaloniaUiService>> _loggerMock;

    public AvaloniaUiServiceTests()
    {
        _loggerMock = new Mock<ILogger<AvaloniaUiService>>();
    }

    [AvaloniaFact]
    public void Constructor_WithNullLogger_DoesNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => new AvaloniaUiService(null, null));
        Assert.Null(exception);
    }

    [AvaloniaFact]
    public void Constructor_WithLogger_DoesNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => new AvaloniaUiService(_loggerMock.Object, null));
        Assert.Null(exception);
    }

    [AvaloniaFact]
    public void GetMainWindow_WithCustomProvider_ReturnsProvidedWindow()
    {
        // Arrange
        var testWindow = new Window();
        var service = new AvaloniaUiService(_loggerMock.Object, () => testWindow);

        // Act
        var result = service.GetMainWindow();

        // Assert
        Assert.Same(testWindow, result);
    }

    [AvaloniaFact]
    public void GetMainWindow_WithNullProvider_ReturnsNull()
    {
        // Arrange
        var service = new AvaloniaUiService(_loggerMock.Object, () => null);

        // Act
        var result = service.GetMainWindow();

        // Assert
        Assert.Null(result);
    }

    [AvaloniaFact]
    public async Task ClickAtAsync_WithNullWindow_ReturnsFalse()
    {
        // Arrange
        var service = new AvaloniaUiService(_loggerMock.Object, () => null);

        // Act
        var result = await service.ClickAtAsync(10, 10);

        // Assert
        Assert.False(result);
    }

    [AvaloniaFact]
    public async Task TypeTextAsync_WithNullWindow_ReturnsFalse()
    {
        // Arrange
        var service = new AvaloniaUiService(_loggerMock.Object, () => null);

        // Act
        var result = await service.TypeTextAsync("test");

        // Assert
        Assert.False(result);
    }

    [AvaloniaFact]
    public async Task CaptureScreenshotAsync_WithNullWindow_ReturnsNull()
    {
        // Arrange
        var service = new AvaloniaUiService(_loggerMock.Object, () => null);

        // Act
        var result = await service.CaptureScreenshotAsync();

        // Assert
        Assert.Null(result);
    }

    [AvaloniaFact]
    public async Task GetElementTreeAsync_WithNullWindow_ReturnsNull()
    {
        // Arrange
        var service = new AvaloniaUiService(_loggerMock.Object, () => null);

        // Act
        var result = await service.GetElementTreeAsync();

        // Assert
        Assert.Null(result);
    }

    [AvaloniaFact]
    public async Task FindElementAsync_WithNullWindow_ReturnsNotFound()
    {
        // Arrange
        var service = new AvaloniaUiService(_loggerMock.Object, () => null);

        // Act
        var result = await service.FindElementAsync("TestElement");

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Found);
    }

    [AvaloniaFact]
    public async Task WaitForElementAsync_WithNullWindow_ReturnsFalse()
    {
        // Arrange
        var service = new AvaloniaUiService(_loggerMock.Object, () => null);

        // Act
        var result = await service.WaitForElementAsync("TestElement", 100);

        // Assert
        Assert.False(result);
    }

    [AvaloniaFact]
    public async Task GetElementTreeAsync_WithWindow_ReturnsTree()
    {
        // Arrange
        var window = new Window
        {
            Width = 800,
            Height = 600,
            Content = new StackPanel
            {
                Name = "MainPanel",
                Children =
                {
                    new Button { Name = "TestButton", Content = "Click Me" },
                    new TextBox { Name = "TestTextBox" }
                }
            }
        };

        // Show and layout the window so it has a proper visual tree
        window.Show();
        await Task.Delay(50); // Give time for layout

        var service = new AvaloniaUiService(_loggerMock.Object, () => window);

        // Act
        var result = await service.GetElementTreeAsync(maxDepth: 5);

        // Clean up
        window.Close();

        // Assert
        Assert.NotNull(result);
    }

    [AvaloniaFact]
    public async Task FindElementAsync_WithNamedElement_FindsElement()
    {
        // Arrange
        var window = new Window
        {
            Width = 800,
            Height = 600,
            Content = new Button { Name = "TestButton", Content = "Click Me" }
        };

        // Show and layout the window
        window.Show();
        await Task.Delay(50); // Give time for layout

        var service = new AvaloniaUiService(_loggerMock.Object, () => window);

        // Act
        var result = await service.FindElementAsync("TestButton");

        // Clean up
        window.Close();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Found);
        Assert.Equal("TestButton", result.Name);
    }

    [AvaloniaFact]
    public async Task FindElementAsync_WithNonExistentElement_ReturnsNotFound()
    {
        // Arrange
        var window = new Window
        {
            Width = 800,
            Height = 600,
            Content = new Button { Name = "TestButton", Content = "Click Me" }
        };

        // Show and layout the window
        window.Show();
        await Task.Delay(50); // Give time for layout

        var service = new AvaloniaUiService(_loggerMock.Object, () => window);

        // Act
        var result = await service.FindElementAsync("NonExistentButton");

        // Clean up
        window.Close();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Found);
        Assert.Equal("NonExistentButton", result.Selector);
    }

    [AvaloniaFact]
    public async Task WaitForElementAsync_WithExistingElement_ReturnsTrue()
    {
        // Arrange
        var window = new Window
        {
            Width = 800,
            Height = 600,
            Content = new Button { Name = "TestButton", Content = "Click Me" }
        };

        // Show and layout the window
        window.Show();
        await Task.Delay(50); // Give time for layout

        var service = new AvaloniaUiService(_loggerMock.Object, () => window);

        // Act
        var result = await service.WaitForElementAsync("TestButton", 1000);

        // Clean up
        window.Close();

        // Assert
        Assert.True(result);
    }

    [AvaloniaFact]
    public async Task WaitForElementAsync_WithNonExistentElement_ReturnsFalse()
    {
        // Arrange
        var window = new Window
        {
            Width = 800,
            Height = 600,
            Content = new Button { Name = "TestButton", Content = "Click Me" }
        };

        // Show and layout the window
        window.Show();
        await Task.Delay(50); // Give time for layout

        var service = new AvaloniaUiService(_loggerMock.Object, () => window);

        // Act
        var result = await service.WaitForElementAsync("NonExistentButton", 200);

        // Clean up
        window.Close();

        // Assert
        Assert.False(result);
    }
}
