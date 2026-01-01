using System.IO;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.Logging;

namespace Avalonia.Mcp.Core.Services;

/// <summary>
/// Implementation of IAvaloniaUiService for interacting with the Avalonia UI.
/// </summary>
public class AvaloniaUiService : IAvaloniaUiService
{
    private readonly ILogger<AvaloniaUiService>? _logger;
    private readonly Func<Window?> _windowProvider;

    public AvaloniaUiService(ILogger<AvaloniaUiService>? logger = null, Func<Window?>? windowProvider = null)
    {
        _logger = logger;
        _windowProvider = windowProvider ?? GetDefaultMainWindow;
    }

    public Window? GetMainWindow()
    {
        return _windowProvider();
    }

    private static Window? GetDefaultMainWindow()
    {
        // Try to get the active window or first window from the application
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow ?? desktop.Windows.FirstOrDefault();
        }

        return null;
    }

    public async Task<bool> ClickAtAsync(double x, double y, CancellationToken cancellationToken = default)
    {
        try
        {
            var window = GetMainWindow();
            if (window == null)
            {
                _logger?.LogWarning("No main window available for click operation");
                return false;
            }

            var success = false;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    var point = new Point(x, y);
                    
                    // Find the element at the specified coordinates
                    var element = window.InputHitTest(point) as InputElement;
                    
                    if (element != null)
                    {
                        // Simulate pointer press and release
                        var pointerPressedEventArgs = new PointerPressedEventArgs(
                            element,
                            new Pointer(0, PointerType.Mouse, true),
                            (Visual)element,
                            point,
                            (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
                            KeyModifiers.None);

                        element.RaiseEvent(pointerPressedEventArgs);

                        var pointerReleasedEventArgs = new PointerReleasedEventArgs(
                            element,
                            new Pointer(0, PointerType.Mouse, true),
                            (Visual)element,
                            point,
                            (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased),
                            KeyModifiers.None,
                            MouseButton.Left);

                        element.RaiseEvent(pointerReleasedEventArgs);

                        _logger?.LogInformation("Clicked at ({X}, {Y})", x, y);
                        success = true;
                    }
                    else
                    {
                        _logger?.LogWarning("No element found at ({X}, {Y})", x, y);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error during click operation");
                }
            });

            return success;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to click at ({X}, {Y})", x, y);
            return false;
        }
    }

    public async Task<bool> TypeTextAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var window = GetMainWindow();
            if (window == null)
            {
                _logger?.LogWarning("No main window available for typing operation");
                return false;
            }

            var success = false;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    // Get focused element from TopLevel
                    var topLevel = TopLevel.GetTopLevel(window);
                    var focusedElement = topLevel?.FocusManager?.GetFocusedElement() as InputElement;
                    
                    if (focusedElement != null)
                    {
                        // Simulate text input for each character
                        foreach (var c in text)
                        {
                            var textInputEventArgs = new TextInputEventArgs
                            {
                                Text = c.ToString(),
                                RoutedEvent = InputElement.TextInputEvent,
                                Source = focusedElement
                            };

                            focusedElement.RaiseEvent(textInputEventArgs);
                        }

                        _logger?.LogInformation("Typed text: {Text}", text);
                        success = true;
                    }
                    else
                    {
                        _logger?.LogWarning("No focused element for typing");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error during typing operation");
                }
            });

            return success;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to type text: {Text}", text);
            return false;
        }
    }

    public async Task<string?> CaptureScreenshotAsync(string format = "png", CancellationToken cancellationToken = default)
    {
        try
        {
            var window = GetMainWindow();
            if (window == null)
            {
                _logger?.LogWarning("No main window available for screenshot");
                return null;
            }

            string? base64Data = null;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    var pixelSize = new PixelSize((int)window.Width, (int)window.Height);
                    var dpiScale = window.RenderScaling;
                    var size = new Size(window.Width, window.Height);

                    using var renderTarget = new RenderTargetBitmap(pixelSize, new Vector(96 * dpiScale, 96 * dpiScale));
                    renderTarget.Render(window);

                    using var memoryStream = new MemoryStream();
                    
                    if (format.Equals("jpg", StringComparison.OrdinalIgnoreCase) || 
                        format.Equals("jpeg", StringComparison.OrdinalIgnoreCase))
                    {
                        // Avalonia doesn't have built-in JPEG encoding, so we'll use PNG
                        _logger?.LogWarning("JPEG format not directly supported, using PNG instead");
                        renderTarget.Save(memoryStream);
                    }
                    else
                    {
                        renderTarget.Save(memoryStream);
                    }

                    var bytes = memoryStream.ToArray();
                    base64Data = Convert.ToBase64String(bytes);

                    _logger?.LogInformation("Captured screenshot ({Format}, {Size} bytes)", format, bytes.Length);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error during screenshot capture");
                }
            });

            return base64Data;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to capture screenshot");
            return null;
        }
    }

    public async Task<object?> GetElementTreeAsync(int maxDepth = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var window = GetMainWindow();
            if (window == null)
            {
                _logger?.LogWarning("No main window available for element tree");
                return null;
            }

            object? result = null;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    result = BuildElementTree(window, 0, maxDepth);
                    _logger?.LogInformation("Built element tree with max depth {MaxDepth}", maxDepth);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error building element tree");
                }
            });

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get element tree");
            return null;
        }
    }

    private object BuildElementTree(Visual visual, int currentDepth, int maxDepth)
    {
        var elementType = visual.GetType().Name;
        var elementName = (visual as Control)?.Name ?? "";
        var automationId = AutomationProperties.GetAutomationId(visual) ?? "";
        
        var bounds = visual.Bounds;
        
        var children = new List<object>();
        
        if (currentDepth < maxDepth)
        {
            var visualChildren = visual.GetVisualChildren();
            foreach (var child in visualChildren)
            {
                children.Add(BuildElementTree(child, currentDepth + 1, maxDepth));
            }
        }

        return new
        {
            type = elementType,
            name = elementName,
            automationId = automationId,
            bounds = new { x = bounds.X, y = bounds.Y, width = bounds.Width, height = bounds.Height },
            isVisible = visual.IsVisible,
            children = children.ToArray()
        };
    }

    public async Task<object?> FindElementAsync(string selector, CancellationToken cancellationToken = default)
    {
        try
        {
            var window = GetMainWindow();
            if (window == null)
            {
                _logger?.LogWarning("No main window available for element search");
                return null;
            }

            object? result = null;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    var element = FindElementRecursive(window, selector);
                    
                    if (element != null)
                    {
                        var bounds = element.Bounds;
                        result = new
                        {
                            found = true,
                            type = element.GetType().Name,
                            name = (element as Control)?.Name ?? "",
                            automationId = AutomationProperties.GetAutomationId(element) ?? "",
                            bounds = new { x = bounds.X, y = bounds.Y, width = bounds.Width, height = bounds.Height },
                            isVisible = element.IsVisible
                        };
                        
                        _logger?.LogInformation("Found element matching selector: {Selector}", selector);
                    }
                    else
                    {
                        result = new { found = false, selector };
                        _logger?.LogInformation("Element not found for selector: {Selector}", selector);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error during element search");
                    result = new { found = false, selector, error = ex.Message };
                }
            });

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to find element: {Selector}", selector);
            return new { found = false, selector, error = ex.Message };
        }
    }

    private Visual? FindElementRecursive(Visual visual, string selector)
    {
        // Check if current element matches the selector
        if (MatchesSelector(visual, selector))
        {
            return visual;
        }

        // Search in children
        foreach (var child in visual.GetVisualChildren())
        {
            var found = FindElementRecursive(child, selector);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private bool MatchesSelector(Visual visual, string selector)
    {
        var control = visual as Control;
        
        // Match by name
        if (!string.IsNullOrEmpty(control?.Name) && 
            control.Name.Equals(selector, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Match by automation ID
        var automationId = AutomationProperties.GetAutomationId(visual);
        if (!string.IsNullOrEmpty(automationId) && 
            automationId.Equals(selector, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Match by type name
        if (visual.GetType().Name.Equals(selector, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    public async Task<bool> WaitForElementAsync(string selector, int timeoutMs = 5000, CancellationToken cancellationToken = default)
    {
        try
        {
            var startTime = DateTimeOffset.UtcNow;
            var pollInterval = 100; // Check every 100ms

            while ((DateTimeOffset.UtcNow - startTime).TotalMilliseconds < timeoutMs)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger?.LogInformation("Wait for element cancelled: {Selector}", selector);
                    return false;
                }

                var result = await FindElementAsync(selector, cancellationToken);
                
                // Check if the result indicates the element was found
                // Use reflection to access the 'found' property
                if (result != null)
                {
                    var foundProperty = result.GetType().GetProperty("found");
                    if (foundProperty != null)
                    {
                        var foundValue = foundProperty.GetValue(result);
                        if (foundValue is bool found && found)
                        {
                            _logger?.LogInformation("Element found within timeout: {Selector}", selector);
                            return true;
                        }
                    }
                }

                await Task.Delay(pollInterval, cancellationToken);
            }

            _logger?.LogWarning("Element not found within timeout ({TimeoutMs}ms): {Selector}", timeoutMs, selector);
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to wait for element: {Selector}", selector);
            return false;
        }
    }
}
