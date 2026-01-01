using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using WatchTower.Models;
using WatchTower.Services;

namespace WatchTower.ViewModels;

/// <summary>
/// ViewModel for panel frame rendering using 5x5 grid slicing.
/// Handles frame bitmap sources, grid dimensions, and slide direction clipping.
/// </summary>
public class PanelFrameViewModel : ViewModelBase
{
    private readonly IFrameSliceService _frameSliceService;
    
    // Cached frame configuration
    private string? _frameSourceUri;
    private FrameSliceDefinition? _frameSliceDefinition;
    private Size _frameSourceSize;
    private double _renderScale = 1.0;
    private double _frameDisplayScale = 1.0;
    private PanelSlideDirection _slideDirection = PanelSlideDirection.Left;
    
    // Frame bitmap sources (16 pieces for 5x5 grid)
    // Row 0: Top edge
    private Bitmap? _topLeftSource;
    private Bitmap? _topLeftStretchSource;
    private Bitmap? _topCenterSource;
    private Bitmap? _topRightStretchSource;
    private Bitmap? _topRightSource;
    
    // Column 0: Left edge (rows 1-3)
    private Bitmap? _leftTopStretchSource;
    private Bitmap? _leftCenterSource;
    private Bitmap? _leftBottomStretchSource;
    
    // Column 4: Right edge (rows 1-3)
    private Bitmap? _rightTopStretchSource;
    private Bitmap? _rightCenterSource;
    private Bitmap? _rightBottomStretchSource;
    
    // Row 4: Bottom edge
    private Bitmap? _bottomLeftSource;
    private Bitmap? _bottomLeftStretchSource;
    private Bitmap? _bottomCenterSource;
    private Bitmap? _bottomRightStretchSource;
    private Bitmap? _bottomRightSource;
    
    // Grid row/column definitions
    private GridLength _row0Height = GridLength.Auto;
    private GridLength _row2Height = GridLength.Auto;
    private GridLength _row4Height = GridLength.Auto;
    private GridLength _col0Width = GridLength.Auto;
    private GridLength _col2Width = GridLength.Auto;
    private GridLength _col4Width = GridLength.Auto;

    public PanelFrameViewModel()
        : this(new FrameSliceService())
    {
    }
    
    public PanelFrameViewModel(IFrameSliceService frameSliceService)
    {
        _frameSliceService = frameSliceService ?? throw new ArgumentNullException(nameof(frameSliceService));
        
        // Initialize edge visibility based on default slide direction
        UpdateEdgeVisibility();
    }
    
    /// <summary>
    /// Gets or sets the current render scaling factor (DPI scale).
    /// </summary>
    public double RenderScale
    {
        get => _renderScale;
        set
        {
            if (SetProperty(ref _renderScale, value))
            {
                UpdateFrameDimensions();
            }
        }
    }
    
    /// <summary>
    /// Gets or sets the scaling factor for the frame elements relative to their source resolution.
    /// </summary>
    public double FrameDisplayScale
    {
        get => _frameDisplayScale;
        set
        {
            if (SetProperty(ref _frameDisplayScale, value))
            {
                UpdateFrameDimensions();
            }
        }
    }
    
    /// <summary>
    /// Gets or sets the slide direction which determines which edges are visible.
    /// </summary>
    public PanelSlideDirection SlideDirection
    {
        get => _slideDirection;
        set
        {
            if (SetProperty(ref _slideDirection, value))
            {
                UpdateEdgeVisibility();
            }
        }
    }
    
    // Edge visibility properties (based on slide direction)
    private bool _showTopEdge = true;
    private bool _showBottomEdge = true;
    private bool _showLeftEdge = true;
    private bool _showRightEdge = true;
    
    public bool ShowTopEdge
    {
        get => _showTopEdge;
        private set => SetProperty(ref _showTopEdge, value);
    }
    
    public bool ShowBottomEdge
    {
        get => _showBottomEdge;
        private set => SetProperty(ref _showBottomEdge, value);
    }
    
    public bool ShowLeftEdge
    {
        get => _showLeftEdge;
        private set => SetProperty(ref _showLeftEdge, value);
    }
    
    public bool ShowRightEdge
    {
        get => _showRightEdge;
        private set => SetProperty(ref _showRightEdge, value);
    }
    
    // Frame bitmap sources properties
    public Bitmap? TopLeftSource
    {
        get => _topLeftSource;
        private set => SetProperty(ref _topLeftSource, value);
    }
    
    public Bitmap? TopLeftStretchSource
    {
        get => _topLeftStretchSource;
        private set => SetProperty(ref _topLeftStretchSource, value);
    }
    
    public Bitmap? TopCenterSource
    {
        get => _topCenterSource;
        private set => SetProperty(ref _topCenterSource, value);
    }
    
    public Bitmap? TopRightStretchSource
    {
        get => _topRightStretchSource;
        private set => SetProperty(ref _topRightStretchSource, value);
    }
    
    public Bitmap? TopRightSource
    {
        get => _topRightSource;
        private set => SetProperty(ref _topRightSource, value);
    }
    
    public Bitmap? LeftTopStretchSource
    {
        get => _leftTopStretchSource;
        private set => SetProperty(ref _leftTopStretchSource, value);
    }
    
    public Bitmap? LeftCenterSource
    {
        get => _leftCenterSource;
        private set => SetProperty(ref _leftCenterSource, value);
    }
    
    public Bitmap? LeftBottomStretchSource
    {
        get => _leftBottomStretchSource;
        private set => SetProperty(ref _leftBottomStretchSource, value);
    }
    
    public Bitmap? RightTopStretchSource
    {
        get => _rightTopStretchSource;
        private set => SetProperty(ref _rightTopStretchSource, value);
    }
    
    public Bitmap? RightCenterSource
    {
        get => _rightCenterSource;
        private set => SetProperty(ref _rightCenterSource, value);
    }
    
    public Bitmap? RightBottomStretchSource
    {
        get => _rightBottomStretchSource;
        private set => SetProperty(ref _rightBottomStretchSource, value);
    }
    
    public Bitmap? BottomLeftSource
    {
        get => _bottomLeftSource;
        private set => SetProperty(ref _bottomLeftSource, value);
    }
    
    public Bitmap? BottomLeftStretchSource
    {
        get => _bottomLeftStretchSource;
        private set => SetProperty(ref _bottomLeftStretchSource, value);
    }
    
    public Bitmap? BottomCenterSource
    {
        get => _bottomCenterSource;
        private set => SetProperty(ref _bottomCenterSource, value);
    }
    
    public Bitmap? BottomRightStretchSource
    {
        get => _bottomRightStretchSource;
        private set => SetProperty(ref _bottomRightStretchSource, value);
    }
    
    public Bitmap? BottomRightSource
    {
        get => _bottomRightSource;
        private set => SetProperty(ref _bottomRightSource, value);
    }
    
    // Grid dimensions
    public GridLength Row0Height
    {
        get => _row0Height;
        private set => SetProperty(ref _row0Height, value);
    }
    
    public GridLength Row2Height
    {
        get => _row2Height;
        private set => SetProperty(ref _row2Height, value);
    }
    
    public GridLength Row4Height
    {
        get => _row4Height;
        private set => SetProperty(ref _row4Height, value);
    }
    
    public GridLength Col0Width
    {
        get => _col0Width;
        private set => SetProperty(ref _col0Width, value);
    }
    
    public GridLength Col2Width
    {
        get => _col2Width;
        private set => SetProperty(ref _col2Width, value);
    }
    
    public GridLength Col4Width
    {
        get => _col4Width;
        private set => SetProperty(ref _col4Width, value);
    }
    
    /// <summary>
    /// Loads and slices the frame image into 16 pieces for the 5x5 grid.
    /// </summary>
    public bool LoadFrameImage(string sourceUri, FrameSliceDefinition sliceDefinition)
    {
        _frameSourceUri = sourceUri;
        _frameSliceDefinition = sliceDefinition;
        
        var frameSlices = _frameSliceService.LoadAndSlice(sourceUri, sliceDefinition);
        
        if (frameSlices == null)
        {
            return false;
        }
        
        _frameSourceSize = frameSlices.SourceSize;
        
        // Update bitmap sources
        TopLeftSource = frameSlices.TopLeft;
        TopLeftStretchSource = frameSlices.TopLeftStretch;
        TopCenterSource = frameSlices.TopCenter;
        TopRightStretchSource = frameSlices.TopRightStretch;
        TopRightSource = frameSlices.TopRight;
        
        LeftTopStretchSource = frameSlices.LeftTopStretch;
        LeftCenterSource = frameSlices.LeftCenter;
        LeftBottomStretchSource = frameSlices.LeftBottomStretch;
        
        RightTopStretchSource = frameSlices.RightTopStretch;
        RightCenterSource = frameSlices.RightCenter;
        RightBottomStretchSource = frameSlices.RightBottomStretch;
        
        BottomLeftSource = frameSlices.BottomLeft;
        BottomLeftStretchSource = frameSlices.BottomLeftStretch;
        BottomCenterSource = frameSlices.BottomCenter;
        BottomRightStretchSource = frameSlices.BottomRightStretch;
        BottomRightSource = frameSlices.BottomRight;
        
        UpdateFrameDimensions();
        UpdateEdgeVisibility();
        
        return true;
    }
    
    /// <summary>
    /// Updates frame dimensions based on current RenderScale.
    /// </summary>
    private void UpdateFrameDimensions()
    {
        if (_frameSliceDefinition == null) return;
        
        var def = _frameSliceDefinition;
        var scale = RenderScale > 0 ? RenderScale : 1.0;
        var frameScale = FrameDisplayScale;
        
        // Row heights
        Row0Height = new GridLength((def.Top * frameScale) / scale, GridUnitType.Pixel);
        Row2Height = new GridLength(((def.BottomInner - def.TopInner) * frameScale) / scale, GridUnitType.Pixel);
        Row4Height = new GridLength(((_frameSourceSize.Height - def.Bottom) * frameScale) / scale, GridUnitType.Pixel);
        
        // Column widths
        Col0Width = new GridLength((def.Left * frameScale) / scale, GridUnitType.Pixel);
        Col2Width = new GridLength(((def.RightInner - def.LeftInner) * frameScale) / scale, GridUnitType.Pixel);
        Col4Width = new GridLength(((_frameSourceSize.Width - def.Right) * frameScale) / scale, GridUnitType.Pixel);
    }
    
    /// <summary>
    /// Updates edge visibility based on slide direction.
    /// </summary>
    private void UpdateEdgeVisibility()
    {
        switch (SlideDirection)
        {
            case PanelSlideDirection.Left:
                // Panel slides from left, hide left edge, show others
                ShowLeftEdge = false;
                ShowRightEdge = true;
                ShowTopEdge = true;
                ShowBottomEdge = true;
                break;
            
            case PanelSlideDirection.Bottom:
                // Panel slides from bottom, hide bottom edge, show others
                ShowLeftEdge = true;
                ShowRightEdge = true;
                ShowTopEdge = true;
                ShowBottomEdge = false;
                break;
            
            case PanelSlideDirection.Right:
                // Panel slides from right, hide right edge, show others
                ShowLeftEdge = true;
                ShowRightEdge = false;
                ShowTopEdge = true;
                ShowBottomEdge = true;
                break;
            
            case PanelSlideDirection.Top:
                // Panel slides from top, hide top edge, show others
                ShowLeftEdge = true;
                ShowRightEdge = true;
                ShowTopEdge = false;
                ShowBottomEdge = true;
                break;
        }
    }
}
