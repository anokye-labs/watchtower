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
    private const double SplashSizeRatio = 0.7; // 70% of screen size

    public ShellWindow()
    {
        InitializeComponent();
        
        // Subscribe to events
        KeyDown += OnKeyDown;
        Loaded += OnLoaded;
        Closed += OnWindowClosed;
        DataContextChanged += OnDataContextChanged;
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
            : Math.Min(400, Bounds.Height * 0.6);

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
            Width = 800;
            Height = 600;
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
            await Task.Delay(AnimationDurationMs + 100);
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
}
