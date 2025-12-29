using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace WatchTower.Services;

/// <summary>
/// Implementation of IFrameSliceService that loads and slices frame images.
/// Uses SkiaSharp (via Avalonia) for bitmap manipulation.
/// Includes LRU-5 cache for resized slices by resolution.
/// </summary>
public class FrameSliceService : IFrameSliceService
{
    private const int MaxCacheSize = 5;
    
    // LRU cache: key is (width, height), value is slices
    // LinkedList maintains access order for LRU eviction
    private readonly Dictionary<(int width, int height), FrameSlices> _cache = new();
    private readonly LinkedList<(int width, int height)> _cacheOrder = new();
    
    // Cache the source bitmap to avoid reloading for each resolution
    private Bitmap? _cachedSourceBitmap;
    private string? _cachedSourceUri;
    private FrameSliceDefinition? _cachedSliceDefinition;
    
    /// <inheritdoc/>
    public FrameSlices? LoadAndSlice(string sourceUri, FrameSliceDefinition sliceDefinition)
    {
        ArgumentNullException.ThrowIfNull(sourceUri);
        ArgumentNullException.ThrowIfNull(sliceDefinition);
        
        try
        {
            // Load the source bitmap
            var sourceBitmap = LoadBitmap(sourceUri);
            if (sourceBitmap == null)
            {
                return null;
            }
            
            var sourceWidth = (int)sourceBitmap.Size.Width;
            var sourceHeight = (int)sourceBitmap.Size.Height;
            
            // Validate slice definition (absolute coordinates for 5x5 grid)
            if (!ValidateSliceDefinition(sliceDefinition, sourceWidth, sourceHeight))
            {
                return null;
            }
            
            // Extract all 16 border slices using absolute coordinates
            var slices = ExtractAllSlices(sourceBitmap, sliceDefinition, sourceWidth, sourceHeight);
            if (slices == null)
            {
                return null;
            }
            
            slices = slices with { SourceSize = sourceBitmap.Size };
            
            return slices;
        }
        catch (Exception)
        {
            return null;
        }
    }
    
    /// <summary>
    /// Validates slice definition coordinates for a 5x5 grid.
    /// </summary>
    private static bool ValidateSliceDefinition(FrameSliceDefinition def, int sourceWidth, int sourceHeight)
    {
        // X coordinates must be in order: 0 < Left < LeftInner < RightInner < Right < sourceWidth
        if (def.Left <= 0 || def.Left >= def.LeftInner ||
            def.LeftInner >= def.RightInner ||
            def.RightInner >= def.Right ||
            def.Right >= sourceWidth)
        {
            return false;
        }
        
        // Y coordinates must be in order: 0 < Top < TopInner < BottomInner < Bottom < sourceHeight
        if (def.Top <= 0 || def.Top >= def.TopInner ||
            def.TopInner >= def.BottomInner ||
            def.BottomInner >= def.Bottom ||
            def.Bottom >= sourceHeight)
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Extracts all 16 border slices from the source bitmap.
    /// </summary>
    private static FrameSlices? ExtractAllSlices(Bitmap source, FrameSliceDefinition def, int sourceWidth, int sourceHeight)
    {
        // Calculate column widths
        var col0Width = def.Left;                           // Left corner
        var col1Width = def.LeftInner - def.Left;           // Left stretch
        var col2Width = def.RightInner - def.LeftInner;     // Center
        var col3Width = def.Right - def.RightInner;         // Right stretch
        var col4Width = sourceWidth - def.Right;            // Right corner
        
        // Calculate row heights
        var row0Height = def.Top;                           // Top corner
        var row1Height = def.TopInner - def.Top;            // Top stretch
        var row2Height = def.BottomInner - def.TopInner;    // Center
        var row3Height = def.Bottom - def.BottomInner;      // Bottom stretch
        var row4Height = sourceHeight - def.Bottom;         // Bottom corner
        
        // Row 0: Top edge (5 cells)
        var topLeft = ExtractRegion(source, 0, 0, col0Width, row0Height);
        var topLeftStretch = ExtractRegion(source, def.Left, 0, col1Width, row0Height);
        var topCenter = ExtractRegion(source, def.LeftInner, 0, col2Width, row0Height);
        var topRightStretch = ExtractRegion(source, def.RightInner, 0, col3Width, row0Height);
        var topRight = ExtractRegion(source, def.Right, 0, col4Width, row0Height);
        
        // Column 0: Left edge (rows 1-3)
        var leftTopStretch = ExtractRegion(source, 0, def.Top, col0Width, row1Height);
        var leftCenter = ExtractRegion(source, 0, def.TopInner, col0Width, row2Height);
        var leftBottomStretch = ExtractRegion(source, 0, def.BottomInner, col0Width, row3Height);
        
        // Column 4: Right edge (rows 1-3)
        var rightTopStretch = ExtractRegion(source, def.Right, def.Top, col4Width, row1Height);
        var rightCenter = ExtractRegion(source, def.Right, def.TopInner, col4Width, row2Height);
        var rightBottomStretch = ExtractRegion(source, def.Right, def.BottomInner, col4Width, row3Height);
        
        // Row 4: Bottom edge (5 cells)
        var bottomLeft = ExtractRegion(source, 0, def.Bottom, col0Width, row4Height);
        var bottomLeftStretch = ExtractRegion(source, def.Left, def.Bottom, col1Width, row4Height);
        var bottomCenter = ExtractRegion(source, def.LeftInner, def.Bottom, col2Width, row4Height);
        var bottomRightStretch = ExtractRegion(source, def.RightInner, def.Bottom, col3Width, row4Height);
        var bottomRight = ExtractRegion(source, def.Right, def.Bottom, col4Width, row4Height);
        
        // Validate all slices were extracted
        if (topLeft == null || topLeftStretch == null || topCenter == null || topRightStretch == null || topRight == null ||
            leftTopStretch == null || leftCenter == null || leftBottomStretch == null ||
            rightTopStretch == null || rightCenter == null || rightBottomStretch == null ||
            bottomLeft == null || bottomLeftStretch == null || bottomCenter == null || bottomRightStretch == null || bottomRight == null)
        {
            return null;
        }
        
        return new FrameSlices
        {
            TopLeft = topLeft,
            TopLeftStretch = topLeftStretch,
            TopCenter = topCenter,
            TopRightStretch = topRightStretch,
            TopRight = topRight,
            LeftTopStretch = leftTopStretch,
            LeftCenter = leftCenter,
            LeftBottomStretch = leftBottomStretch,
            RightTopStretch = rightTopStretch,
            RightCenter = rightCenter,
            RightBottomStretch = rightBottomStretch,
            BottomLeft = bottomLeft,
            BottomLeftStretch = bottomLeftStretch,
            BottomCenter = bottomCenter,
            BottomRightStretch = bottomRightStretch,
            BottomRight = bottomRight,
            SourceSize = default,
            SliceDefinition = def
        };
    }
    
    /// <summary>
    /// Loads a bitmap from a URI (supports avares:// and file paths).
    /// </summary>
    private static Bitmap? LoadBitmap(string uri)
    {
        try
        {
            if (uri.StartsWith("avares://", StringComparison.OrdinalIgnoreCase))
            {
                // Load from embedded resource using Avalonia 11+ API
                var assetUri = new Uri(uri);
                using var stream = Avalonia.Platform.AssetLoader.Open(assetUri);
                return new Bitmap(stream);
            }
            else
            {
                // Load from file path
                return new Bitmap(uri);
            }
        }
        catch (Exception)
        {
            return null;
        }
    }
    
    /// <summary>
    /// Extracts a rectangular region from a source bitmap.
    /// </summary>
    private static Bitmap? ExtractRegion(Bitmap source, int x, int y, int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return null;
        }
        
        try
        {
            // Use RenderTargetBitmap to extract region
            var pixelSize = new PixelSize(width, height);
            var renderTarget = new RenderTargetBitmap(pixelSize);
            
            using (var context = renderTarget.CreateDrawingContext())
            {
                // Draw the source bitmap offset so the desired region is at (0,0)
                var sourceRect = new Rect(x, y, width, height);
                var destRect = new Rect(0, 0, width, height);
                
                context.DrawImage(source, sourceRect, destRect);
            }
            
            return renderTarget;
        }
        catch (Exception)
        {
            return null;
        }
    }
    
    /// <inheritdoc/>
    public FrameSlices? LoadResizeAndSlice(string sourceUri, FrameSliceDefinition sliceDefinition, int targetWidth, int targetHeight)
    {
        ArgumentNullException.ThrowIfNull(sourceUri);
        ArgumentNullException.ThrowIfNull(sliceDefinition);
        
        if (targetWidth <= 0 || targetHeight <= 0)
        {
            return null;
        }
        
        var cacheKey = (targetWidth, targetHeight);
        
        // Check cache first
        if (_cache.TryGetValue(cacheKey, out var cachedSlices))
        {
            // Move to end of LRU order (most recently used)
            _cacheOrder.Remove(cacheKey);
            _cacheOrder.AddLast(cacheKey);
            return cachedSlices;
        }
        
        try
        {
            // Load source bitmap (use cached if same URI)
            Bitmap? sourceBitmap;
            if (_cachedSourceUri == sourceUri && _cachedSourceBitmap != null)
            {
                sourceBitmap = _cachedSourceBitmap;
            }
            else
            {
                sourceBitmap = LoadBitmap(sourceUri);
                if (sourceBitmap == null)
                {
                    return null;
                }
                _cachedSourceBitmap = sourceBitmap;
                _cachedSourceUri = sourceUri;
                _cachedSliceDefinition = sliceDefinition;
            }
            
            var sourceWidth = (int)sourceBitmap.Size.Width;
            var sourceHeight = (int)sourceBitmap.Size.Height;
            
            // Resize the source bitmap to target dimensions
            var resizedBitmap = ResizeBitmap(sourceBitmap, targetWidth, targetHeight);
            if (resizedBitmap == null)
            {
                return null;
            }
            
            // Scale slice coordinates proportionally
            var scaleX = (double)targetWidth / sourceWidth;
            var scaleY = (double)targetHeight / sourceHeight;
            
            var scaledSliceDefinition = new FrameSliceDefinition
            {
                Left = (int)(sliceDefinition.Left * scaleX),
                LeftInner = (int)(sliceDefinition.LeftInner * scaleX),
                RightInner = (int)(sliceDefinition.RightInner * scaleX),
                Right = (int)(sliceDefinition.Right * scaleX),
                Top = (int)(sliceDefinition.Top * scaleY),
                TopInner = (int)(sliceDefinition.TopInner * scaleY),
                BottomInner = (int)(sliceDefinition.BottomInner * scaleY),
                Bottom = (int)(sliceDefinition.Bottom * scaleY)
            };
            
            // Validate scaled slice definition
            if (!ValidateSliceDefinition(scaledSliceDefinition, targetWidth, targetHeight))
            {
                return null;
            }
            
            // Extract all 16 border slices
            var slices = ExtractAllSlices(resizedBitmap, scaledSliceDefinition, targetWidth, targetHeight);
            if (slices == null)
            {
                return null;
            }
            
            slices = slices with { SourceSize = new Size(targetWidth, targetHeight) };
            
#if DEBUG
            // Save slices to temp folder for debugging
            SaveSlicesToTemp(resizedBitmap, slices, targetWidth, targetHeight);
#endif
            
            // Add to cache
            AddToCache(cacheKey, slices);
            
            return slices;
        }
        catch (Exception)
        {
            return null;
        }
    }
    
    /// <inheritdoc/>
    public void ClearCache()
    {
        _cache.Clear();
        _cacheOrder.Clear();
        _cachedSourceBitmap = null;
        _cachedSourceUri = null;
        _cachedSliceDefinition = null;
    }
    
    /// <summary>
    /// Adds slices to the LRU cache, evicting oldest if at capacity.
    /// </summary>
    private void AddToCache((int width, int height) key, FrameSlices slices)
    {
        // Evict oldest if at capacity
        while (_cache.Count >= MaxCacheSize && _cacheOrder.First != null)
        {
            var oldest = _cacheOrder.First.Value;
            _cacheOrder.RemoveFirst();
            _cache.Remove(oldest);
        }
        
        _cache[key] = slices;
        _cacheOrder.AddLast(key);
    }
    
    /// <summary>
    /// Resizes a bitmap to the specified dimensions.
    /// </summary>
    private static Bitmap? ResizeBitmap(Bitmap source, int targetWidth, int targetHeight)
    {
        try
        {
            var pixelSize = new PixelSize(targetWidth, targetHeight);
            var renderTarget = new RenderTargetBitmap(pixelSize);
            
            using (var context = renderTarget.CreateDrawingContext())
            {
                var sourceRect = new Rect(0, 0, source.Size.Width, source.Size.Height);
                var destRect = new Rect(0, 0, targetWidth, targetHeight);
                
                context.DrawImage(source, sourceRect, destRect);
            }
            
            return renderTarget;
        }
        catch (Exception)
        {
            return null;
        }
    }
    
#if DEBUG
    /// <summary>
    /// Saves the resized image and all slices to a temp folder for debugging.
    /// </summary>
    private static void SaveSlicesToTemp(Bitmap resizedBitmap, FrameSlices slices, int targetWidth, int targetHeight)
    {
        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "WatchTower", "FrameSlices", $"{targetWidth}x{targetHeight}");
            Directory.CreateDirectory(tempDir);
            
            Debug.WriteLine($"FrameSliceService: Saving debug slices to: {tempDir}");
            
            // Save resized full frame
            SaveBitmapToPng(resizedBitmap, Path.Combine(tempDir, "resized-full.png"));
            
            // Save each slice (16 total for 5x5 grid)
            // Row 0
            SaveBitmapToPng(slices.TopLeft, Path.Combine(tempDir, "row0-col0-top-left.png"));
            SaveBitmapToPng(slices.TopLeftStretch, Path.Combine(tempDir, "row0-col1-top-left-stretch.png"));
            SaveBitmapToPng(slices.TopCenter, Path.Combine(tempDir, "row0-col2-top-center.png"));
            SaveBitmapToPng(slices.TopRightStretch, Path.Combine(tempDir, "row0-col3-top-right-stretch.png"));
            SaveBitmapToPng(slices.TopRight, Path.Combine(tempDir, "row0-col4-top-right.png"));
            
            // Left edge (rows 1-3)
            SaveBitmapToPng(slices.LeftTopStretch, Path.Combine(tempDir, "row1-col0-left-top-stretch.png"));
            SaveBitmapToPng(slices.LeftCenter, Path.Combine(tempDir, "row2-col0-left-center.png"));
            SaveBitmapToPng(slices.LeftBottomStretch, Path.Combine(tempDir, "row3-col0-left-bottom-stretch.png"));
            
            // Right edge (rows 1-3)
            SaveBitmapToPng(slices.RightTopStretch, Path.Combine(tempDir, "row1-col4-right-top-stretch.png"));
            SaveBitmapToPng(slices.RightCenter, Path.Combine(tempDir, "row2-col4-right-center.png"));
            SaveBitmapToPng(slices.RightBottomStretch, Path.Combine(tempDir, "row3-col4-right-bottom-stretch.png"));
            
            // Row 4
            SaveBitmapToPng(slices.BottomLeft, Path.Combine(tempDir, "row4-col0-bottom-left.png"));
            SaveBitmapToPng(slices.BottomLeftStretch, Path.Combine(tempDir, "row4-col1-bottom-left-stretch.png"));
            SaveBitmapToPng(slices.BottomCenter, Path.Combine(tempDir, "row4-col2-bottom-center.png"));
            SaveBitmapToPng(slices.BottomRightStretch, Path.Combine(tempDir, "row4-col3-bottom-right-stretch.png"));
            SaveBitmapToPng(slices.BottomRight, Path.Combine(tempDir, "row4-col4-bottom-right.png"));
            
            // Write slice info
            var def = slices.SliceDefinition;
            var infoPath = Path.Combine(tempDir, "slice-info.txt");
            var info = $"""
                Resolution: {targetWidth}x{targetHeight}
                Slice Definition (5x5):
                  X: L={def.Left}, LI={def.LeftInner}, RI={def.RightInner}, R={def.Right}
                  Y: T={def.Top}, TI={def.TopInner}, BI={def.BottomInner}, B={def.Bottom}
                
                Slice Sizes (16 border cells):
                  Row 0: TopLeft={slices.TopLeft.Size}, TopLeftS={slices.TopLeftStretch.Size}, TopCenter={slices.TopCenter.Size}, TopRightS={slices.TopRightStretch.Size}, TopRight={slices.TopRight.Size}
                  Col 0: LeftTopS={slices.LeftTopStretch.Size}, LeftCenter={slices.LeftCenter.Size}, LeftBottomS={slices.LeftBottomStretch.Size}
                  Col 4: RightTopS={slices.RightTopStretch.Size}, RightCenter={slices.RightCenter.Size}, RightBottomS={slices.RightBottomStretch.Size}
                  Row 4: BottomLeft={slices.BottomLeft.Size}, BottomLeftS={slices.BottomLeftStretch.Size}, BottomCenter={slices.BottomCenter.Size}, BottomRightS={slices.BottomRightStretch.Size}, BottomRight={slices.BottomRight.Size}
                
                Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
                """;
            File.WriteAllText(infoPath, info);
            
            Debug.WriteLine($"FrameSliceService: Debug slices saved successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"FrameSliceService: Failed to save debug slices: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Saves a bitmap to a PNG file.
    /// </summary>
    private static void SaveBitmapToPng(Bitmap bitmap, string filePath)
    {
        try
        {
            using var stream = File.Create(filePath);
            bitmap.Save(stream);
            Debug.WriteLine($"  Saved: {Path.GetFileName(filePath)} ({bitmap.Size.Width}x{bitmap.Size.Height})");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"  Failed to save {Path.GetFileName(filePath)}: {ex.Message}");
        }
    }
#endif
}
