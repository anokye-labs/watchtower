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
        
        // Load windowed mode configuration into ViewModel if it's already set
        // If DataContext hasn't been set yet, OnDataContextChanged will handle it
        if (_viewModel != null)
        {
            _viewModel.LoadWindowedModeConfiguration(configuration);
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
        
        // Load windowed mode configuration if available and not already loaded
        // This handles the case where SetConfiguration was called before DataContext was set
        if (_viewModel != null && _configuration != null)
        {
            _viewModel.LoadWindowedModeConfiguration(_configuration);
        }
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
        
        // Set minimum size constraints based on frame padding + minimum content area
        SetMinimumSizeConstraints();

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
    /// Sets the minimum window size constraints based on frame padding and minimum content area.
    /// Enforces minimum size during resize operations.
    /// </summary>
    private void SetMinimumSizeConstraints()
    {
        if (_viewModel == null || _configuration == null)
        {
            return;
        }
        
        // Get frame padding from configuration
        var paddingLeft = _configuration.GetValue<double>("Frame:Padding:Left", 0);
        var paddingTop = _configuration.GetValue<double>("Frame:Padding:Top", 0);
        var paddingRight = _configuration.GetValue<double>("Frame:Padding:Right", 0);
        var paddingBottom = _configuration.GetValue<double>("Frame:Padding:Bottom", 0);
        
        // Calculate minimum window size including frame padding
        // This ensures the content area never gets smaller than the configured minimum
        MinWidth = _minContentWidth + paddingLeft + paddingRight;
        MinHeight = _minContentHeight + paddingTop + paddingBottom;
        
        // Enable resize in windowed mode
        if (_viewModel.IsWindowedModeEnabled)
        {
            CanResize = true;
        }
    }

    /// <summary>
    /// Sets the minimum window size based on frame static components plus minimum content area.
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
    /// Animates the window expansion from splash size to full-screen or windowed size.
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
            
            // Calculate target values based on windowed mode setting
            double targetX, targetY, targetWidth, targetHeight;
            
            if (_viewModel?.IsWindowedModeEnabled == true)
            {
                // Windowed mode: animate to configured initial size, keep centered
                targetWidth = _viewModel.InitialWindowWidth;
                targetHeight = _viewModel.InitialWindowHeight;
                
                // Center on screen
                var logicalScreenX = workingArea.X / scaling;
                var logicalScreenY = workingArea.Y / scaling;
                var logicalScreenWidth = workingArea.Width / scaling;
                var logicalScreenHeight = workingArea.Height / scaling;
                targetX = logicalScreenX + (logicalScreenWidth - targetWidth) / 2;
                targetY = logicalScreenY + (logicalScreenHeight - targetHeight) / 2;
            }
            else
            {
                // Fullscreen mode: animate to working area (existing behavior)
                targetX = workingArea.X / scaling;
                targetY = workingArea.Y / scaling;
                targetWidth = workingArea.Width / scaling;
                targetHeight = workingArea.Height / scaling;
            }

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

            // Phase 3: Expand back - respect windowed mode setting
            double targetX, targetY, targetWidth, targetHeight;
            
            if (_viewModel?.IsWindowedModeEnabled == true)
            {
                // Windowed mode: expand to configured initial size
                targetWidth = _viewModel.InitialWindowWidth;
                targetHeight = _viewModel.InitialWindowHeight;
                targetX = logicalScreenX + (logicalScreenWidth - targetWidth) / 2;
                targetY = logicalScreenY + (logicalScreenHeight - targetHeight) / 2;
            }
            else
            {
                // Fullscreen mode: expand to working area (existing behavior)
                targetX = logicalScreenX;
                targetY = logicalScreenY;
                targetWidth = logicalScreenWidth;
                targetHeight = logicalScreenHeight;
            }
            
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
    /// Handles monitor switch by smoothly resizing to the new screen's working area or staying at configured size.
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

        // Calculate target position and size based on windowed mode
        double targetX, targetY, targetWidth, targetHeight;
        
        if (_viewModel?.IsWindowedModeEnabled == true)
        {
            // Windowed mode: maintain current size, just re-center on new screen
            targetWidth = currentWidth;
            targetHeight = currentHeight;
            
            var logicalScreenX = workingArea.X / scaling;
            var logicalScreenY = workingArea.Y / scaling;
            var logicalScreenWidth = workingArea.Width / scaling;
            var logicalScreenHeight = workingArea.Height / scaling;
            targetX = logicalScreenX + (logicalScreenWidth - targetWidth) / 2;
            targetY = logicalScreenY + (logicalScreenHeight - targetHeight) / 2;
        }
        else
        {
            // Fullscreen mode: expand to new screen's working area (existing behavior)
            targetX = workingArea.X / scaling;
            targetY = workingArea.Y / scaling;
            targetWidth = workingArea.Width / scaling;
            targetHeight = workingArea.Height / scaling;
        }
        
        // Smoothly animate to new position/size (300ms)
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
}
