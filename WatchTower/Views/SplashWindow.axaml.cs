using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Collections.Specialized;
using WatchTower.ViewModels;

namespace WatchTower.Views;

public partial class SplashWindow : UserControl
{
    private ScrollViewer? _diagnosticsScroller;

    public SplashWindow()
    {
        InitializeComponent();
        
        // Subscribe to keyboard events
        KeyDown += OnKeyDown;
        
        // Cleanup when control is unloaded
        Unloaded += OnUnloaded;

        // Setup auto-scroll for diagnostics
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _diagnosticsScroller = this.FindControl<ScrollViewer>("DiagnosticsScroller");

        if (DataContext is SplashWindowViewModel viewModel)
        {
            viewModel.DiagnosticMessages.CollectionChanged += OnDiagnosticMessagesChanged;
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

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        // Unsubscribe from events
        KeyDown -= OnKeyDown;
        Unloaded -= OnUnloaded;
        Loaded -= OnLoaded;

        // Cleanup ViewModel timer and collection change handler
        if (DataContext is SplashWindowViewModel viewModel)
        {
            viewModel.DiagnosticMessages.CollectionChanged -= OnDiagnosticMessagesChanged;
            viewModel.Cleanup();
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not SplashWindowViewModel viewModel)
        {
            return;
        }

        // D key toggles diagnostics
        if (e.Key == Key.D)
        {
            viewModel.ToggleDiagnosticsCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // Escape key exits
        if (e.Key == Key.Escape)
        {
            viewModel.ExitCommand.Execute(null);
            e.Handled = true;
        }
    }
}
