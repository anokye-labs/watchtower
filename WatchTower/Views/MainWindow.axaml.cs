using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
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
    private const double SlideDistance = 500.0;
    
    private Border? _overlayPanel;
    private TranslateTransform? _overlayTransform;
    private MainWindowViewModel? _previousViewModel;

    public MainWindow()
    {
        InitializeComponent();
        
        // Subscribe to DataContext changes to wire up property changed events
        DataContextChanged += OnDataContextChanged;
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
        
        // Log warning if animation controls not found
        if (_overlayPanel == null || _overlayTransform == null)
        {
            System.Diagnostics.Debug.WriteLine("Warning: Overlay animation controls not found in XAML");
        }
    }

    private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsOverlayVisible) && sender is MainWindowViewModel vm)
        {
            if (_overlayPanel != null && _overlayTransform != null)
            {
                if (vm.IsOverlayVisible)
                {
                    // Start off-screen at the bottom
                    _overlayTransform.Y = SlideDistance;
                    
                    // Animate slide up
                    var animation = new Animation
                    {
                        Duration = TimeSpan.FromMilliseconds(300),
                        Easing = new CubicEaseOut(),
                        Children =
                        {
                            new KeyFrame
                            {
                                Cue = new Cue(0.0),
                                Setters = { new Setter(TranslateTransform.YProperty, SlideDistance) }
                            },
                            new KeyFrame
                            {
                                Cue = new Cue(1.0),
                                Setters = { new Setter(TranslateTransform.YProperty, 0.0) }
                            }
                        }
                    };
                    
                    await animation.RunAsync(_overlayTransform);
                }
                else
                {
                    // Animate slide down
                    var animation = new Animation
                    {
                        Duration = TimeSpan.FromMilliseconds(300),
                        Easing = new CubicEaseIn(),
                        Children =
                        {
                            new KeyFrame
                            {
                                Cue = new Cue(0.0),
                                Setters = { new Setter(TranslateTransform.YProperty, 0.0) }
                            },
                            new KeyFrame
                            {
                                Cue = new Cue(1.0),
                                Setters = { new Setter(TranslateTransform.YProperty, SlideDistance) }
                            }
                        }
                    };
                    
                    await animation.RunAsync(_overlayTransform);
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
}