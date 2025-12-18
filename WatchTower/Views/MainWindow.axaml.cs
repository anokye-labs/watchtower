using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.Logging;
using WatchTower.ViewModels;

namespace WatchTower.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
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