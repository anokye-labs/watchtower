using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.McpProxy.ViewModels;

namespace Avalonia.McpProxy.Views;

public partial class LogWindow : Window
{
    public LogWindow()
    {
        InitializeComponent();
    }

    private async void OnScreenshotAppClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ConnectedAppInfo app } && DataContext is ProxyViewModel vm)
        {
            await vm.TakeScreenshotOfAppAsync(app);
        }
    }

    private async void OnStopAppClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ConnectedAppInfo app } && DataContext is ProxyViewModel vm)
        {
            await vm.StopAppAsync(app);
        }
    }
}
