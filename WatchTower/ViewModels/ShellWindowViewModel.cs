using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using WatchTower.Services;

namespace WatchTower.ViewModels;

/// <summary>
/// ViewModel for the shell window that hosts both splash and main content.
/// Manages the transition between splash screen and main application.
/// </summary>
public class ShellWindowViewModel : ViewModelBase, IStartupLogger
{
    private object? _currentContent;
    private bool _isInSplashMode = true;
    private bool _isAnimating;
    private readonly SplashWindowViewModel _splashViewModel;
    private readonly IFrameSliceService _frameSliceService;
    private MainWindowViewModel? _mainViewModel;
    private bool _cleanedUp;
    
    // Cached frame configuration for re-slicing on monitor switch
    private string? _frameSourceUri;
    private FrameSliceDefinition? _frameSliceDefinition;
    private Size _frameSourceSize;
    private double _renderScale = 1.0;
    private double _frameDisplayScale = 1.0;
    private Thickness _contentPadding;
    private string _backgroundColor = "#1A1A1A";
    
    // Windowed mode configuration
    private bool _isWindowedModeEnabled;
    private double _initialWindowWidth = 1280;
    private double _initialWindowHeight = 720;
    private bool _windowedModeConfigurationLoaded;
    
    // Windowed mode validation constants
    private const double MinWindowWidth = 400;
    private const double MaxWindowWidth = 4000;
    private const double MinWindowHeight = 300;
    private const double MaxWindowHeight = 4000;

    // Frame bitmap sources (dynamically sliced from source image) - 16 pieces for 5x5 grid
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

    public ShellWindowViewModel(SplashWindowViewModel splashViewModel)
        : this(splashViewModel, new FrameSliceService())
    {
    }
    
    public ShellWindowViewModel(SplashWindowViewModel splashViewModel, IFrameSliceService frameSliceService)
    {
        _splashViewModel = splashViewModel ?? throw new ArgumentNullException(nameof(splashViewModel));
        _frameSliceService = frameSliceService ?? throw new ArgumentNullException(nameof(frameSliceService));
        _currentContent = _splashViewModel;
        
        // Forward exit request from splash
        _splashViewModel.ExitRequested += OnSplashExitRequested;
    }

    /// <summary>
    /// Gets or sets the current content to display (SplashViewModel or MainViewModel).
    /// </summary>
    public object? CurrentContent
    {
        get => _currentContent;
        set => SetProperty(ref _currentContent, value);
    }

    /// <summary>
    /// Gets whether the window is currently in splash mode.
    /// </summary>
    public bool IsInSplashMode
    {
        get => _isInSplashMode;
        set => SetProperty(ref _isInSplashMode, value);
    }

    /// <summary>
    /// Gets whether the window is currently animating.
    /// </summary>
    public bool IsAnimating
    {
        get => _isAnimating;
        set => SetProperty(ref _isAnimating, value);
    }

    /// <summary>
    /// Gets the splash view model for direct access.
    /// </summary>
    public SplashWindowViewModel SplashViewModel => _splashViewModel;

    /// <summary>
    /// Gets whether windowed mode is enabled (non-fullscreen with configurable size).
    /// </summary>
    public bool IsWindowedModeEnabled
    {
        get => _isWindowedModeEnabled;
        set => SetProperty(ref _isWindowedModeEnabled, value);
    }
    
    /// <summary>
    /// Gets the initial window width for windowed mode (in logical pixels).
    /// </summary>
    public double InitialWindowWidth
    {
        get => _initialWindowWidth;
        set => SetProperty(ref _initialWindowWidth, value);
    }
    
    /// <summary>
    /// Gets the initial window height for windowed mode (in logical pixels).
    /// </summary>
    public double InitialWindowHeight
    {
        get => _initialWindowHeight;
        set => SetProperty(ref _initialWindowHeight, value);
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
    /// Default is 1.0 (original size).
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
    /// Gets or sets the padding applied to the content container underneath the frame.
    /// </summary>
    public Thickness ContentPadding
    {
        get => _contentPadding;
        set => SetProperty(ref _contentPadding, value);
    }
    
    /// <summary>
    /// Gets or sets the background color for the padding area.
    /// </summary>
    public string BackgroundColor
    {
        get => _backgroundColor;
        set => SetProperty(ref _backgroundColor, value);
    }
    
    /// <summary>
    /// Gets the frame slice definition used for the current frame.
    /// </summary>
    public FrameSliceDefinition? FrameSliceDefinition => _frameSliceDefinition;
    
    /// <summary>
    /// Gets the source size of the loaded frame image.
    /// </summary>
    public Size FrameSourceSize => _frameSourceSize;
    
    // Frame bitmap sources - dynamically sliced from source image (16 pieces for 5x5 grid)
    
    // Row 0: Top edge (5 pieces)
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
    
    // Column 0: Left edge (rows 1-3, 3 pieces)
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
    
    // Column 4: Right edge (rows 1-3, 3 pieces)
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
    
    // Row 4: Bottom edge (5 pieces)
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
    
    // Grid row/column definitions for the 5x5 frame layout
    // Rows: 0=Top corner, 1=Top stretch, 2=Center, 3=Bottom stretch, 4=Bottom corner
    // Cols: 0=Left corner, 1=Left stretch, 2=Center, 3=Right stretch, 4=Right corner
    private GridLength _row0Height = GridLength.Auto;  // Top corner row (fixed)
    private GridLength _row2Height = GridLength.Auto;  // Center row (fixed)
    private GridLength _row4Height = GridLength.Auto;  // Bottom corner row (fixed)
    private GridLength _col0Width = GridLength.Auto;   // Left corner column (fixed)
    private GridLength _col2Width = GridLength.Auto;   // Center column (fixed)
    private GridLength _col4Width = GridLength.Auto;   // Right corner column (fixed)
    
    /// <summary>
    /// Height of row 0 (top corners and top edge).
    /// </summary>
    public GridLength Row0Height
    {
        get => _row0Height;
        private set => SetProperty(ref _row0Height, value);
    }
    
    /// <summary>
    /// Height of row 2 (center edge pieces - left center, right center).
    /// </summary>
    public GridLength Row2Height
    {
        get => _row2Height;
        private set => SetProperty(ref _row2Height, value);
    }
    
    /// <summary>
    /// Height of row 4 (bottom corners and bottom edge).
    /// </summary>
    public GridLength Row4Height
    {
        get => _row4Height;
        private set => SetProperty(ref _row4Height, value);
    }
    
    /// <summary>
    /// Width of column 0 (left corners and left edge).
    /// </summary>
    public GridLength Col0Width
    {
        get => _col0Width;
        private set => SetProperty(ref _col0Width, value);
    }
    
    /// <summary>
    /// Width of column 2 (center edge pieces - top center, bottom center).
    /// </summary>
    public GridLength Col2Width
    {
        get => _col2Width;
        private set => SetProperty(ref _col2Width, value);
    }
    
    /// <summary>
    /// Width of column 4 (right corners and right edge).
    /// </summary>
    public GridLength Col4Width
    {
        get => _col4Width;
        private set => SetProperty(ref _col4Width, value);
    }
    
    /// <summary>
    /// Loads and slices the frame image into 16 pieces for the 5x5 grid.
    /// Uses cached slices if available.
    /// </summary>
    /// <param name="sourceUri">URI to the source frame image (e.g., avares://WatchTower/Assets/main-frame.png).</param>
    /// <param name="sliceDefinition">The slice coordinates for 5x5 extraction (relative to original source).</param>
    /// <returns>True if loading succeeded, false otherwise.</returns>
    public bool LoadFrameImageForScreen(string sourceUri, FrameSliceDefinition sliceDefinition)
    {
        // Cache configuration
        _frameSourceUri = sourceUri;
        _frameSliceDefinition = sliceDefinition;
        
        // Load high-res source slices once (no resizing)
        var frameSlices = _frameSliceService.LoadAndSlice(sourceUri, sliceDefinition);
        
        if (frameSlices == null)
        {
            return false;
        }
        
        _frameSourceSize = frameSlices.SourceSize;

        // Update bitmap sources - this will trigger property changed notifications (16 pieces)
        // Row 0
        TopLeftSource = frameSlices.TopLeft;
        TopLeftStretchSource = frameSlices.TopLeftStretch;
        TopCenterSource = frameSlices.TopCenter;
        TopRightStretchSource = frameSlices.TopRightStretch;
        TopRightSource = frameSlices.TopRight;
        
        // Column 0 (rows 1-3)
        LeftTopStretchSource = frameSlices.LeftTopStretch;
        LeftCenterSource = frameSlices.LeftCenter;
        LeftBottomStretchSource = frameSlices.LeftBottomStretch;
        
        // Column 4 (rows 1-3)
        RightTopStretchSource = frameSlices.RightTopStretch;
        RightCenterSource = frameSlices.RightCenter;
        RightBottomStretchSource = frameSlices.RightBottomStretch;
        
        // Row 4
        BottomLeftSource = frameSlices.BottomLeft;
        BottomLeftStretchSource = frameSlices.BottomLeftStretch;
        BottomCenterSource = frameSlices.BottomCenter;
        BottomRightStretchSource = frameSlices.BottomRightStretch;
        BottomRightSource = frameSlices.BottomRight;
        
        UpdateFrameDimensions();
        
        return true;
    }
    
    /// <summary>
    /// Loads windowed mode configuration from IConfiguration.
    /// Should be called during initialization.
    /// Only loads configuration once to avoid overwriting values.
    /// </summary>
    /// <param name="configuration">The configuration instance to load from.</param>
    public void LoadWindowedModeConfiguration(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        if (configuration == null || _windowedModeConfigurationLoaded)
        {
            return;
        }
        
        // Parse configuration values with defaults and safe parsing
        var enabledStr = configuration["Frame:EnableWindowedMode"];
        if (!string.IsNullOrEmpty(enabledStr) && bool.TryParse(enabledStr, out var enabled))
        {
            IsWindowedModeEnabled = enabled;
        }
        
        var widthStr = configuration["Frame:InitialWidth"];
        if (!string.IsNullOrEmpty(widthStr) && double.TryParse(widthStr, out var width))
        {
            InitialWindowWidth = width;
        }
        
        var heightStr = configuration["Frame:InitialHeight"];
        if (!string.IsNullOrEmpty(heightStr) && double.TryParse(heightStr, out var height))
        {
            InitialWindowHeight = height;
        }
        
        // Validate initial dimensions
        if (InitialWindowWidth < MinWindowWidth || InitialWindowWidth > MaxWindowWidth)
        {
            throw new InvalidOperationException(
                $"Frame:InitialWidth must be between {MinWindowWidth} and {MaxWindowWidth}. Current value: {InitialWindowWidth}");
        }
        
        if (InitialWindowHeight < MinWindowHeight || InitialWindowHeight > MaxWindowHeight)
        {
            throw new InvalidOperationException(
                $"Frame:InitialHeight must be between {MinWindowHeight} and {MaxWindowHeight}. Current value: {InitialWindowHeight}");
        }
        
        // Mark configuration as loaded to prevent duplicate loading
        _windowedModeConfigurationLoaded = true;
    }
    
    /// <summary>
    /// Updates frame dimensions based on current RenderScale for the 5x5 grid.
    /// Fixed rows/columns: 0, 2, 4 (corners and centers)
    /// Stretch rows/columns: 1, 3 (handled by Star sizing in XAML)
    /// </summary>
    private void UpdateFrameDimensions()
    {
        if (_frameSliceDefinition == null) return;

        var def = _frameSliceDefinition;
        var scale = RenderScale > 0 ? RenderScale : 1.0;
        var frameScale = FrameDisplayScale;

        // Calculate logical dimensions based on physical slice definition and render scale
        // Physical * FrameScale / RenderScale = Logical
        
        // Row heights (fixed rows: 0, 2, 4)
        Row0Height = new GridLength((def.Top * frameScale) / scale, GridUnitType.Pixel);
        Row2Height = new GridLength(((def.BottomInner - def.TopInner) * frameScale) / scale, GridUnitType.Pixel);
        Row4Height = new GridLength(((_frameSourceSize.Height - def.Bottom) * frameScale) / scale, GridUnitType.Pixel);
        
        // Column widths (fixed columns: 0, 2, 4)
        Col0Width = new GridLength((def.Left * frameScale) / scale, GridUnitType.Pixel);
        Col2Width = new GridLength(((def.RightInner - def.LeftInner) * frameScale) / scale, GridUnitType.Pixel);
        Col4Width = new GridLength(((_frameSourceSize.Width - def.Right) * frameScale) / scale, GridUnitType.Pixel);
    }

    /// <summary>
    /// Event raised when the user requests to exit from the splash screen.
    /// </summary>
    public event EventHandler? ExitRequested;

    private void OnSplashExitRequested(object? sender, EventArgs e)
    {
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Transitions from splash content to main content.
    /// </summary>
    /// <param name="mainViewModel">The main window view model to display.</param>
    public void TransitionToMainContent(MainWindowViewModel mainViewModel)
    {
        if (mainViewModel == null)
        {
            throw new ArgumentNullException(nameof(mainViewModel));
        }

        _mainViewModel = mainViewModel;
        
        Dispatcher.UIThread.Post(() =>
        {
            CurrentContent = mainViewModel;
            IsInSplashMode = false;
        });
    }

    /// <summary>
    /// Marks that the expansion animation is starting.
    /// </summary>
    public void BeginExpansionAnimation()
    {
        IsAnimating = true;
    }

    /// <summary>
    /// Marks that the expansion animation has completed.
    /// </summary>
    public void EndExpansionAnimation()
    {
        IsAnimating = false;
    }

    // IStartupLogger implementation - forward to splash view model
    public void Info(string message) => _splashViewModel.Info(message);
    public void Warn(string message) => _splashViewModel.Warn(message);
    public void Error(string message, Exception? exception = null) => _splashViewModel.Error(message, exception);

    public void Cleanup()
    {
        if (_cleanedUp)
            return;
        _cleanedUp = true;

        _splashViewModel.ExitRequested -= OnSplashExitRequested;
        _splashViewModel.Cleanup();
        
        // Dispose MainWindowViewModel if it was created
        _mainViewModel?.Dispose();
        _mainViewModel = null;
    }
}
