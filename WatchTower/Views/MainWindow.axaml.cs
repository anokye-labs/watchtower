using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using WatchTower.ViewModels;

namespace WatchTower.Views;

public partial class MainWindow : Window
{
    private const double SlideDistanceY = 500.0;
    
    private Border? _overlayPanel;
    private TranslateTransform? _overlayTransform;
    private Border? _eventLogPanel;
    private TranslateTransform? _eventLogTransform;
    private MainWindowViewModel? _previousViewModel;

    public MainWindow()
    {
        InitializeComponent();
        
        // Subscribe to DataContext changes to wire up property changed events
        DataContextChanged += OnDataContextChanged;
        
        // Subscribe to keyboard events for overlay shortcuts
        KeyDown += OnKeyDown;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Unsubscribe from previous ViewModel to prevent memory leaks
        if (_previousViewModel != null)
        {
            _previousViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _previousViewModel = viewModel;
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _overlayPanel = this.FindControl<Border>("OverlayPanel");
        _overlayTransform = _overlayPanel?.RenderTransform as TranslateTransform;
        _eventLogPanel = this.FindControl<Border>("EventLogPanel");
        _eventLogTransform = _eventLogPanel?.RenderTransform as TranslateTransform;
        
        // Log warning if animation controls not found
        if (_overlayPanel == null || _overlayTransform == null)
        {
            System.Diagnostics.Debug.WriteLine("Warning: Input overlay animation controls not found in XAML");
        }
        if (_eventLogPanel == null || _eventLogTransform == null)
        {
            System.Diagnostics.Debug.WriteLine("Warning: Event log animation controls not found in XAML");
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not MainWindowViewModel vm)
        {
            return;
        }

        // Handle input overlay (Rich Text / Voice) - slides from bottom
        if (e.PropertyName == nameof(MainWindowViewModel.IsInputOverlayVisible))
        {
            // Ensure controls are initialized (happens if property changes before OnLoaded)
            if (_overlayPanel == null || _overlayTransform == null)
            {
                _overlayPanel = this.FindControl<Border>("OverlayPanel");
                _overlayTransform = _overlayPanel?.RenderTransform as TranslateTransform;
            }

            if (_overlayPanel != null && _overlayTransform != null)
            {
                if (vm.IsInputOverlayVisible)
                {
                    // Start off-screen at the bottom
                    _overlayTransform.Y = SlideDistanceY;
                    
                    // Animate slide up using transitions
                    _overlayPanel.Transitions = new Transitions
                    {
                        new DoubleTransition
                        {
                            Property = TranslateTransform.YProperty,
                            Duration = TimeSpan.FromMilliseconds(300),
                            Easing = new CubicEaseOut()
                        }
                    };
                    
                    _overlayTransform.Y = 0.0;
                }
                else
                {
                    // Animate slide down using transitions
                    _overlayPanel.Transitions = new Transitions
                    {
                        new DoubleTransition
                        {
                            Property = TranslateTransform.YProperty,
                            Duration = TimeSpan.FromMilliseconds(300),
                            Easing = new CubicEaseIn()
                        }
                    };
                    
                    _overlayTransform.Y = SlideDistanceY;
                }
            }
        }

        // Handle event log overlay - slides from left
        if (e.PropertyName == nameof(MainWindowViewModel.IsEventLogVisible))
        {
            // Ensure controls are initialized
            if (_eventLogPanel == null || _eventLogTransform == null)
            {
                _eventLogPanel = this.FindControl<Border>("EventLogPanel");
                _eventLogTransform = _eventLogPanel?.RenderTransform as TranslateTransform;
            }

            if (_eventLogPanel != null && _eventLogTransform != null)
            {
                // Use actual panel width (half window width) for slide distance
                var slideDistance = _eventLogPanel.Bounds.Width > 0 ? _eventLogPanel.Bounds.Width : Bounds.Width / 2;
                
                if (vm.IsEventLogVisible)
                {
                    // Start off-screen to the left
                    _eventLogTransform.X = -slideDistance;
                    
                    // Animate slide in from left
                    _eventLogPanel.Transitions = new Transitions
                    {
                        new DoubleTransition
                        {
                            Property = TranslateTransform.XProperty,
                            Duration = TimeSpan.FromMilliseconds(300),
                            Easing = new CubicEaseOut()
                        }
                    };
                    
                    _eventLogTransform.X = 0.0;
                }
                else
                {
                    // Animate slide out to the left
                    _eventLogPanel.Transitions = new Transitions
                    {
                        new DoubleTransition
                        {
                            Property = TranslateTransform.XProperty,
                            Duration = TimeSpan.FromMilliseconds(300),
                            Easing = new CubicEaseIn()
                        }
                    };
                    
                    _eventLogTransform.X = -slideDistance;
                }
            }
        }
    }

    private void OnBackdropTapped(object? sender, RoutedEventArgs e)
    {
        // Close overlay when backdrop is tapped
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.CloseOverlayCommand.Execute(null);
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

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
}