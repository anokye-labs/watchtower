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
using WatchTower.ViewModels;

namespace WatchTower.Views;

public partial class ShellWindow : Window
{
    private ScrollViewer? _diagnosticsScroller;
    private ShellWindowViewModel? _viewModel;
    private bool _hasAnimated;

    // Animation parameters
    private const int AnimationDurationMs = 800;
    private const int AnimationBufferMs = 100; // Extra buffer after animation completes
    private const double SplashSizeRatio = 0.7; // 70% of screen size
    
    // Fallback dimensions if screen info unavailable
    private const int FallbackWidth = 800;
    private const int FallbackHeight = 600;
    
    // Overlay sizing
    private const int DefaultOverlayHeight = 400;
    private const double OverlayHeightRatio = 0.6;
    
    // Original frame image dimensions (pixels)
    private const double OriginalTopLeftWidth = 1317;
    private const double OriginalTopLeftHeight = 965;
    private const double OriginalTopRightWidth = 1333;
    private const double OriginalTopRightHeight = 965;
    private const double OriginalBottomLeftWidth = 1330;
    private const double OriginalBottomLeftHeight = 966;
    private const double OriginalBottomRightWidth = 1337;
    private const double OriginalBottomRightHeight = 966;
    private const double OriginalTopEdgeWidth = 4262;
    private const double OriginalTopEdgeHeight = 272;
    private const double OriginalBottomEdgeWidth = 4245;
    private const double OriginalBottomEdgeHeight = 129;
    private const double OriginalLeftEdgeWidth = 81;
    private const double OriginalLeftEdgeHeight = 1909;
    private const double OriginalRightEdgeWidth = 66;
    private const double OriginalRightEdgeHeight = 1909;
    
    // Fully assembled frame dimensions
    // Width = left corner + top edge + right corner
    private const double FullFrameWidth = OriginalTopLeftWidth + OriginalTopEdgeWidth + OriginalTopRightWidth; // 6912
    // Height = top corner + left edge + bottom corner  
    private const double FullFrameHeight = OriginalTopLeftHeight + OriginalLeftEdgeHeight + OriginalBottomLeftHeight; // 3840
    
    // Frame image references for setting Width/Height
    private Image? _topLeftCorner;
    private Image? _topRightCorner;
    private Image? _bottomLeftCorner;
    private Image? _bottomRightCorner;
    private Image? _topEdge;
    private Image? _bottomEdge;
    private Image? _leftEdge;
    private Image? _rightEdge;
    
    // Frame grid for setting row/column sizes
    private Grid? _frameGrid;

    public ShellWindow()
    {
        InitializeComponent();
        
        // Subscribe to events
        KeyDown += OnKeyDown;
        Loaded += OnLoaded;
        Closed += OnWindowClosed;
        DataContextChanged += OnDataContextChanged;
        
        // Update frame scale when window size changes
        this.GetObservable(BoundsProperty).Subscribe(_ => UpdateFrameScale());
    }
    
    private void InitializeFrameElements()
    {
        // Get frame images
        _topLeftCorner = this.FindControl<Image>("TopLeftCorner");
        _topRightCorner = this.FindControl<Image>("TopRightCorner");
        _bottomLeftCorner = this.FindControl<Image>("BottomLeftCorner");
        _bottomRightCorner = this.FindControl<Image>("BottomRightCorner");
        _topEdge = this.FindControl<Image>("TopEdge");
        _bottomEdge = this.FindControl<Image>("BottomEdge");
        _leftEdge = this.FindControl<Image>("LeftEdge");
        _rightEdge = this.FindControl<Image>("RightEdge");
        
        // Get frame grid
        _frameGrid = this.FindControl<Grid>("FrameGrid");
    }
    
    /// <summary>
    /// Calculates and applies uniform scale to all frame images based on screen size.
    /// Scale = min(screenWidth / fullFrameWidth, screenHeight / fullFrameHeight)
    /// </summary>
    private void UpdateFrameScale()
    {
        // Initialize frame elements if not done yet
        if (_frameGrid == null)
        {
            InitializeFrameElements();
        }
        
        var screen = Screens.Primary;
        if (screen == null) return;
        
        var screenWidth = screen.WorkingArea.Width;
        var screenHeight = screen.WorkingArea.Height;
        
        // Calculate scale: pick the smaller ratio to fit the frame on screen
        var scaleX = screenWidth / FullFrameWidth;
        var scaleY = screenHeight / FullFrameHeight;
        var scale = Math.Min(scaleX, scaleY);
        
        // Apply uniform scale to all frame elements
        ApplyFrameScale(scale);
    }
    
    /// <summary>
    /// Applies the given scale factor to all frame images by setting explicit Width/Height.
    /// </summary>
    private void ApplyFrameScale(double scale)
    {
        // Set explicit dimensions on corner images
        if (_topLeftCorner != null)
        {
            _topLeftCorner.Width = OriginalTopLeftWidth * scale;
            _topLeftCorner.Height = OriginalTopLeftHeight * scale;
        }
        if (_topRightCorner != null)
        {
            _topRightCorner.Width = OriginalTopRightWidth * scale;
            _topRightCorner.Height = OriginalTopRightHeight * scale;
        }
        if (_bottomLeftCorner != null)
        {
            _bottomLeftCorner.Width = OriginalBottomLeftWidth * scale;
            _bottomLeftCorner.Height = OriginalBottomLeftHeight * scale;
        }
        if (_bottomRightCorner != null)
        {
            _bottomRightCorner.Width = OriginalBottomRightWidth * scale;
            _bottomRightCorner.Height = OriginalBottomRightHeight * scale;
        }
        
        // Set explicit dimensions on edge images
        if (_topEdge != null)
        {
            _topEdge.Width = OriginalTopEdgeWidth * scale;
            _topEdge.Height = OriginalTopEdgeHeight * scale;
        }
        if (_bottomEdge != null)
        {
            _bottomEdge.Width = OriginalBottomEdgeWidth * scale;
            _bottomEdge.Height = OriginalBottomEdgeHeight * scale;
        }
        if (_leftEdge != null)
        {
            _leftEdge.Width = OriginalLeftEdgeWidth * scale;
            _leftEdge.Height = OriginalLeftEdgeHeight * scale;
        }
        if (_rightEdge != null)
        {
            _rightEdge.Width = OriginalRightEdgeWidth * scale;
            _rightEdge.Height = OriginalRightEdgeHeight * scale;
        }
        
        // Set explicit row heights and column widths based on scaled corner sizes
        if (_frameGrid != null)
        {
            // Calculate scaled corner dimensions
            var scaledTopHeight = OriginalTopLeftHeight * scale;
            var scaledBottomHeight = OriginalBottomLeftHeight * scale;
            var scaledLeftWidth = OriginalTopLeftWidth * scale;
            var scaledRightWidth = OriginalTopRightWidth * scale;
            
            // Set row heights
            _frameGrid.RowDefinitions[0].Height = new GridLength(scaledTopHeight);
            _frameGrid.RowDefinitions[2].Height = new GridLength(scaledBottomHeight);
            
            // Set column widths
            _frameGrid.ColumnDefinitions[0].Width = new GridLength(scaledLeftWidth);
            _frameGrid.ColumnDefinitions[2].Width = new GridLength(scaledRightWidth);
        }
    }

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

        // Cleanup ViewModel
        if (_viewModel?.SplashViewModel != null)
        {
            _viewModel.SplashViewModel.DiagnosticMessages.CollectionChanged -= OnDiagnosticMessagesChanged;
            _viewModel.Cleanup();
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
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
        _viewModel?.BeginExpansionAnimation();

        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var screen = Screens.Primary;
            if (screen == null)
            {
                // Fallback: just maximize
                WindowState = WindowState.Maximized;
                _viewModel?.EndExpansionAnimation();
                return;
            }

            var workingArea = screen.WorkingArea;
            var startWidth = Width;
            var startHeight = Height;
            var startPosition = Position;

            var targetWidth = workingArea.Width;
            var targetHeight = workingArea.Height;
            var targetPosition = new PixelPoint(workingArea.X, workingArea.Y);

            // Use manual animation with DispatcherTimer for smooth interpolation
            var startTime = DateTime.UtcNow;
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
            };

            timer.Tick += (s, e) =>
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                var progress = Math.Min(elapsed / AnimationDurationMs, 1.0);
                
                // Apply cubic ease-out
                var easedProgress = CubicEaseOut(progress);

                // Interpolate width and height
                var currentWidth = startWidth + (targetWidth - startWidth) * easedProgress;
                var currentHeight = startHeight + (targetHeight - startHeight) * easedProgress;
                
                // Interpolate position
                var currentX = startPosition.X + (int)((targetPosition.X - startPosition.X) * easedProgress);
                var currentY = startPosition.Y + (int)((targetPosition.Y - startPosition.Y) * easedProgress);

                Width = currentWidth;
                Height = currentHeight;
                Position = new PixelPoint(currentX, currentY);

                if (progress >= 1.0)
                {
                    timer.Stop();
                    
                    // Ensure we're exactly at the target
                    WindowState = WindowState.Maximized;
                    
                    _viewModel?.EndExpansionAnimation();
                }
            };

            timer.Start();

            // Wait for animation to complete
            await Task.Delay(AnimationDurationMs + AnimationBufferMs);
        });
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
