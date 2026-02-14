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

    private async void OnStartAppClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: AppGroupViewModel app } && DataContext is ProxyViewModel vm)
        {
            await vm.StartAppAsync(app.Name);
        }
    }

    private void OnUnregisterAppClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: AppGroupViewModel app } && DataContext is ProxyViewModel vm)
        {
            vm.UnregisterApp(app.Name);
        }
    }

    private async void OnScreenshotInstanceClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ProcessInstanceViewModel instance } && DataContext is ProxyViewModel vm)
        {
            await vm.TakeScreenshotOfInstanceAsync(instance.AppName);
        }
    }

    private void OnStopInstanceClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ProcessInstanceViewModel instance } && DataContext is ProxyViewModel vm)
        {
            vm.StopProcessInstance(instance.Pid, instance.AppName);
        }
    }
}
