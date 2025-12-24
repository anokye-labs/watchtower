using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using Microsoft.Extensions.Configuration;
using WatchTower.Services;
using WatchTower.ViewModels;

namespace WatchTower.Views;

public partial class ShellWindow : Window
{
    private ScrollViewer? _diagnosticsScroller;
    private ShellWindowViewModel? _viewModel;
    private bool _hasAnimated;
    private bool _isAnimating;
    private Screen? _currentScreen;
    private System.Threading.CancellationTokenSource? _monitorSwitchDebounce;
    private IConfiguration? _configuration;

    // Animation parameters
    private const int StartupAnimationDurationMs = 1000;
    private const int ReplayAnimationDurationMs = 1000; // Each direction for Ctrl+F5 (2000ms total)
    private const int MonitorSwitchDurationMs = 250; // Each direction for monitor switch (500ms total)
    private const int AnimationBufferMs = 100; // Extra buffer after animation completes
    private const int MonitorSwitchDebounceMs = 100; // Debounce delay for rapid monitor changes
    private const double SplashSizeRatio = 0.7; // 70% of screen size
    
    // Fallback dimensions if screen info unavailable
    private const int FallbackWidth = 800;
    private const int FallbackHeight = 600;
    
    // Overlay sizing
    private const int DefaultOverlayHeight = 400;
    private const double OverlayHeightRatio = 0.6;
    
    // Frame grid reference
    private Grid? _frameGrid;
    
    /// <summary>
    /// Sets the configuration for frame settings.
    /// Call this before the window is shown.
    /// </summary>
    public void SetConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
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
        
        System.Diagnostics.Debug.WriteLine($"InitializeFrameElements: frameGrid={_frameGrid != null}");
    }
    
    /// <summary>
    /// Loads the frame image from source and slices it into 16 regions (5x5 grid).
    /// Should be called once during initialization.
    /// </summary>
    private void LoadFrameImage()
    {
        if (_viewModel == null)
        {
            System.Diagnostics.Debug.WriteLine("LoadFrameImage: ViewModel is null");
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
        
        System.Diagnostics.Debug.WriteLine($"LoadFrameImage: source={frameSourceUri}, slice X=L:{sliceLeft} LI:{sliceLeftInner} RI:{sliceRightInner} R:{sliceRight}, Y=T:{sliceTop} TI:{sliceTopInner} BI:{sliceBottomInner} B:{sliceBottom}, scale={frameScale}, padding=({paddingLeft},{paddingTop},{paddingRight},{paddingBottom}), bg={backgroundColor}");
        
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
            System.Diagnostics.Debug.WriteLine("LoadFrameImage: Successfully loaded and sliced frame image (5x5)");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("LoadFrameImage: Failed to load frame image");
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
    
    /// <summary>
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Unsubscribe from previous view model
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnShellViewModelPropertyChanged;
        }

        _viewModel = DataContext as ShellWindowViewModel;

        // Subscribe to new view model
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += OnShellViewModelPropertyChanged;
        }
    }

    private void OnShellViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // When CurrentContent changes to MainWindowViewModel, set up overlay animations
        if (e.PropertyName == nameof(ShellWindowViewModel.CurrentContent))
        {
            if (_viewModel?.CurrentContent is MainWindowViewModel mainViewModel)
            {
                // Subscribe to property changes for overlay animations
                mainViewModel.PropertyChanged += OnMainViewModelPropertyChanged;
                
                // Configure adaptive card theme after content switches
                Dispatcher.UIThread.Post(() =>
                {
                    ConfigureAdaptiveCardTheme();
                }, DispatcherPriority.Loaded);
            }
        }
    }

    private void OnMainViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not MainWindowViewModel vm)
        {
            return;
        }

        // Handle input overlay (Rich Text / Voice) - slides from bottom
        if (e.PropertyName == nameof(MainWindowViewModel.IsInputOverlayVisible))
        {
            AnimateInputOverlay(vm);
        }

        // Handle event log overlay - slides from left
        if (e.PropertyName == nameof(MainWindowViewModel.IsEventLogVisible))
        {
            AnimateEventLogOverlay(vm);
        }
    }

    private void AnimateInputOverlay(MainWindowViewModel vm)
    {
        var overlayPanel = this.FindControl<Border>("OverlayPanel");
        if (overlayPanel == null)
        {
            return;
        }

        var overlayTransform = overlayPanel.RenderTransform as TranslateTransform;
        if (overlayTransform == null)
        {
            return;
        }

        // Calculate slide distance
        var slideDistance = overlayPanel.Bounds.Height > 0 
            ? overlayPanel.Bounds.Height 
            : Math.Min(DefaultOverlayHeight, Bounds.Height * OverlayHeightRatio);

        // Initialize transitions if not already created
        if (overlayPanel.Transitions == null || overlayPanel.Transitions.Count == 0)
        {
            overlayPanel.Transitions = new Transitions
            {
                new DoubleTransition
                {
                    Property = TranslateTransform.YProperty,
                    Duration = TimeSpan.FromMilliseconds(300),
                    Easing = new CubicEaseOut()
                }
            };
        }

        if (vm.IsInputOverlayVisible)
        {
            // Start off-screen at the bottom
            overlayTransform.Y = slideDistance;
            
            // Slide up
            overlayTransform.Y = 0.0;
            
            // Auto-focus the TextBox when rich text mode is shown
            if (vm.IsRichTextMode)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    var inputTextBox = this.FindControl<TextBox>("InputTextBox");
                    inputTextBox?.Focus();
                }, DispatcherPriority.Loaded);
            }
        }
        else
        {
            // Slide down
            overlayTransform.Y = slideDistance;
        }
    }

    private void AnimateEventLogOverlay(MainWindowViewModel vm)
    {
        var eventLogPanel = this.FindControl<Border>("EventLogPanel");
        if (eventLogPanel == null)
        {
            return;
        }

        var eventLogTransform = eventLogPanel.RenderTransform as TranslateTransform;
        if (eventLogTransform == null)
        {
            return;
        }

        // Use actual panel width for slide distance
        var slideDistance = eventLogPanel.Bounds.Width > 0 ? eventLogPanel.Bounds.Width : Bounds.Width / 2;

        // Initialize transitions if not already created
        if (eventLogPanel.Transitions == null)
        {
            eventLogPanel.Transitions = new Transitions();
        }

        var transitions = eventLogPanel.Transitions;
        DoubleTransition? slideTransition = null;

        // Find existing transition or create new one
        for (int i = 0; i < transitions.Count; i++)
        {
            if (transitions[i] is DoubleTransition dt &&
                dt.Property == TranslateTransform.XProperty)
            {
                slideTransition = dt;
                break;
            }
        }

        if (slideTransition == null)
        {
            slideTransition = new DoubleTransition
            {
                Property = TranslateTransform.XProperty,
                Duration = TimeSpan.FromMilliseconds(300),
                Easing = new CubicEaseOut()
            };
            transitions.Add(slideTransition);
        }

        if (vm.IsEventLogVisible)
        {
            // Start off-screen to the left
            eventLogTransform.X = -slideDistance;

            // Slide in from left
            slideTransition.Easing = new CubicEaseOut();
            eventLogTransform.X = 0.0;
        }
        else
        {
            // Slide out to the left
            slideTransition.Easing = new CubicEaseIn();
            eventLogTransform.X = -slideDistance;
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
    /// Sets the initial splash window size (70% of screen, centered).
    /// </summary>
    private void SetSplashSize()
    {
        var screen = Screens.Primary;
        if (screen != null)
        {
            var workingArea = screen.WorkingArea;
            var splashWidth = workingArea.Width * SplashSizeRatio;
            var splashHeight = workingArea.Height * SplashSizeRatio;

            Width = splashWidth;
            Height = splashHeight;
            
            // Center the window
            var left = (workingArea.Width - splashWidth) / 2;
            var top = (workingArea.Height - splashHeight) / 2;
            Position = new PixelPoint((int)left, (int)top);
        }
        else
        {
            // Fallback if screen info not available
            Width = FallbackWidth;
            Height = FallbackHeight;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }

    /// <summary>
    /// Animates the window expansion from splash size to full-screen.
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
            var screen = Screens.Primary;
            if (screen == null)
            {
                // Fallback: just maximize
                WindowState = WindowState.Maximized;
                _viewModel?.EndExpansionAnimation();
                _isAnimating = false;
                return;
            }

            var workingArea = screen.WorkingArea;
            var startWidth = Width;
            var startHeight = Height;
            var startPosition = Position;

            var targetWidth = (double)workingArea.Width;
            var targetHeight = (double)workingArea.Height;
            var targetPosition = new PixelPoint(workingArea.X, workingArea.Y);

            await AnimateWindowSizeAsync(
                startWidth, startHeight, startPosition,
                targetWidth, targetHeight, targetPosition,
                StartupAnimationDurationMs);
            
            // Ensure we're exactly at the target
            WindowState = WindowState.Maximized;
            
            _viewModel?.EndExpansionAnimation();
            _isAnimating = false;
        });
    }

    /// <summary>
    /// Generic bidirectional window size animation helper.
    /// </summary>
    private async Task AnimateWindowSizeAsync(
        double startWidth, double startHeight, PixelPoint startPosition,
        double endWidth, double endHeight, PixelPoint endPosition,
        int durationMs)
    {
        var tcs = new TaskCompletionSource<bool>();
        var startTime = DateTime.UtcNow;
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
        };

        timer.Tick += (s, e) =>
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            var progress = Math.Min(elapsed / durationMs, 1.0);
            
            // Apply cubic ease-out
            var easedProgress = CubicEaseOut(progress);

            // Interpolate width and height
            var currentWidth = startWidth + (endWidth - startWidth) * easedProgress;
            var currentHeight = startHeight + (endHeight - startHeight) * easedProgress;
            
            // Interpolate position
            var currentX = startPosition.X + (int)((endPosition.X - startPosition.X) * easedProgress);
            var currentY = startPosition.Y + (int)((endPosition.Y - startPosition.Y) * easedProgress);

            Width = currentWidth;
            Height = currentHeight;
            Position = new PixelPoint(currentX, currentY);

            if (progress >= 1.0)
            {
                timer.Stop();
                tcs.TrySetResult(true);
            }
        };

        timer.Start();
        await tcs.Task;
    }

    /// <summary>
    /// Replays the splash animation: contracts to splash size, pauses, then expands back.
    /// Triggered by Ctrl+F5.
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
            
            // Calculate splash size (70% of screen, centered)
            var splashWidth = workingArea.Width * SplashSizeRatio;
            var splashHeight = workingArea.Height * SplashSizeRatio;
            var splashLeft = workingArea.X + (int)((workingArea.Width - splashWidth) / 2);
            var splashTop = workingArea.Y + (int)((workingArea.Height - splashHeight) / 2);
            var splashPosition = new PixelPoint(splashLeft, splashTop);

            // Store current state
            var currentWidth = Width;
            var currentHeight = Height;
            var currentPosition = Position;
            
            // If maximized, restore first
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                Width = workingArea.Width;
                Height = workingArea.Height;
                Position = new PixelPoint(workingArea.X, workingArea.Y);
                currentWidth = Width;
                currentHeight = Height;
                currentPosition = Position;
            }

            // Phase 1: Contract to splash size
            await AnimateWindowSizeAsync(
                currentWidth, currentHeight, currentPosition,
                splashWidth, splashHeight, splashPosition,
                ReplayAnimationDurationMs);

            // Phase 2: Brief pause at splash size
            await Task.Delay(200);

            // Phase 3: Expand back to fullscreen
            var targetPosition = new PixelPoint(workingArea.X, workingArea.Y);
            await AnimateWindowSizeAsync(
                splashWidth, splashHeight, splashPosition,
                workingArea.Width, workingArea.Height, targetPosition,
                ReplayAnimationDurationMs);

            // Ensure we're exactly at the target
            WindowState = WindowState.Maximized;
            
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
    /// Handles monitor switch by zooming down and back up on the new screen.
    /// </summary>
    private async Task OnMonitorSwitchedAsync(Screen newScreen)
    {
        if (_isAnimating) return;
        _isAnimating = true;
        _viewModel?.BeginExpansionAnimation();

        var workingArea = newScreen.WorkingArea;
        
        // Calculate splash size for the new screen (70% of screen, centered)
        var splashWidth = workingArea.Width * SplashSizeRatio;
        var splashHeight = workingArea.Height * SplashSizeRatio;
        var splashLeft = workingArea.X + (int)((workingArea.Width - splashWidth) / 2);
        var splashTop = workingArea.Y + (int)((workingArea.Height - splashHeight) / 2);
        var splashPosition = new PixelPoint(splashLeft, splashTop);

        // Store current state
        var currentWidth = Width;
        var currentHeight = Height;
        var currentPosition = Position;
        
        // If maximized, restore first
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            // Use current screen dimensions as starting point
            var prevScreen = _currentScreen ?? newScreen;
            Width = prevScreen.WorkingArea.Width;
            Height = prevScreen.WorkingArea.Height;
            currentWidth = Width;
            currentHeight = Height;
        }

        // Phase 1: Zoom down to splash size on new screen
        await AnimateWindowSizeAsync(
            currentWidth, currentHeight, currentPosition,
            splashWidth, splashHeight, splashPosition,
            MonitorSwitchDurationMs);

        // Phase 2: Zoom back up to fullscreen on new screen
        var targetPosition = new PixelPoint(workingArea.X, workingArea.Y);
        await AnimateWindowSizeAsync(
            splashWidth, splashHeight, splashPosition,
            workingArea.Width, workingArea.Height, targetPosition,
            MonitorSwitchDurationMs);

        // Ensure we're exactly at the target
        WindowState = WindowState.Maximized;
        _currentScreen = newScreen;
        
        // Update frame scale for the new screen
        UpdateFrameScale();
        
        _viewModel?.EndExpansionAnimation();
        _isAnimating = false;
    }

    /// <summary>
    /// Cubic ease-out easing function.
    /// </summary>
    private static double CubicEaseOut(double t)
    {
        var p = t - 1;
        return p * p * p + 1;
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

    private void ConfigureAdaptiveCardTheme()
    {
        // Get the AdaptiveCardView control
        var adaptiveCardView = this.FindControl<AdaptiveCards.Rendering.Avalonia.AdaptiveCardView>("AdaptiveCardView");
        if (adaptiveCardView != null)
        {
            try
            {
                // Create a renderer and get its default host config
                var renderer = new AdaptiveCards.Rendering.Avalonia.AdaptiveCardRenderer();
                var hostConfig = renderer.HostConfig;
                
                if (hostConfig != null)
                {
                    // NOTE: Using reflection to configure HostConfig for dark theme
                    // This is necessary because the AdaptiveCards library (v3.1.0) doesn't expose
                    // a direct API for creating or modifying HostConfig in code.
                    // If the library's internal structure changes, this code may need updates.
                    // Tested with: Iciclecreek.AdaptiveCards.Rendering.Avalonia v1.0.4
                    
                    // Try to configure dark theme colors via reflection
                    var containerStylesProperty = hostConfig.GetType().GetProperty("ContainerStyles");
                    if (containerStylesProperty != null)
                    {
                        var containerStyles = containerStylesProperty.GetValue(hostConfig);
                        if (containerStyles != null)
                        {
                            // Set default container background to transparent
                            var defaultStyleProperty = containerStyles.GetType().GetProperty("Default");
                            if (defaultStyleProperty != null)
                            {
                                var defaultStyle = defaultStyleProperty.GetValue(containerStyles);
                                if (defaultStyle != null)
                                {
                                    var bgColorProperty = defaultStyle.GetType().GetProperty("BackgroundColor");
                                    bgColorProperty?.SetValue(defaultStyle, "#00000000");
                                }
                            }
                            
                            // Set emphasis container background to slightly visible
                            var emphasisStyleProperty = containerStyles.GetType().GetProperty("Emphasis");
                            if (emphasisStyleProperty != null)
                            {
                                var emphasisStyle = emphasisStyleProperty.GetValue(containerStyles);
                                if (emphasisStyle != null)
                                {
                                    var bgColorProperty = emphasisStyle.GetType().GetProperty("BackgroundColor");
                                    bgColorProperty?.SetValue(emphasisStyle, "#1AFFFFFF");
                                }
                            }
                        }
                    }
                    
                    // Try to set the HostConfig on the AdaptiveCardView
                    var hostConfigProperty = adaptiveCardView.GetType().GetProperty("HostConfig");
                    hostConfigProperty?.SetValue(adaptiveCardView, hostConfig);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error configuring Adaptive Card theme: {ex.Message}");
            }
        }
    }
}
