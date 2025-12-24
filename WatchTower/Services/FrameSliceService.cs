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
                System.Diagnostics.Debug.WriteLine($"FrameSliceService: Failed to load source image from {sourceUri}");
                return null;
            }
            
            var sourceWidth = (int)sourceBitmap.Size.Width;
            var sourceHeight = (int)sourceBitmap.Size.Height;
            
            System.Diagnostics.Debug.WriteLine($"FrameSliceService: Loaded source {sourceWidth}x{sourceHeight}, slicing at L={sliceDefinition.Left}, T={sliceDefinition.Top}, R={sliceDefinition.Right}, B={sliceDefinition.Bottom}");
            
            // Validate slice definition (absolute coordinates)
            if (sliceDefinition.Left <= 0 || sliceDefinition.Left >= sliceDefinition.Right ||
                sliceDefinition.Right >= sourceWidth ||
                sliceDefinition.Top <= 0 || sliceDefinition.Top >= sliceDefinition.Bottom ||
                sliceDefinition.Bottom >= sourceHeight)
            {
                System.Diagnostics.Debug.WriteLine($"FrameSliceService: Invalid slice definition - coordinates out of bounds or in wrong order");
                return null;
            }
            
            // Calculate dimensions from absolute coordinates
            var leftWidth = sliceDefinition.Left;
            var topHeight = sliceDefinition.Top;
            var rightWidth = sourceWidth - sliceDefinition.Right;
            var bottomHeight = sourceHeight - sliceDefinition.Bottom;
            var centerWidth = sliceDefinition.Right - sliceDefinition.Left;
            var centerHeight = sliceDefinition.Bottom - sliceDefinition.Top;
            
            System.Diagnostics.Debug.WriteLine($"FrameSliceService: Calculated regions - leftW={leftWidth}, topH={topHeight}, rightW={rightWidth}, bottomH={bottomHeight}, centerW={centerWidth}, centerH={centerHeight}");
            
            // Extract all 9 slices using absolute coordinates
            // Top row
            var topLeft = ExtractRegion(sourceBitmap, 0, 0, leftWidth, topHeight);
            var topCenter = ExtractRegion(sourceBitmap, sliceDefinition.Left, 0, centerWidth, topHeight);
            var topRight = ExtractRegion(sourceBitmap, sliceDefinition.Right, 0, rightWidth, topHeight);
            
            // Middle row
            var middleLeft = ExtractRegion(sourceBitmap, 0, sliceDefinition.Top, leftWidth, centerHeight);
            var middleRight = ExtractRegion(sourceBitmap, sliceDefinition.Right, sliceDefinition.Top, rightWidth, centerHeight);
            
            // Bottom row
            var bottomLeft = ExtractRegion(sourceBitmap, 0, sliceDefinition.Bottom, leftWidth, bottomHeight);
            var bottomCenter = ExtractRegion(sourceBitmap, sliceDefinition.Left, sliceDefinition.Bottom, centerWidth, bottomHeight);
            var bottomRight = ExtractRegion(sourceBitmap, sliceDefinition.Right, sliceDefinition.Bottom, rightWidth, bottomHeight);
            
            if (topLeft == null || topCenter == null || topRight == null ||
                middleLeft == null || middleRight == null ||
                bottomLeft == null || bottomCenter == null || bottomRight == null)
            {
                System.Diagnostics.Debug.WriteLine("FrameSliceService: Failed to extract one or more regions");
                return null;
            }
            
            System.Diagnostics.Debug.WriteLine($"FrameSliceService: Successfully sliced - TopLeft={topLeft.Size}, TopCenter={topCenter.Size}, TopRight={topRight.Size}");
            
            return new FrameSlices
            {
                TopLeft = topLeft,
                TopCenter = topCenter,
                TopRight = topRight,
                MiddleLeft = middleLeft,
                MiddleRight = middleRight,
                BottomLeft = bottomLeft,
                BottomCenter = bottomCenter,
                BottomRight = bottomRight,
                SourceSize = sourceBitmap.Size,
                SliceDefinition = sliceDefinition
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FrameSliceService: Error slicing image: {ex.Message}");
            return null;
        }
    }
    
    /// <inheritdoc/>
    public FrameSlices? LoadAndSliceByPercent(string sourceUri, double leftPercent, double topPercent, double rightPercent, double bottomPercent)
    {
        ArgumentNullException.ThrowIfNull(sourceUri);
        
        // Validate percentages
        if (leftPercent < 0 || leftPercent > 1 || topPercent < 0 || topPercent > 1 ||
            rightPercent < 0 || rightPercent > 1 || bottomPercent < 0 || bottomPercent > 1)
        {
            throw new ArgumentException("Percentages must be between 0.0 and 1.0");
        }
        
        if (leftPercent + rightPercent >= 1 || topPercent + bottomPercent >= 1)
        {
            throw new ArgumentException("Combined percentages for opposite edges must be less than 1.0");
        }
        
        try
        {
            // Load source to get dimensions
            var sourceBitmap = LoadBitmap(sourceUri);
            if (sourceBitmap == null)
            {
                return null;
            }
            
            var sourceWidth = (int)sourceBitmap.Size.Width;
            var sourceHeight = (int)sourceBitmap.Size.Height;
            
            // Calculate pixel coordinates from percentages
            var sliceDefinition = new FrameSliceDefinition
            {
                Left = (int)(sourceWidth * leftPercent),
                Top = (int)(sourceHeight * topPercent),
                Right = (int)(sourceWidth * rightPercent),
                Bottom = (int)(sourceHeight * bottomPercent)
            };
            
            // Dispose the source bitmap we loaded just for dimensions
            // LoadAndSlice will load it again - this is slightly inefficient but keeps the code clean
            // For optimization, we could pass the already-loaded bitmap
            
            return LoadAndSlice(sourceUri, sliceDefinition);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FrameSliceService: Error in LoadAndSliceByPercent: {ex.Message}");
            return null;
        }
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FrameSliceService: Failed to load bitmap from {uri}: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine($"FrameSliceService: Invalid region dimensions {width}x{height}");
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FrameSliceService: Failed to extract region at ({x},{y}) size {width}x{height}: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine($"FrameSliceService: Invalid target dimensions {targetWidth}x{targetHeight}");
            return null;
        }
        
        var cacheKey = (targetWidth, targetHeight);
        
        // Check cache first
        if (_cache.TryGetValue(cacheKey, out var cachedSlices))
        {
            // Move to end of LRU order (most recently used)
            _cacheOrder.Remove(cacheKey);
            _cacheOrder.AddLast(cacheKey);
            System.Diagnostics.Debug.WriteLine($"FrameSliceService: Cache hit for {targetWidth}x{targetHeight}");
            return cachedSlices;
        }
        
        try
        {
            // Load source bitmap (use cached if same URI)
            Bitmap? sourceBitmap;
            if (_cachedSourceUri == sourceUri && _cachedSourceBitmap != null)
            {
                sourceBitmap = _cachedSourceBitmap;
                System.Diagnostics.Debug.WriteLine($"FrameSliceService: Using cached source bitmap");
            }
            else
            {
                sourceBitmap = LoadBitmap(sourceUri);
                if (sourceBitmap == null)
                {
                    System.Diagnostics.Debug.WriteLine($"FrameSliceService: Failed to load source image from {sourceUri}");
                    return null;
                }
                _cachedSourceBitmap = sourceBitmap;
                _cachedSourceUri = sourceUri;
                _cachedSliceDefinition = sliceDefinition;
            }
            
            var sourceWidth = (int)sourceBitmap.Size.Width;
            var sourceHeight = (int)sourceBitmap.Size.Height;
            
            System.Diagnostics.Debug.WriteLine($"FrameSliceService: Source {sourceWidth}x{sourceHeight} -> Target {targetWidth}x{targetHeight}");
            
            // Resize the source bitmap to target dimensions
            var resizedBitmap = ResizeBitmap(sourceBitmap, targetWidth, targetHeight);
            if (resizedBitmap == null)
            {
                System.Diagnostics.Debug.WriteLine($"FrameSliceService: Failed to resize bitmap");
                return null;
            }
            
            // Scale slice coordinates proportionally
            var scaleX = (double)targetWidth / sourceWidth;
            var scaleY = (double)targetHeight / sourceHeight;
            
            var scaledSliceDefinition = new FrameSliceDefinition
            {
                Left = (int)(sliceDefinition.Left * scaleX),
                Top = (int)(sliceDefinition.Top * scaleY),
                Right = (int)(sliceDefinition.Right * scaleX),
                Bottom = (int)(sliceDefinition.Bottom * scaleY)
            };
            
            System.Diagnostics.Debug.WriteLine($"FrameSliceService: Scaled slices L={scaledSliceDefinition.Left}, T={scaledSliceDefinition.Top}, R={scaledSliceDefinition.Right}, B={scaledSliceDefinition.Bottom}");
            
            // Validate scaled slice definition
            if (scaledSliceDefinition.Left <= 0 || scaledSliceDefinition.Left >= scaledSliceDefinition.Right ||
                scaledSliceDefinition.Right >= targetWidth ||
                scaledSliceDefinition.Top <= 0 || scaledSliceDefinition.Top >= scaledSliceDefinition.Bottom ||
                scaledSliceDefinition.Bottom >= targetHeight)
            {
                System.Diagnostics.Debug.WriteLine($"FrameSliceService: Invalid scaled slice definition");
                return null;
            }
            
            // Calculate dimensions from scaled coordinates
            var leftWidth = scaledSliceDefinition.Left;
            var topHeight = scaledSliceDefinition.Top;
            var rightWidth = targetWidth - scaledSliceDefinition.Right;
            var bottomHeight = targetHeight - scaledSliceDefinition.Bottom;
            var centerWidth = scaledSliceDefinition.Right - scaledSliceDefinition.Left;
            var centerHeight = scaledSliceDefinition.Bottom - scaledSliceDefinition.Top;
            
            // Extract all 8 outer slices (skip center)
            var topLeft = ExtractRegion(resizedBitmap, 0, 0, leftWidth, topHeight);
            var topCenter = ExtractRegion(resizedBitmap, scaledSliceDefinition.Left, 0, centerWidth, topHeight);
            var topRight = ExtractRegion(resizedBitmap, scaledSliceDefinition.Right, 0, rightWidth, topHeight);
            
            var middleLeft = ExtractRegion(resizedBitmap, 0, scaledSliceDefinition.Top, leftWidth, centerHeight);
            var middleRight = ExtractRegion(resizedBitmap, scaledSliceDefinition.Right, scaledSliceDefinition.Top, rightWidth, centerHeight);
            
            var bottomLeft = ExtractRegion(resizedBitmap, 0, scaledSliceDefinition.Bottom, leftWidth, bottomHeight);
            var bottomCenter = ExtractRegion(resizedBitmap, scaledSliceDefinition.Left, scaledSliceDefinition.Bottom, centerWidth, bottomHeight);
            var bottomRight = ExtractRegion(resizedBitmap, scaledSliceDefinition.Right, scaledSliceDefinition.Bottom, rightWidth, bottomHeight);
            
            if (topLeft == null || topCenter == null || topRight == null ||
                middleLeft == null || middleRight == null ||
                bottomLeft == null || bottomCenter == null || bottomRight == null)
            {
                System.Diagnostics.Debug.WriteLine("FrameSliceService: Failed to extract one or more regions from resized image");
                return null;
            }
            
            var slices = new FrameSlices
            {
                TopLeft = topLeft,
                TopCenter = topCenter,
                TopRight = topRight,
                MiddleLeft = middleLeft,
                MiddleRight = middleRight,
                BottomLeft = bottomLeft,
                BottomCenter = bottomCenter,
                BottomRight = bottomRight,
                SourceSize = new Size(targetWidth, targetHeight),
                SliceDefinition = scaledSliceDefinition
            };
            
#if DEBUG
            // Save slices to temp folder for debugging
            SaveSlicesToTemp(resizedBitmap, slices, targetWidth, targetHeight);
#endif
            
            // Add to cache
            AddToCache(cacheKey, slices);
            
            System.Diagnostics.Debug.WriteLine($"FrameSliceService: Successfully sliced resized image - TopLeft={topLeft.Size}");
            return slices;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FrameSliceService: Error in LoadResizeAndSlice: {ex.Message}");
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
        System.Diagnostics.Debug.WriteLine("FrameSliceService: Cache cleared");
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
            System.Diagnostics.Debug.WriteLine($"FrameSliceService: Evicted cache entry {oldest.width}x{oldest.height}");
        }
        
        _cache[key] = slices;
        _cacheOrder.AddLast(key);
        System.Diagnostics.Debug.WriteLine($"FrameSliceService: Cached slices for {key.width}x{key.height} (cache size: {_cache.Count})");
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FrameSliceService: Failed to resize bitmap to {targetWidth}x{targetHeight}: {ex.Message}");
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
            
            // Save each slice
            SaveBitmapToPng(slices.TopLeft, Path.Combine(tempDir, "top-left.png"));
            SaveBitmapToPng(slices.TopCenter, Path.Combine(tempDir, "top-center.png"));
            SaveBitmapToPng(slices.TopRight, Path.Combine(tempDir, "top-right.png"));
            SaveBitmapToPng(slices.MiddleLeft, Path.Combine(tempDir, "middle-left.png"));
            SaveBitmapToPng(slices.MiddleRight, Path.Combine(tempDir, "middle-right.png"));
            SaveBitmapToPng(slices.BottomLeft, Path.Combine(tempDir, "bottom-left.png"));
            SaveBitmapToPng(slices.BottomCenter, Path.Combine(tempDir, "bottom-center.png"));
            SaveBitmapToPng(slices.BottomRight, Path.Combine(tempDir, "bottom-right.png"));
            
            // Write slice info
            var infoPath = Path.Combine(tempDir, "slice-info.txt");
            var info = $"""
                Resolution: {targetWidth}x{targetHeight}
                Slice Definition: L={slices.SliceDefinition.Left}, T={slices.SliceDefinition.Top}, R={slices.SliceDefinition.Right}, B={slices.SliceDefinition.Bottom}
                
                Slice Sizes:
                  TopLeft: {slices.TopLeft.Size}
                  TopCenter: {slices.TopCenter.Size}
                  TopRight: {slices.TopRight.Size}
                  MiddleLeft: {slices.MiddleLeft.Size}
                  MiddleRight: {slices.MiddleRight.Size}
                  BottomLeft: {slices.BottomLeft.Size}
                  BottomCenter: {slices.BottomCenter.Size}
                  BottomRight: {slices.BottomRight.Size}
                
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
