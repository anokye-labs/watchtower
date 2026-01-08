using Avalonia.Mcp.Core.Handlers;
using Avalonia.Mcp.Core.Models;
using Avalonia.Mcp.Core.Services;
using Microsoft.Extensions.Logging;

namespace Avalonia.Mcp.Core.Tools;

/// <summary>
/// Provides standard UI interaction tools for Avalonia applications.
/// These tools work in both headless and GUI modes.
/// </summary>
public class StandardUiTools
{
    private readonly IMcpHandler _handler;
    private readonly IAvaloniaUiService _uiService;
    private readonly ILogger<StandardUiTools>? _logger;

    public StandardUiTools(IMcpHandler handler, IAvaloniaUiService uiService, ILogger<StandardUiTools>? logger = null)
    {
        _handler = handler;
        _uiService = uiService;
        _logger = logger;
    }

    /// <summary>
    /// Registers all standard UI tools with the MCP handler.
    /// </summary>
    public void RegisterTools()
    {
        RegisterClickElementTool();
        RegisterTypeTextTool();
        RegisterCaptureScreenshotTool();
        RegisterGetElementTreeTool();
        RegisterFindElementTool();
        RegisterWaitForElementTool();
    }

    private void RegisterClickElementTool()
    {
        var tool = new McpToolDefinition
        {
            Name = "ClickElement",
            Description = "Click on a UI element at the specified coordinates",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    x = new { type = "number", description = "X coordinate" },
                    y = new { type = "number", description = "Y coordinate" }
                },
                required = new[] { "x", "y" }
            }
        };

        _handler.RegisterTool(tool, async (parameters) =>
        {
            try
            {
                if (parameters == null)
                    return McpToolResult.Fail("Parameters are required");

                if (!parameters.TryGetValue("x", out var xObj) || !parameters.TryGetValue("y", out var yObj))
                    return McpToolResult.Fail("Missing x or y coordinates");

                var x = Convert.ToDouble(xObj);
                var y = Convert.ToDouble(yObj);

                _logger?.LogInformation("Clicking at ({X}, {Y})", x, y);

                var clicked = await _uiService.ClickAtAsync(x, y);

                return McpToolResult.Ok(new { x, y, clicked });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing ClickElement");
                return McpToolResult.Fail($"Click failed: {ex.Message}");
            }
        });
    }

    private void RegisterTypeTextTool()
    {
        var tool = new McpToolDefinition
        {
            Name = "TypeText",
            Description = "Type text into the focused element",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    text = new { type = "string", description = "Text to type" }
                },
                required = new[] { "text" }
            }
        };

        _handler.RegisterTool(tool, async (parameters) =>
        {
            try
            {
                if (parameters == null || !parameters.TryGetValue("text", out var textObj))
                    return McpToolResult.Fail("Text parameter is required");

                var text = textObj.ToString() ?? "";
                _logger?.LogInformation("Typing text: {Text}", text);

                var typed = await _uiService.TypeTextAsync(text);

                return McpToolResult.Ok(new { text, typed });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing TypeText");
                return McpToolResult.Fail($"Type failed: {ex.Message}");
            }
        });
    }

    private void RegisterCaptureScreenshotTool()
    {
        var tool = new McpToolDefinition
        {
            Name = "CaptureScreenshot",
            Description = "Capture a screenshot of the application window",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    format = new { type = "string", description = "Image format (png, jpg)", @default = "png" }
                }
            }
        };

        _handler.RegisterTool(tool, async (parameters) =>
        {
            try
            {
                var format = parameters?.TryGetValue("format", out var formatObj) == true
                    ? formatObj.ToString() ?? "png"
                    : "png";

                _logger?.LogInformation("Capturing screenshot in format: {Format}", format);

                var base64Data = await _uiService.CaptureScreenshotAsync(format);

                return base64Data != null 
                    ? McpToolResult.Ok(new
                    {
                        format,
                        base64Data,
                        timestamp = DateTime.UtcNow
                    })
                    : McpToolResult.Fail("Failed to capture screenshot");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing CaptureScreenshot");
                return McpToolResult.Fail($"Screenshot failed: {ex.Message}");
            }
        });
    }

    private void RegisterGetElementTreeTool()
    {
        var tool = new McpToolDefinition
        {
            Name = "GetElementTree",
            Description = "Get the current UI element tree structure",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    maxDepth = new { type = "number", description = "Maximum tree depth", @default = 10 }
                }
            }
        };

        _handler.RegisterTool(tool, async (parameters) =>
        {
            try
            {
                var maxDepth = parameters?.TryGetValue("maxDepth", out var depthObj) == true
                    ? Convert.ToInt32(depthObj)
                    : 10;

                _logger?.LogInformation("Getting element tree with max depth: {MaxDepth}", maxDepth);

                var tree = await _uiService.GetElementTreeAsync(maxDepth);

                return tree != null 
                    ? McpToolResult.Ok(new { root = tree })
                    : McpToolResult.Fail("Failed to get element tree");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing GetElementTree");
                return McpToolResult.Fail($"Get element tree failed: {ex.Message}");
            }
        });
    }

    private void RegisterFindElementTool()
    {
        var tool = new McpToolDefinition
        {
            Name = "FindElement",
            Description = "Find a UI element by selector (name, type, or automation ID)",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    selector = new { type = "string", description = "Element selector" }
                },
                required = new[] { "selector" }
            }
        };

        _handler.RegisterTool(tool, async (parameters) =>
        {
            try
            {
                if (parameters == null || !parameters.TryGetValue("selector", out var selectorObj))
                    return McpToolResult.Fail("Selector parameter is required");

                var selector = selectorObj.ToString() ?? "";
                _logger?.LogInformation("Finding element: {Selector}", selector);

                var result = await _uiService.FindElementAsync(selector);

                return McpToolResult.Ok(result);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing FindElement");
                return McpToolResult.Fail($"Find element failed: {ex.Message}");
            }
        });
    }

    private void RegisterWaitForElementTool()
    {
        var tool = new McpToolDefinition
        {
            Name = "WaitForElement",
            Description = "Wait for a UI element to appear",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    selector = new { type = "string", description = "Element selector" },
                    timeoutMs = new { type = "number", description = "Timeout in milliseconds", @default = 5000 }
                },
                required = new[] { "selector" }
            }
        };

        _handler.RegisterTool(tool, async (parameters) =>
        {
            try
            {
                if (parameters == null || !parameters.TryGetValue("selector", out var selectorObj))
                    return McpToolResult.Fail("Selector parameter is required");

                var selector = selectorObj.ToString() ?? "";
                var timeoutMs = parameters.TryGetValue("timeoutMs", out var timeoutObj)
                    ? Convert.ToInt32(timeoutObj)
                    : 5000;

                _logger?.LogInformation("Waiting for element: {Selector} (timeout: {TimeoutMs}ms)", selector, timeoutMs);

                var found = await _uiService.WaitForElementAsync(selector, timeoutMs);

                return McpToolResult.Ok(new
                {
                    selector,
                    found,
                    timeoutMs,
                    message = found ? "Element found" : "Element not found within timeout"
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing WaitForElement");
                return McpToolResult.Fail($"Wait for element failed: {ex.Message}");
            }
        });
    }
}
