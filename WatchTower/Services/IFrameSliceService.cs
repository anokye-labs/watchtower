using Avalonia;
using Avalonia.Media.Imaging;

namespace WatchTower.Services;

/// <summary>
/// Defines slice coordinates for 25-slice (5x5) frame extraction.
/// All coordinates are absolute pixel positions in the source image.
/// The 5x5 grid provides: corners (fixed), edge centers (fixed), and stretch sections.
/// </summary>
public record FrameSliceDefinition
{
    /// <summary>
    /// X coordinate where the left corner column ends (column 0 to 1 boundary).
    /// </summary>
    public int Left { get; init; }

    /// <summary>
    /// X coordinate where the left stretch section ends and center section begins (column 1 to 2 boundary).
    /// </summary>
    public int LeftInner { get; init; }

    /// <summary>
    /// X coordinate where the center section ends and right stretch section begins (column 2 to 3 boundary).
    /// </summary>
    public int RightInner { get; init; }

    /// <summary>
    /// X coordinate where the right stretch section ends and right corner begins (column 3 to 4 boundary).
    /// </summary>
    public int Right { get; init; }

    /// <summary>
    /// Y coordinate where the top corner row ends (row 0 to 1 boundary).
    /// </summary>
    public int Top { get; init; }

    /// <summary>
    /// Y coordinate where the top stretch section ends and center section begins (row 1 to 2 boundary).
    /// </summary>
    public int TopInner { get; init; }

    /// <summary>
    /// Y coordinate where the center section ends and bottom stretch section begins (row 2 to 3 boundary).
    /// </summary>
    public int BottomInner { get; init; }

    /// <summary>
    /// Y coordinate where the bottom stretch section ends and bottom corner begins (row 3 to 4 boundary).
    /// </summary>
    public int Bottom { get; init; }
}

/// <summary>
/// Contains the 16 sliced bitmaps extracted from a source frame image (5x5 grid, excluding center 3x3).
/// Grid layout:
///   Col:  0          1           2           3           4
///   Row 0: TopLeft    TopLeftS    TopCenter   TopRightS   TopRight
///   Row 1: LeftTopS   (content)   (content)   (content)   RightTopS
///   Row 2: LeftCenter (content)   (content)   (content)   RightCenter
///   Row 3: LeftBottomS(content)   (content)   (content)   RightBottomS
///   Row 4: BottomLeft BottomLeftS BottomCenter BottomRightS BottomRight
/// Where 'S' = Stretch section.
/// </summary>
public record FrameSlices
{
    // Row 0: Top edge
    public required Bitmap TopLeft { get; init; }
    public required Bitmap TopLeftStretch { get; init; }
    public required Bitmap TopCenter { get; init; }
    public required Bitmap TopRightStretch { get; init; }
    public required Bitmap TopRight { get; init; }

    // Column 0: Left edge (rows 1-3)
    public required Bitmap LeftTopStretch { get; init; }
    public required Bitmap LeftCenter { get; init; }
    public required Bitmap LeftBottomStretch { get; init; }

    // Column 4: Right edge (rows 1-3)
    public required Bitmap RightTopStretch { get; init; }
    public required Bitmap RightCenter { get; init; }
    public required Bitmap RightBottomStretch { get; init; }

    // Row 4: Bottom edge
    public required Bitmap BottomLeft { get; init; }
    public required Bitmap BottomLeftStretch { get; init; }
    public required Bitmap BottomCenter { get; init; }
    public required Bitmap BottomRightStretch { get; init; }
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
/// Service for loading and slicing frame images using the 25-slice (5x5) technique.
/// </summary>
public interface IFrameSliceService
{
    /// <summary>
    /// Loads a source image and slices it into 16 border regions based on the provided definition.
    /// </summary>
    /// <param name="sourceUri">URI to the source image (avares:// or file path).</param>
    /// <param name="sliceDefinition">The slice coordinates defining the 5x5 grid boundaries.</param>
    /// <returns>The sliced frame bitmaps, or null if loading fails.</returns>
    FrameSlices? LoadAndSlice(string sourceUri, FrameSliceDefinition sliceDefinition);

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
