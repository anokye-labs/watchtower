using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using Microsoft.Extensions.Configuration;
using WatchTower.Services;
using WatchTower.ViewModels;

namespace WatchTower.Views;

/// <summary>
/// Defines the type of window resize operation based on frame region.
/// </summary>
internal enum ResizeMode
{
    None,
    TopLeft,
    Top,
    TopRight,
    Left,
    Right,
    BottomLeft,
    Bottom,
    BottomRight
}

/// <summary>
/// Result of a frame hit test operation.
/// </summary>
internal record FrameHitTestResult
{
    public bool IsOpaque { get; init; }
    public ResizeMode ResizeMode { get; init; }
    public StandardCursorType CursorType { get; init; }
}

public partial class ShellWindow : AnimatableWindow
{
    private ScrollViewer? _diagnosticsScroller;
    private ShellWindowViewModel? _viewModel;
    private bool _hasAnimated;
    private bool _isAnimating;
    private Screen? _currentScreen;
    private System.Threading.CancellationTokenSource? _monitorSwitchDebounce;
    private IConfiguration? _configuration;
    private double _minContentWidth = 400; // Minimum content area width (logical pixels)
    private double _minContentHeight = 300; // Minimum content area height (logical pixels)

    // Animation parameters
    private const int MonitorSwitchDebounceMs = 100; // Debounce delay for rapid monitor changes
    
    // Fallback dimensions if screen info unavailable
    private const int FallbackWidth = 800;
    private const int FallbackHeight = 600;
    
    // Frame grid reference
    private Grid? _frameGrid;
    
    // Frame interaction state
    private bool _isResizing;
    private bool _isDragging;
    private ResizeMode _currentResizeMode;
    private Point _dragStartPosition;
    private PixelPoint _windowStartPosition;
    private Size _windowStartSize;
    
    /// <summary>
    /// Sets the configuration for frame settings.
    /// Call this before the window is shown.
    /// </summary>
    public void SetConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
        
        // Load minimum content area dimensions
        _minContentWidth = configuration.GetValue("Startup:MinContentWidth", 400.0);
        if (_minContentWidth < 100 || _minContentWidth > 2000)
        {
            throw new InvalidOperationException(
                $"Startup:MinContentWidth must be between 100 and 2000. Current value: {_minContentWidth}");
        }
        
        _minContentHeight = configuration.GetValue("Startup:MinContentHeight", 300.0);
        if (_minContentHeight < 100 || _minContentHeight > 2000)
        {
            throw new InvalidOperationException(
                $"Startup:MinContentHeight must be between 100 and 2000. Current value: {_minContentHeight}");
        }
    }

    public ShellWindow()
    {
        InitializeComponent();
        
        // Subscribe to events
        KeyDown += OnKeyDown;
        Loaded += OnLoaded;
        Closed += OnWindowClosed;
        DataContextChanged += OnDataContextChanged;
        PositionChanged += OnPositionChanged;
        ScalingChanged += OnScalingChanged;
        
        // Initialize current screen tracking
        _currentScreen = Screens.ScreenFromWindow(this);
        
        // DON'T set up Transitions yet - we need to set initial splash size first
        // Transitions will be enabled just before first animation
    }

    private void OnScalingChanged(object? sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.RenderScale = RenderScaling;
        }
    }
    
    private void InitializeFrameElements()
    {
        // Get frame grid
        _frameGrid = this.FindControl<Grid>("FrameGrid");
    }
    
    /// <summary>
    /// Loads the frame image from source and slices it into 16 regions (5x5 grid).
    /// Should be called once during initialization.
    /// </summary>
    private void LoadFrameImage()
    {
        if (_viewModel == null)
        {
            return;
        }
        
        // Read frame configuration (5x5 grid requires 8 slice coordinates)
        var frameSourceUri = _configuration?.GetValue<string>("Frame:SourceUri") 
            ?? "avares://WatchTower/Assets/main-frame.png";
        
        // X coordinates (4 cut lines)
        var sliceLeft = _configuration?.GetValue<int>("Frame:Slice:Left") ?? 400;
        var sliceLeftInner = _configuration?.GetValue<int>("Frame:Slice:LeftInner") ?? 800;
        var sliceRightInner = _configuration?.GetValue<int>("Frame:Slice:RightInner") ?? 1200;
        var sliceRight = _configuration?.GetValue<int>("Frame:Slice:Right") ?? 1600;
        
        // Y coordinates (4 cut lines)
        var sliceTop = _configuration?.GetValue<int>("Frame:Slice:Top") ?? 400;
        var sliceTopInner = _configuration?.GetValue<int>("Frame:Slice:TopInner") ?? 800;
        var sliceBottomInner = _configuration?.GetValue<int>("Frame:Slice:BottomInner") ?? 1200;
        var sliceBottom = _configuration?.GetValue<int>("Frame:Slice:Bottom") ?? 1600;
        
        var frameScale = _configuration?.GetValue<double>("Frame:Scale") ?? 1.0;
        
        // Read padding configuration
        var paddingLeft = _configuration?.GetValue<double>("Frame:Padding:Left") ?? 0;
        var paddingTop = _configuration?.GetValue<double>("Frame:Padding:Top") ?? 0;
        var paddingRight = _configuration?.GetValue<double>("Frame:Padding:Right") ?? 0;
        var paddingBottom = _configuration?.GetValue<double>("Frame:Padding:Bottom") ?? 0;
        var backgroundColor = _configuration?.GetValue<string>("Frame:BackgroundColor") ?? "#1A1A1A";
        
        // Read frame interaction configuration
        var enableWindowedMode = _configuration?.GetValue<bool>("Frame:EnableWindowedMode") ?? true;
        var resizeHandleSize = _configuration?.GetValue<double>("Frame:ResizeHandleSize") ?? 8.0;
        
        var sliceDefinition = new FrameSliceDefinition
        {
            Left = sliceLeft,
            LeftInner = sliceLeftInner,
            RightInner = sliceRightInner,
            Right = sliceRight,
            Top = sliceTop,
            TopInner = sliceTopInner,
            BottomInner = sliceBottomInner,
            Bottom = sliceBottom
        };
        
        if (_viewModel.LoadFrameImageForScreen(frameSourceUri, sliceDefinition))
        {
            _viewModel.FrameDisplayScale = frameScale;
            _viewModel.RenderScale = RenderScaling;
            _viewModel.ContentPadding = new Thickness(paddingLeft, paddingTop, paddingRight, paddingBottom);
            _viewModel.BackgroundColor = backgroundColor;
            _viewModel.EnableWindowedMode = enableWindowedMode;
            _viewModel.ResizeHandleSize = resizeHandleSize;
        }
    }
    
    /// <summary>
    /// Updates frame slices for the current screen.
    /// Called on monitor switch to re-slice for new resolution.
    /// </summary>
    private void UpdateFrameScale()
    {
        // Initialize frame elements if not done yet
        if (_frameGrid == null)
        {
            InitializeFrameElements();
        }
        
        // Just update render scale, no need to re-slice
        if (_viewModel != null)
        {
            _viewModel.RenderScale = RenderScaling;
        }
    }
    
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        _viewModel = DataContext as ShellWindowViewModel;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _diagnosticsScroller = this.FindControl<ScrollViewer>("DiagnosticsScroller");

        if (_viewModel?.SplashViewModel != null)
        {
            _viewModel.SplashViewModel.DiagnosticMessages.CollectionChanged += OnDiagnosticMessagesChanged;
        }

        // Initialize frame elements
        InitializeFrameElements();
        
        // Load, resize, and slice the frame image for current screen
        LoadFrameImage();

        // Set initial splash window size if not already animated
        if (!_hasAnimated && _viewModel?.IsInSplashMode == true)
        {
            SetSplashSize();
        }
    }

    private void OnDiagnosticMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && _diagnosticsScroller != null)
        {
            // Scroll to bottom when new messages are added
            _diagnosticsScroller.ScrollToEnd();
        }
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        // Unsubscribe from events
        KeyDown -= OnKeyDown;
        Loaded -= OnLoaded;
        Closed -= OnWindowClosed;
        DataContextChanged -= OnDataContextChanged;
        PositionChanged -= OnPositionChanged;
        ScalingChanged -= OnScalingChanged;
        
        // Cancel any pending monitor switch debounce
        _monitorSwitchDebounce?.Cancel();
        _monitorSwitchDebounce?.Dispose();

        // Cleanup ViewModel
        if (_viewModel?.SplashViewModel != null)
        {
            _viewModel.SplashViewModel.DiagnosticMessages.CollectionChanged -= OnDiagnosticMessagesChanged;
            _viewModel.Cleanup();
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Ctrl+F5 replays splash animation (works in any mode, but only after initial animation)
        if (e.Key == Key.F5 && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (_hasAnimated && !_isAnimating)
            {
                _ = ReplaySplashAnimationAsync();
            }
            e.Handled = true;
            return;
        }
        
        if (_viewModel?.IsInSplashMode == true && _viewModel.SplashViewModel != null)
        {
            var splashViewModel = _viewModel.SplashViewModel;

            // D key toggles diagnostics
            if (e.Key == Key.D)
            {
                splashViewModel.ToggleDiagnosticsCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // Escape key exits
            if (e.Key == Key.Escape)
            {
                splashViewModel.ExitCommand.Execute(null);
                e.Handled = true;
            }
        }
        else if (_viewModel?.IsInSplashMode == false && _viewModel.CurrentContent is MainWindowViewModel mainViewModel)
        {
            // Forward key events to main window view model handling
            HandleMainWindowKeyEvents(mainViewModel, e);
        }
    }

    private void HandleMainWindowKeyEvents(MainWindowViewModel viewModel, KeyEventArgs e)
    {
        // Ctrl+R toggles Rich Text input
        if (e.Key == Key.R && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (viewModel.IsRichTextMode)
            {
                viewModel.CloseOverlayCommand.Execute(null);
            }
            else
            {
                viewModel.ShowRichTextInputCommand.Execute(null);
            }
            e.Handled = true;
            return;
        }

        // Ctrl+M toggles Voice (Mic) input
        if (e.Key == Key.M && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (viewModel.IsVoiceMode)
            {
                viewModel.CloseOverlayCommand.Execute(null);
            }
            else
            {
                viewModel.ShowVoiceInputCommand.Execute(null);
            }
            e.Handled = true;
            return;
        }

        // Ctrl+L toggles Event Log
        if (e.Key == Key.L && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            viewModel.ToggleEventLogCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // Escape key closes any overlay
        if (e.Key == Key.Escape && viewModel.IsOverlayVisible)
        {
            viewModel.CloseOverlayCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // Ctrl+Enter submits input (Rich Text mode only)
        if (e.Key == Key.Enter && e.KeyModifiers.HasFlag(KeyModifiers.Control) && viewModel.IsRichTextMode)
        {
            viewModel.SubmitInputCommand.Execute(null);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Sets the initial splash window size based on frame static components plus minimum content area.
    /// All values use logical coordinates/pixels consistently.
    /// </summary>
    private void SetSplashSize()
    {
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen != null)
        {
            var workingArea = screen.WorkingArea;
            var scaling = screen.Scaling;
            
            // Calculate frame-based minimum size (logical pixels)
            var frameSize = CalculateFrameBasedSplashSize(scaling);
            var logicalWidth = frameSize.Width;
            var logicalHeight = frameSize.Height;
            
            // Ensure window doesn't exceed screen size
            var maxLogicalWidth = workingArea.Width / scaling;
            var maxLogicalHeight = workingArea.Height / scaling;
            const double MaxScreenUsageRatio = 0.9; // Max 90% of screen
            logicalWidth = Math.Min(logicalWidth, maxLogicalWidth * MaxScreenUsageRatio);
            logicalHeight = Math.Min(logicalHeight, maxLogicalHeight * MaxScreenUsageRatio);
            
            // Calculate centered position in logical coordinates
            var logicalScreenX = workingArea.X / scaling;
            var logicalScreenY = workingArea.Y / scaling;
            var logicalScreenWidth = workingArea.Width / scaling;
            var logicalScreenHeight = workingArea.Height / scaling;
            var logicalLeft = logicalScreenX + (logicalScreenWidth - logicalWidth) / 2;
            var logicalTop = logicalScreenY + (logicalScreenHeight - logicalHeight) / 2;
            
            // Set both platform properties AND animated properties
            // AnimatableWindow converts logical to physical at the boundary
            Width = logicalWidth;
            Height = logicalHeight;
            AnimatedX = logicalLeft;
            AnimatedY = logicalTop;
            AnimatedWidth = logicalWidth;
            AnimatedHeight = logicalHeight;
        }
        else
        {
            // Fallback if screen info not available
            Width = FallbackWidth;
            Height = FallbackHeight;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            AnimatedX = 0;
            AnimatedY = 0;
            AnimatedWidth = FallbackWidth;
            AnimatedHeight = FallbackHeight;
        }
    }
    
    /// <summary>
    /// Calculates the minimum splash window size based on frame static components.
    /// Returns size in logical pixels accounting for frame scale and DPI.
    /// </summary>
    private Size CalculateFrameBasedSplashSize(double dpiScaling)
    {
        // Check if frame is loaded with slice definition
        if (_viewModel?.FrameSliceDefinition == null || _viewModel.FrameSourceSize == default)
        {
            return new Size(FallbackWidth, FallbackHeight);
        }
        
        var def = _viewModel.FrameSliceDefinition;
        var frameSourceSize = _viewModel.FrameSourceSize;
        var frameScale = _viewModel.FrameDisplayScale;
        var renderScale = _viewModel.RenderScale > 0 ? _viewModel.RenderScale : dpiScaling;
        var padding = _viewModel.ContentPadding;
        
        // Calculate fixed column widths (source pixels) for columns 0, 2, 4
        var col0Width = def.Left;                              // Left edge
        var col2Width = def.RightInner - def.LeftInner;        // Center column
        var col4Width = frameSourceSize.Width - def.Right;     // Right edge
        
        // Calculate fixed row heights (source pixels) for rows 0, 2, 4
        var row0Height = def.Top;                              // Top edge
        var row2Height = def.BottomInner - def.TopInner;       // Center row
        var row4Height = frameSourceSize.Height - def.Bottom;  // Bottom edge
        
        // Convert to logical pixels: (source pixels * frame scale) / DPI scale
        var frameLogicalWidth = ((col0Width + col2Width + col4Width) * frameScale) / renderScale;
        var frameLogicalHeight = ((row0Height + row2Height + row4Height) * frameScale) / renderScale;
        
        // Add padding and minimum content area
        var totalWidth = frameLogicalWidth + padding.Left + padding.Right + _minContentWidth;
        var totalHeight = frameLogicalHeight + padding.Top + padding.Bottom + _minContentHeight;
        
        return new Size(totalWidth, totalHeight);
    }

    /// <summary>
    /// Helper method to create an animation for a property.
    /// </summary>
    private Animation CreateAnimation(AvaloniaProperty property, double startValue, double endValue, int durationMs, Easing? easing = null)
    {
        return new Animation
        {
            Duration = TimeSpan.FromMilliseconds(durationMs),
            Easing = easing ?? new CubicEaseOut(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame 
                { 
                    Cue = new Cue(0.0), 
                    Setters = 
                    { 
                        new Avalonia.Styling.Setter(property, startValue) 
                    } 
                },
                new KeyFrame 
                { 
                    Cue = new Cue(1.0), 
                    Setters = 
                    { 
                        new Avalonia.Styling.Setter(property, endValue) 
                    } 
                }
            }
        };
    }

    /// <summary>
    /// Animates the window expansion from splash size to full-screen.
    /// All values use logical coordinates/pixels consistently.
    /// Uses Avalonia's Animation system with KeyFrame animations.
    /// </summary>
    public async Task AnimateExpansionAsync()
    {
        if (_hasAnimated)
        {
            return;
        }

        _hasAnimated = true;
        _isAnimating = true;
        _viewModel?.BeginExpansionAnimation();

        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
            if (screen == null)
            {
                // Fallback: set to default size
                Width = FallbackWidth;
                Height = FallbackHeight;
                _viewModel?.EndExpansionAnimation();
                _isAnimating = false;
                return;
            }

            var workingArea = screen.WorkingArea;
            var scaling = screen.Scaling;

            // Get starting values (already in logical coordinates)
            var startX = AnimatedX;
            var startY = AnimatedY;
            var startWidth = AnimatedWidth;
            var startHeight = AnimatedHeight;
            
            // Calculate target values in logical coordinates
            var targetX = workingArea.X / scaling;
            var targetY = workingArea.Y / scaling;
            var targetWidth = workingArea.Width / scaling;
            var targetHeight = workingArea.Height / scaling;

            // Run all animations in parallel
            await Task.WhenAll(
                CreateAnimation(AnimatedXProperty, startX, targetX, 500).RunAsync(this),
                CreateAnimation(AnimatedYProperty, startY, targetY, 500).RunAsync(this),
                CreateAnimation(AnimatedWidthProperty, startWidth, targetWidth, 500).RunAsync(this),
                CreateAnimation(AnimatedHeightProperty, startHeight, targetHeight, 500).RunAsync(this)
            );
            
            // Explicitly set final values to ensure they persist
            AnimatedX = targetX;
            AnimatedY = targetY;
            AnimatedWidth = targetWidth;
            AnimatedHeight = targetHeight;
            
            _viewModel?.EndExpansionAnimation();
            _isAnimating = false;
        });
    }



    /// <summary>
    /// Replays the splash animation: contracts to splash size, pauses, then expands back.
    /// Triggered by Ctrl+F5. All values use logical coordinates/pixels consistently.
    /// Uses Avalonia's Animation API with KeyFrame animations.
    /// </summary>
    private async Task ReplaySplashAnimationAsync()
    {
        if (_isAnimating) return;
        _isAnimating = true;
        _viewModel?.BeginExpansionAnimation();

        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
            if (screen == null)
            {
                _isAnimating = false;
                _viewModel?.EndExpansionAnimation();
                return;
            }

            var workingArea = screen.WorkingArea;
            var scaling = screen.Scaling;
            
            // Calculate splash size and position in logical coordinates
            var frameSize = CalculateFrameBasedSplashSize(scaling);
            var splashWidth = frameSize.Width;
            var splashHeight = frameSize.Height;
            var logicalScreenX = workingArea.X / scaling;
            var logicalScreenY = workingArea.Y / scaling;
            var logicalScreenWidth = workingArea.Width / scaling;
            var logicalScreenHeight = workingArea.Height / scaling;
            var splashLeft = logicalScreenX + (logicalScreenWidth - splashWidth) / 2;
            var splashTop = logicalScreenY + (logicalScreenHeight - splashHeight) / 2;

            // Get current values (already in logical coordinates)
            var currentX = AnimatedX;
            var currentY = AnimatedY;
            var currentWidth = AnimatedWidth;
            var currentHeight = AnimatedHeight;

            // Phase 1: Contract to splash size (500ms)
            await Task.WhenAll(
                CreateAnimation(AnimatedXProperty, currentX, splashLeft, 500).RunAsync(this),
                CreateAnimation(AnimatedYProperty, currentY, splashTop, 500).RunAsync(this),
                CreateAnimation(AnimatedWidthProperty, currentWidth, splashWidth, 500).RunAsync(this),
                CreateAnimation(AnimatedHeightProperty, currentHeight, splashHeight, 500).RunAsync(this)
            );

            // Phase 2: Brief pause at splash size (100ms)
            await Task.Delay(100);

            // Phase 3: Expand back to working area (500ms) - all in logical coordinates
            var targetX = logicalScreenX;
            var targetY = logicalScreenY;
            var targetWidth = logicalScreenWidth;
            var targetHeight = logicalScreenHeight;
            
            await Task.WhenAll(
                CreateAnimation(AnimatedXProperty, splashLeft, targetX, 500).RunAsync(this),
                CreateAnimation(AnimatedYProperty, splashTop, targetY, 500).RunAsync(this),
                CreateAnimation(AnimatedWidthProperty, splashWidth, targetWidth, 500).RunAsync(this),
                CreateAnimation(AnimatedHeightProperty, splashHeight, targetHeight, 500).RunAsync(this)
            );
            
            // Explicitly set final values to ensure they persist
            AnimatedX = targetX;
            AnimatedY = targetY;
            AnimatedWidth = targetWidth;
            AnimatedHeight = targetHeight;
            
            _viewModel?.EndExpansionAnimation();
            _isAnimating = false;
        });
    }

    /// <summary>
    /// Handles position changes to detect monitor switches.
    /// </summary>
    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        // Don't check during animations
        if (_isAnimating) return;
        
        var newScreen = Screens.ScreenFromWindow(this);
        if (newScreen != null && newScreen != _currentScreen && _currentScreen != null)
        {
            // Monitor changed - trigger debounced animation
            TriggerMonitorSwitchAnimation(newScreen);
        }
        _currentScreen = newScreen;
    }

    /// <summary>
    /// Triggers a debounced monitor switch animation.
    /// </summary>
    private void TriggerMonitorSwitchAnimation(Screen newScreen)
    {
        // Cancel any pending debounce
        _monitorSwitchDebounce?.Cancel();
        _monitorSwitchDebounce?.Dispose();
        _monitorSwitchDebounce = new System.Threading.CancellationTokenSource();
        var token = _monitorSwitchDebounce.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(MonitorSwitchDebounceMs, token);
                if (!token.IsCancellationRequested)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => OnMonitorSwitchedAsync(newScreen));
                }
            }
            catch (TaskCanceledException)
            {
                // Debounce was cancelled, ignore
            }
        }, token);
    }

    /// <summary>
    /// Handles monitor switch by smoothly resizing to the new screen's working area.
    /// All values use logical coordinates/pixels consistently.
    /// Uses Avalonia's Animation API with 300ms timing.
    /// </summary>
    private async Task OnMonitorSwitchedAsync(Screen newScreen)
    {
        if (_isAnimating) return;
        _isAnimating = true;
        _viewModel?.BeginExpansionAnimation();

        var workingArea = newScreen.WorkingArea;
        var scaling = newScreen.Scaling;
        
        // Sync animated properties from actual window state (converts to logical coordinates)
        SyncFromWindowState();
        
        // Get current values (already in logical coordinates)
        var currentX = AnimatedX;
        var currentY = AnimatedY;
        var currentWidth = AnimatedWidth;
        var currentHeight = AnimatedHeight;

        // Calculate target position and size in logical coordinates
        var targetX = workingArea.X / scaling;
        var targetY = workingArea.Y / scaling;
        var targetWidth = workingArea.Width / scaling;
        var targetHeight = workingArea.Height / scaling;
        
        // Smoothly animate directly to new screen size (300ms)
        await Task.WhenAll(
            CreateAnimation(AnimatedXProperty, currentX, targetX, 300).RunAsync(this),
            CreateAnimation(AnimatedYProperty, currentY, targetY, 300).RunAsync(this),
            CreateAnimation(AnimatedWidthProperty, currentWidth, targetWidth, 300).RunAsync(this),
            CreateAnimation(AnimatedHeightProperty, currentHeight, targetHeight, 300).RunAsync(this)
        );
        
        // Explicitly set final values to ensure they persist
        AnimatedX = targetX;
        AnimatedY = targetY;
        AnimatedWidth = targetWidth;
        AnimatedHeight = targetHeight;

        _currentScreen = newScreen;
        
        // Update frame scale for the new screen
        UpdateFrameScale();
        
        _viewModel?.EndExpansionAnimation();
        _isAnimating = false;
    }

    /// <summary>
    /// Handles backdrop taps when in main content mode to close overlays.
    /// </summary>
    private void OnMainBackdropTapped(object? sender, RoutedEventArgs e)
    {
        if (_viewModel?.CurrentContent is MainWindowViewModel mainViewModel)
        {
            mainViewModel.CloseOverlayCommand.Execute(null);
        }
    }
    
    /// <summary>
    /// Handles pointer pressed events on the frame grid for window dragging and resizing.
    /// </summary>
    private void OnFramePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_viewModel == null || !_viewModel.EnableWindowedMode || _isAnimating)
        {
            // Pass through - let clicks reach content below
            e.Handled = false;
            return;
        }
        
        // Get the pointer position relative to the window
        var position = e.GetPosition(this);
        var windowSize = new Size(Width, Height);
        
        // Perform hit test using simplified heuristic
        var hitResult = PerformFrameHitTest(position, windowSize);
        
        if (!hitResult.IsOpaque)
        {
            // Transparent region - pass through to content
            e.Handled = false;
            return;
        }
        
        // Opaque region - capture for window interaction
        e.Handled = true;
        
        _currentResizeMode = hitResult.ResizeMode;
        _dragStartPosition = position;
        _windowStartPosition = Position;
        _windowStartSize = new Size(Width, Height);
        
        if (hitResult.ResizeMode == ResizeMode.None)
        {
            // No resize zone - enable dragging
            _isDragging = true;
        }
        else
        {
            // In resize zone - enable resizing
            _isResizing = true;
        }
    }
    
    /// <summary>
    /// Handles pointer moved events on the frame grid for cursor updates and resize/drag operations.
    /// </summary>
    private void OnFramePointerMoved(object? sender, PointerEventArgs e)
    {
        if (_viewModel == null || !_viewModel.EnableWindowedMode || _isAnimating)
        {
            return;
        }
        
        var position = e.GetPosition(this);
        
        // Handle active resize or drag
        if (_isResizing)
        {
            PerformResize(position);
            e.Handled = true;
            return;
        }
        
        if (_isDragging)
        {
            PerformDrag(position);
            e.Handled = true;
            return;
        }
        
        // Update cursor based on frame region
        var windowSize = new Size(Width, Height);
        var hitResult = PerformFrameHitTest(position, windowSize);
        
        if (hitResult.IsOpaque)
        {
            Cursor = new Cursor(hitResult.CursorType);
            e.Handled = true;
        }
        else
        {
            Cursor = Cursor.Default;
            e.Handled = false;
        }
    }
    
    /// <summary>
    /// Handles pointer released events on the frame grid to end resize/drag operations.
    /// </summary>
    private void OnFramePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isResizing || _isDragging)
        {
            _isResizing = false;
            _isDragging = false;
            _currentResizeMode = ResizeMode.None;
            e.Handled = true;
        }
    }
    
    /// <summary>
    /// Performs a simplified frame hit test to determine if a point is on an opaque frame region.
    /// </summary>
    private FrameHitTestResult PerformFrameHitTest(Point point, Size windowSize)
    {
        if (_viewModel?.CurrentFrameSlices == null)
        {
            return new FrameHitTestResult
            {
                IsOpaque = false,
                ResizeMode = ResizeMode.None,
                CursorType = StandardCursorType.Arrow
            };
        }
        
        // Use simplified heuristic approach for hit testing
        // This checks if the point is within the frame border regions
        var def = _viewModel.FrameSliceDefinition;
        if (def == null)
        {
            return new FrameHitTestResult
            {
                IsOpaque = false,
                ResizeMode = ResizeMode.None,
                CursorType = StandardCursorType.Arrow
            };
        }
        
        var sourceSize = _viewModel.FrameSourceSize;
        var scale = RenderScaling > 0 ? RenderScaling : 1.0;
        var frameScale = _viewModel.FrameDisplayScale;
        
        // Calculate logical dimensions of all frame regions (including stretchable)
        var col0Width = (def.Left * frameScale) / scale;
        var col1Width = ((def.LeftInner - def.Left) * frameScale) / scale;
        var col3Width = ((def.Right - def.RightInner) * frameScale) / scale;
        var col4Width = ((sourceSize.Width - def.Right) * frameScale) / scale;
        
        var row0Height = (def.Top * frameScale) / scale;
        var row1Height = ((def.TopInner - def.Top) * frameScale) / scale;
        var row3Height = ((def.Bottom - def.BottomInner) * frameScale) / scale;
        var row4Height = ((sourceSize.Height - def.Bottom) * frameScale) / scale;
        
        // Calculate content area boundaries (excluding all frame border regions)
        var contentLeft = col0Width + col1Width;
        var contentRight = windowSize.Width - col3Width - col4Width;
        var contentTop = row0Height + row1Height;
        var contentBottom = windowSize.Height - row3Height - row4Height;
        
        // Check if point is in content area (center of 5x5 grid)
        var isInContentArea = point.X >= contentLeft && point.X < contentRight &&
                              point.Y >= contentTop && point.Y < contentBottom;
        
        if (isInContentArea)
        {
            // Point is in content area
            return new FrameHitTestResult
            {
                IsOpaque = false,
                ResizeMode = ResizeMode.None,
                CursorType = StandardCursorType.Arrow
            };
        }
        
        // Point is in frame region - determine resize mode
        var resizeMode = DetermineResizeMode(point, windowSize);
        var cursorType = GetCursorForResizeMode(resizeMode);
        
        return new FrameHitTestResult
        {
            IsOpaque = true,
            ResizeMode = resizeMode,
            CursorType = cursorType
        };
    }
    
    /// <summary>
    /// Determines the resize mode based on pointer position.
    /// </summary>
    private ResizeMode DetermineResizeMode(Point point, Size windowSize)
    {
        var handleSize = _viewModel?.ResizeHandleSize ?? 8.0;
        
        var nearLeft = point.X < handleSize;
        var nearRight = point.X > windowSize.Width - handleSize;
        var nearTop = point.Y < handleSize;
        var nearBottom = point.Y > windowSize.Height - handleSize;
        
        // Check corners first
        if (nearTop && nearLeft) return ResizeMode.TopLeft;
        if (nearTop && nearRight) return ResizeMode.TopRight;
        if (nearBottom && nearLeft) return ResizeMode.BottomLeft;
        if (nearBottom && nearRight) return ResizeMode.BottomRight;
        
        // Check edges
        if (nearTop) return ResizeMode.Top;
        if (nearBottom) return ResizeMode.Bottom;
        if (nearLeft) return ResizeMode.Left;
        if (nearRight) return ResizeMode.Right;
        
        // Not in a resize zone - allow dragging
        return ResizeMode.None;
    }
    
    /// <summary>
    /// Gets the appropriate cursor for a resize mode.
    /// </summary>
    private StandardCursorType GetCursorForResizeMode(ResizeMode mode)
    {
        return mode switch
        {
            ResizeMode.TopLeft => StandardCursorType.TopLeftCorner,
            ResizeMode.Top => StandardCursorType.TopSide,
            ResizeMode.TopRight => StandardCursorType.TopRightCorner,
            ResizeMode.Left => StandardCursorType.LeftSide,
            ResizeMode.Right => StandardCursorType.RightSide,
            ResizeMode.BottomLeft => StandardCursorType.BottomLeftCorner,
            ResizeMode.Bottom => StandardCursorType.BottomSide,
            ResizeMode.BottomRight => StandardCursorType.BottomRightCorner,
            _ => StandardCursorType.Arrow
        };
    }
    
    /// <summary>
    /// Performs window resize based on pointer movement.
    /// </summary>
    private void PerformResize(Point currentPosition)
    {
        var deltaX = currentPosition.X - _dragStartPosition.X;
        var deltaY = currentPosition.Y - _dragStartPosition.Y;
        
        var newX = _windowStartPosition.X;
        var newY = _windowStartPosition.Y;
        var newWidth = _windowStartSize.Width;
        var newHeight = _windowStartSize.Height;
        
        // Calculate frame dimensions to respect minimum size
        var frameWidth = 0.0;
        var frameHeight = 0.0;
        
        if (_viewModel?.FrameSliceDefinition != null)
        {
            var def = _viewModel.FrameSliceDefinition;
            var sourceSize = _viewModel.FrameSourceSize;
            var scale = RenderScaling > 0 ? RenderScaling : 1.0;
            var frameScale = _viewModel.FrameDisplayScale;
            var padding = _viewModel.ContentPadding;
            
            var col0Width = (def.Left * frameScale) / scale;
            var col2Width = ((def.RightInner - def.LeftInner) * frameScale) / scale;
            var col4Width = ((sourceSize.Width - def.Right) * frameScale) / scale;
            
            var row0Height = (def.Top * frameScale) / scale;
            var row2Height = ((def.BottomInner - def.TopInner) * frameScale) / scale;
            var row4Height = ((sourceSize.Height - def.Bottom) * frameScale) / scale;
            
            frameWidth = col0Width + col2Width + col4Width + padding.Left + padding.Right;
            frameHeight = row0Height + row2Height + row4Height + padding.Top + padding.Bottom;
        }
        
        var minWidth = Math.Max(frameWidth + _minContentWidth, 200);
        var minHeight = Math.Max(frameHeight + _minContentHeight, 150);
        
        var scaling = GetCurrentScaling();
        
        switch (_currentResizeMode)
        {
            case ResizeMode.TopLeft:
                newWidth = Math.Max(minWidth, _windowStartSize.Width - deltaX);
                newHeight = Math.Max(minHeight, _windowStartSize.Height - deltaY);
                if (newWidth > minWidth)
                {
                    newX = _windowStartPosition.X + (int)(deltaX * scaling);
                }
                if (newHeight > minHeight)
                {
                    newY = _windowStartPosition.Y + (int)(deltaY * scaling);
                }
                break;
                
            case ResizeMode.Top:
                newHeight = Math.Max(minHeight, _windowStartSize.Height - deltaY);
                if (newHeight > minHeight)
                {
                    newY = _windowStartPosition.Y + (int)(deltaY * scaling);
                }
                break;
                
            case ResizeMode.TopRight:
                newWidth = Math.Max(minWidth, _windowStartSize.Width + deltaX);
                newHeight = Math.Max(minHeight, _windowStartSize.Height - deltaY);
                if (newHeight > minHeight)
                {
                    newY = _windowStartPosition.Y + (int)(deltaY * scaling);
                }
                break;
                
            case ResizeMode.Left:
                newWidth = Math.Max(minWidth, _windowStartSize.Width - deltaX);
                if (newWidth > minWidth)
                {
                    newX = _windowStartPosition.X + (int)(deltaX * scaling);
                }
                break;
                
            case ResizeMode.Right:
                newWidth = Math.Max(minWidth, _windowStartSize.Width + deltaX);
                break;
                
            case ResizeMode.BottomLeft:
                newWidth = Math.Max(minWidth, _windowStartSize.Width - deltaX);
                newHeight = Math.Max(minHeight, _windowStartSize.Height + deltaY);
                if (newWidth > minWidth)
                {
                    newX = _windowStartPosition.X + (int)(deltaX * scaling);
                }
                break;
                
            case ResizeMode.Bottom:
                newHeight = Math.Max(minHeight, _windowStartSize.Height + deltaY);
                break;
                
            case ResizeMode.BottomRight:
                newWidth = Math.Max(minWidth, _windowStartSize.Width + deltaX);
                newHeight = Math.Max(minHeight, _windowStartSize.Height + deltaY);
                break;
        }
        
        // Apply new dimensions
        Position = new PixelPoint(newX, newY);
        Width = newWidth;
        Height = newHeight;
    }
    
    /// <summary>
    /// Performs window drag based on pointer movement.
    /// </summary>
    private void PerformDrag(Point currentPosition)
    {
        var deltaX = currentPosition.X - _dragStartPosition.X;
        var deltaY = currentPosition.Y - _dragStartPosition.Y;
        
        var scaling = GetCurrentScaling();
        var newX = _windowStartPosition.X + (int)(deltaX * scaling);
        var newY = _windowStartPosition.Y + (int)(deltaY * scaling);
        
        Position = new PixelPoint(newX, newY);
    }
    
    /// <summary>
    /// Gets the current DPI scaling factor for this window.
    /// </summary>
    private double GetCurrentScaling()
    {
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        return screen?.Scaling ?? 1.0;
    }
}
