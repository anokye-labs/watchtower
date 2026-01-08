using Avalonia.Mcp.Core.Handlers;
using Avalonia.Mcp.Core.Models;
using Avalonia.Mcp.Core.Services;
using Avalonia.Mcp.Core.Tools;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Avalonia.Mcp.Core.Tests.Tools;

public class StandardUiToolsTests
{
    private readonly Mock<IMcpHandler> _handlerMock;
    private readonly Mock<IAvaloniaUiService> _uiServiceMock;
    private readonly Mock<ILogger<StandardUiTools>> _loggerMock;

    public StandardUiToolsTests()
    {
        _handlerMock = new Mock<IMcpHandler>();
        _uiServiceMock = new Mock<IAvaloniaUiService>();
        _loggerMock = new Mock<ILogger<StandardUiTools>>();
    }

    [Fact]
    public void Constructor_WithValidParameters_DoesNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => 
            new StandardUiTools(_handlerMock.Object, _uiServiceMock.Object, _loggerMock.Object));
        Assert.Null(exception);
    }

    [Fact]
    public void RegisterTools_CallsRegisterToolForAllTools()
    {
        // Arrange
        var tools = new StandardUiTools(_handlerMock.Object, _uiServiceMock.Object, _loggerMock.Object);
        var registeredTools = new List<string>();

        _handlerMock
            .Setup(h => h.RegisterTool(It.IsAny<McpToolDefinition>(), It.IsAny<Func<Dictionary<string, object>?, Task<McpToolResult>>>()))
            .Callback<McpToolDefinition, Func<Dictionary<string, object>?, Task<McpToolResult>>>(
                (def, handler) => registeredTools.Add(def.Name));

        // Act
        tools.RegisterTools();

        // Assert
        Assert.Contains("ClickElement", registeredTools);
        Assert.Contains("TypeText", registeredTools);
        Assert.Contains("CaptureScreenshot", registeredTools);
        Assert.Contains("GetElementTree", registeredTools);
        Assert.Contains("FindElement", registeredTools);
        Assert.Contains("WaitForElement", registeredTools);
        Assert.Equal(6, registeredTools.Count);
    }

    [Fact]
    public async Task ClickElement_WithValidCoordinates_CallsUiService()
    {
        // Arrange
        var tools = new StandardUiTools(_handlerMock.Object, _uiServiceMock.Object, _loggerMock.Object);
        Func<Dictionary<string, object>?, Task<McpToolResult>>? clickHandler = null;

        _handlerMock
            .Setup(h => h.RegisterTool(
                It.Is<McpToolDefinition>(d => d.Name == "ClickElement"),
                It.IsAny<Func<Dictionary<string, object>?, Task<McpToolResult>>>()))
            .Callback<McpToolDefinition, Func<Dictionary<string, object>?, Task<McpToolResult>>>(
                (def, handler) => clickHandler = handler);

        _uiServiceMock
            .Setup(s => s.ClickAtAsync(100, 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        tools.RegisterTools();

        // Act
        var parameters = new Dictionary<string, object> { { "x", 100.0 }, { "y", 200.0 } };
        var result = await clickHandler!(parameters);

        // Assert
        Assert.True(result.Success);
        _uiServiceMock.Verify(s => s.ClickAtAsync(100, 200, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TypeText_WithValidText_CallsUiService()
    {
        // Arrange
        var tools = new StandardUiTools(_handlerMock.Object, _uiServiceMock.Object, _loggerMock.Object);
        Func<Dictionary<string, object>?, Task<McpToolResult>>? typeHandler = null;

        _handlerMock
            .Setup(h => h.RegisterTool(
                It.Is<McpToolDefinition>(d => d.Name == "TypeText"),
                It.IsAny<Func<Dictionary<string, object>?, Task<McpToolResult>>>()))
            .Callback<McpToolDefinition, Func<Dictionary<string, object>?, Task<McpToolResult>>>(
                (def, handler) => typeHandler = handler);

        _uiServiceMock
            .Setup(s => s.TypeTextAsync("Hello World", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        tools.RegisterTools();

        // Act
        var parameters = new Dictionary<string, object> { { "text", "Hello World" } };
        var result = await typeHandler!(parameters);

        // Assert
        Assert.True(result.Success);
        _uiServiceMock.Verify(s => s.TypeTextAsync("Hello World", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CaptureScreenshot_WithValidFormat_CallsUiService()
    {
        // Arrange
        var tools = new StandardUiTools(_handlerMock.Object, _uiServiceMock.Object, _loggerMock.Object);
        Func<Dictionary<string, object>?, Task<McpToolResult>>? screenshotHandler = null;

        _handlerMock
            .Setup(h => h.RegisterTool(
                It.Is<McpToolDefinition>(d => d.Name == "CaptureScreenshot"),
                It.IsAny<Func<Dictionary<string, object>?, Task<McpToolResult>>>()))
            .Callback<McpToolDefinition, Func<Dictionary<string, object>?, Task<McpToolResult>>>(
                (def, handler) => screenshotHandler = handler);

        _uiServiceMock
            .Setup(s => s.CaptureScreenshotAsync("png", It.IsAny<CancellationToken>()))
            .ReturnsAsync("base64data");

        tools.RegisterTools();

        // Act
        var parameters = new Dictionary<string, object> { { "format", "png" } };
        var result = await screenshotHandler!(parameters);

        // Assert
        Assert.True(result.Success);
        _uiServiceMock.Verify(s => s.CaptureScreenshotAsync("png", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetElementTree_WithMaxDepth_CallsUiService()
    {
        // Arrange
        var tools = new StandardUiTools(_handlerMock.Object, _uiServiceMock.Object, _loggerMock.Object);
        Func<Dictionary<string, object>?, Task<McpToolResult>>? treeHandler = null;

        _handlerMock
            .Setup(h => h.RegisterTool(
                It.Is<McpToolDefinition>(d => d.Name == "GetElementTree"),
                It.IsAny<Func<Dictionary<string, object>?, Task<McpToolResult>>>()))
            .Callback<McpToolDefinition, Func<Dictionary<string, object>?, Task<McpToolResult>>>(
                (def, handler) => treeHandler = handler);

        var mockTree = new { type = "Window", name = "TestWindow" };
        _uiServiceMock
            .Setup(s => s.GetElementTreeAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockTree);

        tools.RegisterTools();

        // Act
        var parameters = new Dictionary<string, object> { { "maxDepth", 5 } };
        var result = await treeHandler!(parameters);

        // Assert
        Assert.True(result.Success);
        _uiServiceMock.Verify(s => s.GetElementTreeAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindElement_WithSelector_CallsUiService()
    {
        // Arrange
        var tools = new StandardUiTools(_handlerMock.Object, _uiServiceMock.Object, _loggerMock.Object);
        Func<Dictionary<string, object>?, Task<McpToolResult>>? findHandler = null;

        _handlerMock
            .Setup(h => h.RegisterTool(
                It.Is<McpToolDefinition>(d => d.Name == "FindElement"),
                It.IsAny<Func<Dictionary<string, object>?, Task<McpToolResult>>>()))
            .Callback<McpToolDefinition, Func<Dictionary<string, object>?, Task<McpToolResult>>>(
                (def, handler) => findHandler = handler);

        var mockResult = new ElementSearchResult { Found = true, Name = "TestButton" };
        _uiServiceMock
            .Setup(s => s.FindElementAsync("TestButton", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResult);

        tools.RegisterTools();

        // Act
        var parameters = new Dictionary<string, object> { { "selector", "TestButton" } };
        var result = await findHandler!(parameters);

        // Assert
        Assert.True(result.Success);
        _uiServiceMock.Verify(s => s.FindElementAsync("TestButton", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WaitForElement_WithSelectorAndTimeout_CallsUiService()
    {
        // Arrange
        var tools = new StandardUiTools(_handlerMock.Object, _uiServiceMock.Object, _loggerMock.Object);
        Func<Dictionary<string, object>?, Task<McpToolResult>>? waitHandler = null;

        _handlerMock
            .Setup(h => h.RegisterTool(
                It.Is<McpToolDefinition>(d => d.Name == "WaitForElement"),
                It.IsAny<Func<Dictionary<string, object>?, Task<McpToolResult>>>()))
            .Callback<McpToolDefinition, Func<Dictionary<string, object>?, Task<McpToolResult>>>(
                (def, handler) => waitHandler = handler);

        _uiServiceMock
            .Setup(s => s.WaitForElementAsync("TestButton", 3000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        tools.RegisterTools();

        // Act
        var parameters = new Dictionary<string, object> { { "selector", "TestButton" }, { "timeoutMs", 3000 } };
        var result = await waitHandler!(parameters);

        // Assert
        Assert.True(result.Success);
        _uiServiceMock.Verify(s => s.WaitForElementAsync("TestButton", 3000, It.IsAny<CancellationToken>()), Times.Once);
    }
}
