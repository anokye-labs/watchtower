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
    
    // Cached frame configuration for re-slicing on monitor switch
    private string? _frameSourceUri;
    private FrameSliceDefinition? _frameSliceDefinition;
    private Size _frameSourceSize;
    private double _renderScale = 1.0;
    private double _frameDisplayScale = 1.0;

    // Frame bitmap sources (dynamically sliced from source image)
    private Bitmap? _topLeftSource;
    private Bitmap? _topCenterSource;
    private Bitmap? _topRightSource;
    private Bitmap? _middleLeftSource;
    private Bitmap? _middleRightSource;
    private Bitmap? _bottomLeftSource;
    private Bitmap? _bottomCenterSource;
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
    
    // Frame bitmap sources - dynamically sliced from source image
    public Bitmap? TopLeftSource
    {
        get => _topLeftSource;
        private set => SetProperty(ref _topLeftSource, value);
    }
    
    public Bitmap? TopCenterSource
    {
        get => _topCenterSource;
        private set => SetProperty(ref _topCenterSource, value);
    }
    
    public Bitmap? TopRightSource
    {
        get => _topRightSource;
        private set => SetProperty(ref _topRightSource, value);
    }
    
    public Bitmap? MiddleLeftSource
    {
        get => _middleLeftSource;
        private set => SetProperty(ref _middleLeftSource, value);
    }
    
    public Bitmap? MiddleRightSource
    {
        get => _middleRightSource;
        private set => SetProperty(ref _middleRightSource, value);
    }
    
    public Bitmap? BottomLeftSource
    {
        get => _bottomLeftSource;
        private set => SetProperty(ref _bottomLeftSource, value);
    }
    
    public Bitmap? BottomCenterSource
    {
        get => _bottomCenterSource;
        private set => SetProperty(ref _bottomCenterSource, value);
    }
    
    public Bitmap? BottomRightSource
    {
        get => _bottomRightSource;
        private set => SetProperty(ref _bottomRightSource, value);
    }
    
    // Grid row/column definitions for the frame layout
    private GridLength _topRowHeight = GridLength.Auto;
    private GridLength _bottomRowHeight = GridLength.Auto;
    private GridLength _leftColumnWidth = GridLength.Auto;
    private GridLength _rightColumnWidth = GridLength.Auto;
    
    /// <summary>
    /// Height of the top row (corners and top edge).
    /// </summary>
    public GridLength TopRowHeight
    {
        get => _topRowHeight;
        private set => SetProperty(ref _topRowHeight, value);
    }
    
    /// <summary>
    /// Height of the bottom row (corners and bottom edge).
    /// </summary>
    public GridLength BottomRowHeight
    {
        get => _bottomRowHeight;
        private set => SetProperty(ref _bottomRowHeight, value);
    }
    
    /// <summary>
    /// Width of the left column (corners and left edge).
    /// </summary>
    public GridLength LeftColumnWidth
    {
        get => _leftColumnWidth;
        private set => SetProperty(ref _leftColumnWidth, value);
    }
    
    /// <summary>
    /// Width of the right column (corners and right edge).
    /// </summary>
    public GridLength RightColumnWidth
    {
        get => _rightColumnWidth;
        private set => SetProperty(ref _rightColumnWidth, value);
    }
    
    /// <summary>
    /// Loads and slices the frame image.
    /// Uses cached slices if available.
    /// </summary>
    /// <param name="sourceUri">URI to the source frame image (e.g., avares://WatchTower/Assets/main-frame.png).</param>
    /// <param name="sliceDefinition">The slice coordinates for 9-slice extraction (relative to original source).</param>
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
            System.Diagnostics.Debug.WriteLine("ShellWindowViewModel: Failed to load and slice frame image");
            return false;
        }
        
        _frameSourceSize = frameSlices.SourceSize;

        // Update bitmap sources - this will trigger property changed notifications
        TopLeftSource = frameSlices.TopLeft;
        TopCenterSource = frameSlices.TopCenter;
        TopRightSource = frameSlices.TopRight;
        MiddleLeftSource = frameSlices.MiddleLeft;
        MiddleRightSource = frameSlices.MiddleRight;
        BottomLeftSource = frameSlices.BottomLeft;
        BottomCenterSource = frameSlices.BottomCenter;
        BottomRightSource = frameSlices.BottomRight;
        
        UpdateFrameDimensions();
        
        System.Diagnostics.Debug.WriteLine($"ShellWindowViewModel: Frame loaded - SourceSize={_frameSourceSize}, TopLeft={frameSlices.TopLeft.Size}");
        return true;
    }
    
    /// <summary>
    /// Updates frame dimensions based on current RenderScale.
    /// </summary>
    private void UpdateFrameDimensions()
    {
        if (_frameSliceDefinition == null) return;

        var sliceDef = _frameSliceDefinition;
        var scale = RenderScale > 0 ? RenderScale : 1.0;

        // Calculate logical dimensions based on physical slice definition and render scale
        // Physical * FrameScale / RenderScale = Logical
        
        TopRowHeight = new GridLength((sliceDef.Top * FrameDisplayScale) / scale, GridUnitType.Pixel);
        BottomRowHeight = new GridLength(((_frameSourceSize.Height - sliceDef.Bottom) * FrameDisplayScale) / scale, GridUnitType.Pixel);
        LeftColumnWidth = new GridLength((sliceDef.Left * FrameDisplayScale) / scale, GridUnitType.Pixel);
        RightColumnWidth = new GridLength(((_frameSourceSize.Width - sliceDef.Right) * FrameDisplayScale) / scale, GridUnitType.Pixel);
        
        System.Diagnostics.Debug.WriteLine($"ShellWindowViewModel: Frame dimensions updated for Scale={scale}, FrameScale={FrameDisplayScale}: T={TopRowHeight.Value:F1}, B={BottomRowHeight.Value:F1}, L={LeftColumnWidth.Value:F1}, R={RightColumnWidth.Value:F1}");
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
        _splashViewModel.ExitRequested -= OnSplashExitRequested;
        _splashViewModel.Cleanup();
    }
}
