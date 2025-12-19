using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using WatchTower.ViewModels;

namespace WatchTower.Views;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
        
        // Subscribe to keyboard events
        KeyDown += OnKeyDown;
        
        // Cleanup when window closes
        Closed += OnWindowClosed;
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        // Unsubscribe from events
        KeyDown -= OnKeyDown;
        Closed -= OnWindowClosed;

        // Cleanup ViewModel timer
        if (DataContext is SplashWindowViewModel viewModel)
        {
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
