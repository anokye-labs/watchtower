using Avalonia;
using Avalonia.Input;

namespace WatchTower.Services;

/// <summary>
/// Defines the type of window resize operation based on frame region.
/// </summary>
public enum ResizeMode
{
    /// <summary>
    /// Not in a resize zone - allows window dragging or click-through.
    /// </summary>
    None,
    
    /// <summary>
    /// Top-left corner - diagonal resize (both dimensions).
    /// </summary>
    TopLeft,
    
    /// <summary>
    /// Top edge - vertical resize (height only).
    /// </summary>
    Top,
    
    /// <summary>
    /// Top-right corner - diagonal resize (both dimensions).
    /// </summary>
    TopRight,
    
    /// <summary>
    /// Left edge - horizontal resize (width only).
    /// </summary>
    Left,
    
    /// <summary>
    /// Right edge - horizontal resize (width only).
    /// </summary>
    Right,
    
    /// <summary>
    /// Bottom-left corner - diagonal resize (both dimensions).
    /// </summary>
    BottomLeft,
    
    /// <summary>
    /// Bottom edge - vertical resize (height only).
    /// </summary>
    Bottom,
    
    /// <summary>
    /// Bottom-right corner - diagonal resize (both dimensions).
    /// </summary>
    BottomRight
}

/// <summary>
/// Result of a frame hit test operation.
/// </summary>
public record FrameHitTestResult
{
    /// <summary>
    /// True if the hit point is on an opaque frame region (above alpha threshold).
    /// </summary>
    public bool IsOpaque { get; init; }
    
    /// <summary>
    /// The resize mode for this frame region.
    /// </summary>
    public ResizeMode ResizeMode { get; init; }
    
    /// <summary>
    /// The appropriate cursor for this frame region.
    /// </summary>
    public StandardCursorType CursorType { get; init; }
}

/// <summary>
/// Service for performing hit testing on the shell window frame to determine
/// if clicks should pass through (transparent regions) or be captured for
/// window interaction (opaque regions with resize/drag functionality).
/// </summary>
public interface IFrameHitTestService
{
    /// <summary>
    /// Performs a hit test on the frame at the specified window coordinates.
    /// </summary>
    /// <param name="point">Point in window coordinates (logical pixels).</param>
    /// <param name="windowSize">Current window size (logical pixels).</param>
    /// <param name="frameSlices">The current frame slices being displayed.</param>
    /// <param name="frameDisplayScale">The display scale applied to frame elements.</param>
    /// <param name="renderScale">The DPI render scale.</param>
    /// <param name="alphaThreshold">Alpha threshold (0.0-1.0) - values above this are considered opaque.</param>
    /// <param name="resizeHandleSize">Size of resize handle zone in logical pixels.</param>
    /// <returns>Hit test result indicating opacity, resize mode, and cursor type.</returns>
    FrameHitTestResult HitTest(
        Point point,
        Size windowSize,
        FrameSlices frameSlices,
        double frameDisplayScale,
        double renderScale,
        double alphaThreshold,
        double resizeHandleSize);
}
