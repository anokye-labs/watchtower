using Avalonia;
using Avalonia.Media.Imaging;

namespace WatchTower.Services;

/// <summary>
/// Defines slice coordinates for 9-slice frame extraction.
/// All coordinates are absolute pixel positions in the source image.
/// </summary>
public record FrameSliceDefinition
{
    /// <summary>
    /// X coordinate where the left column ends (and center column begins).
    /// </summary>
    public int Left { get; init; }
    
    /// <summary>
    /// Y coordinate where the top row ends (and center row begins).
    /// </summary>
    public int Top { get; init; }
    
    /// <summary>
    /// X coordinate where the right column begins (and center column ends).
    /// </summary>
    public int Right { get; init; }
    
    /// <summary>
    /// Y coordinate where the bottom row begins (and center row ends).
    /// </summary>
    public int Bottom { get; init; }
}

/// <summary>
/// Contains the 9 sliced bitmaps extracted from a source frame image.
/// </summary>
public record FrameSlices
{
    public required Bitmap TopLeft { get; init; }
    public required Bitmap TopCenter { get; init; }
    public required Bitmap TopRight { get; init; }
    public required Bitmap MiddleLeft { get; init; }
    public required Bitmap MiddleRight { get; init; }
    public required Bitmap BottomLeft { get; init; }
    public required Bitmap BottomCenter { get; init; }
    public required Bitmap BottomRight { get; init; }
    
    /// <summary>
    /// Original source image dimensions.
    /// </summary>
    public Size SourceSize { get; init; }
    
    /// <summary>
    /// The slice definition used to create these slices.
    /// </summary>
    public required FrameSliceDefinition SliceDefinition { get; init; }
}

/// <summary>
/// Service for loading and slicing frame images using the 9-slice technique.
/// </summary>
public interface IFrameSliceService
{
    /// <summary>
    /// Loads a source image and slices it into 9 regions based on the provided definition.
    /// </summary>
    /// <param name="sourceUri">URI to the source image (avares:// or file path).</param>
    /// <param name="sliceDefinition">The slice coordinates defining the 9 regions.</param>
    /// <returns>The sliced frame bitmaps, or null if loading fails.</returns>
    FrameSlices? LoadAndSlice(string sourceUri, FrameSliceDefinition sliceDefinition);
    
    /// <summary>
    /// Loads a source image and slices it using percentage-based coordinates.
    /// Useful when the slice points are proportional to the image size.
    /// </summary>
    /// <param name="sourceUri">URI to the source image.</param>
    /// <param name="leftPercent">Left inset as percentage of width (0.0 to 1.0).</param>
    /// <param name="topPercent">Top inset as percentage of height (0.0 to 1.0).</param>
    /// <param name="rightPercent">Right inset as percentage of width (0.0 to 1.0).</param>
    /// <param name="bottomPercent">Bottom inset as percentage of height (0.0 to 1.0).</param>
    /// <returns>The sliced frame bitmaps, or null if loading fails.</returns>
    FrameSlices? LoadAndSliceByPercent(string sourceUri, double leftPercent, double topPercent, double rightPercent, double bottomPercent);
    
    /// <summary>
    /// Loads and resizes a source image to target dimensions, then slices it.
    /// Results are cached by resolution (LRU-5 cache).
    /// </summary>
    /// <param name="sourceUri">URI to the source image.</param>
    /// <param name="sliceDefinition">The slice coordinates (relative to original source size).</param>
    /// <param name="targetWidth">Target width to resize the image to before slicing.</param>
    /// <param name="targetHeight">Target height to resize the image to before slicing.</param>
    /// <returns>The sliced frame bitmaps at the target resolution, or null if loading fails.</returns>
    FrameSlices? LoadResizeAndSlice(string sourceUri, FrameSliceDefinition sliceDefinition, int targetWidth, int targetHeight);
    
    /// <summary>
    /// Clears the resolution cache.
    /// </summary>
    void ClearCache();
}
