using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace Avalonia.Mcp.Core.Services;

/// <summary>
/// Service interface for interacting with the Avalonia UI system.
/// Provides methods for input simulation, screenshot capture, and element tree inspection.
/// </summary>
public interface IAvaloniaUiService
{
    /// <summary>
    /// Simulates a mouse click at the specified coordinates.
    /// </summary>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the click was successful.</returns>
    Task<bool> ClickAtAsync(double x, double y, CancellationToken cancellationToken = default);

    /// <summary>
    /// Simulates typing text into the currently focused element.
    /// </summary>
    /// <param name="text">The text to type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if typing was successful.</returns>
    Task<bool> TypeTextAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Captures a screenshot of the application window.
    /// </summary>
    /// <param name="format">The image format (png or jpg).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Base64-encoded screenshot data, or null if capture failed.</returns>
    Task<string?> CaptureScreenshotAsync(string format = "png", CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the element tree structure starting from the root.
    /// </summary>
    /// <param name="maxDepth">Maximum depth to traverse.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A hierarchical representation of the element tree.</returns>
    Task<object?> GetElementTreeAsync(int maxDepth = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an element by selector (name, type, or automation ID).
    /// </summary>
    /// <param name="selector">The selector string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Information about the found element, or null if not found.</returns>
    Task<object?> FindElementAsync(string selector, CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits for an element matching the selector to appear.
    /// </summary>
    /// <param name="selector">The selector string.</param>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the element was found within the timeout.</returns>
    Task<bool> WaitForElementAsync(string selector, int timeoutMs = 5000, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the main application window.
    /// </summary>
    /// <returns>The main window, or null if not available.</returns>
    Window? GetMainWindow();
}
