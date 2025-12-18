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
    private Border? _overlayPanel;
    private TranslateTransform? _overlayTransform;
    private Border? _eventLogPanel;
    private TranslateTransform? _eventLogTransform;
    private MainWindowViewModel? _previousViewModel;
    
    // Reusable transitions for better performance
    private Transitions? _overlayTransitions;
    private Transitions? _eventLogTransitions;

    public MainWindow()
    {
        InitializeComponent();
        
        // Subscribe to DataContext changes to wire up property changed events
        DataContextChanged += OnDataContextChanged;
        
        // Subscribe to keyboard events for overlay shortcuts
        KeyDown += OnKeyDown;
        
        // Cleanup subscriptions when the window is closed
        Closed += OnWindowClosed;
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        // Unsubscribe from window-level events to avoid potential memory leaks
        DataContextChanged -= OnDataContextChanged;
        KeyDown -= OnKeyDown;
        Closed -= OnWindowClosed;

        // Ensure we detach from the last ViewModel as well
        if (_previousViewModel != null)
        {
            _previousViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _previousViewModel = null;
        }
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
                // Calculate slide distance based on actual panel height or window height
                var slideDistance = _overlayPanel.Bounds.Height > 0 
                    ? _overlayPanel.Bounds.Height 
                    : Math.Min(400, Bounds.Height * 0.6); // Fallback to max 400px or 60% of window height
                
                // Initialize transitions once if not already created
                if (_overlayTransitions == null)
                {
                    _overlayTransitions = new Transitions
                    {
                        new DoubleTransition
                        {
                            Property = TranslateTransform.YProperty,
                            Duration = TimeSpan.FromMilliseconds(300),
                            Easing = new CubicEaseOut()
                        }
                    };
                    _overlayPanel.Transitions = _overlayTransitions;
                }
                
                if (vm.IsInputOverlayVisible)
                {
                    // Start off-screen at the bottom
                    _overlayTransform.Y = slideDistance;
                    
                    // Update easing for slide up
                    if (_overlayTransitions.Count > 0 && _overlayTransitions[0] is DoubleTransition transition)
                    {
                        transition.Easing = new CubicEaseOut();
                    }
                    
                    _overlayTransform.Y = 0.0;
                    
                    // Auto-focus the TextBox when rich text mode is shown
                    if (vm.IsRichTextMode)
                    {
                        var inputTextBox = this.FindControl<TextBox>("InputTextBox");
                        inputTextBox?.Focus();
                    }
                }
                else
                {
                    // Update easing for slide down
                    if (_overlayTransitions.Count > 0 && _overlayTransitions[0] is DoubleTransition transition)
                    {
                        transition.Easing = new CubicEaseIn();
                    }
                    
                    _overlayTransform.Y = slideDistance;
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
                
                // Initialize transitions once if not already created
                if (_eventLogTransitions == null)
                {
                    _eventLogTransitions = new Transitions
                    {
                        new DoubleTransition
                        {
                            Property = TranslateTransform.XProperty,
                            Duration = TimeSpan.FromMilliseconds(300),
                            Easing = new CubicEaseOut()
                        }
                    };
                    _eventLogPanel.Transitions = _eventLogTransitions;
                }
                
                if (vm.IsEventLogVisible)
                {
                    // Start off-screen to the left
                    _eventLogTransform.X = -slideDistance;
                    
                    // Update easing for slide in
                    if (_eventLogTransitions.Count > 0 && _eventLogTransitions[0] is DoubleTransition transition)
                    {
                        transition.Easing = new CubicEaseOut();
                    }
                    
                    _eventLogTransform.X = 0.0;
                }
                else
                {
                    // Update easing for slide out
                    if (_eventLogTransitions.Count > 0 && _eventLogTransitions[0] is DoubleTransition transition)
                    {
                        transition.Easing = new CubicEaseIn();
                    }
                    
                    _eventLogTransform.X = -slideDistance;
                }
            }
        }
    }

    private void OnBackdropTapped(object? sender, RoutedEventArgs e)
    {
        // Code-behind is used here for simplicity, as the backdrop is a Border element
        // and we need to handle the Tapped event. Using a behavior or ICommand binding
        // would add unnecessary complexity for this straightforward interaction pattern.
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