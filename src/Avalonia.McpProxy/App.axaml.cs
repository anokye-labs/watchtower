using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.McpProxy.ViewModels;
using Avalonia.McpProxy.Views;
using System;

namespace Avalonia.McpProxy;

public class App : Application
{
    private TrayIcon? _trayIcon;
    private LogWindow? _logWindow;
    
    public static ProxyViewModel? ViewModel { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Create view model
            ViewModel = new ProxyViewModel();
            
            // Don't show main window on startup - just tray icon
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            
            // Set up tray icon
            SetupTrayIcon(desktop);
            
            // Start the proxy service
            ViewModel.Start();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void SetupTrayIcon(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var menu = new NativeMenu();
        
        var showLogsItem = new NativeMenuItem("Show Logs");
        showLogsItem.Click += (_, _) => ShowLogWindow();
        menu.Add(showLogsItem);
        
        menu.Add(new NativeMenuItemSeparator());
        
        var reconnectItem = new NativeMenuItem("Reconnect All Apps");
        reconnectItem.Click += (_, _) => ViewModel?.ReconnectAll();
        menu.Add(reconnectItem);
        
        menu.Add(new NativeMenuItemSeparator());
        
        var exitItem = new NativeMenuItem("Exit");
        exitItem.Click += (_, _) =>
        {
            ViewModel?.Stop();
            desktop.Shutdown();
        };
        menu.Add(exitItem);

        _trayIcon = new TrayIcon
        {
            ToolTipText = "MCP Proxy",
            Menu = menu,
            IsVisible = true
        };
        
        _trayIcon.Clicked += (_, _) => ShowLogWindow();
        
        Console.Error.WriteLine("[Proxy] Tray icon initialized");
    }

    private void ShowLogWindow()
    {
        if (_logWindow == null || !_logWindow.IsVisible)
        {
            _logWindow = new LogWindow
            {
                DataContext = ViewModel
            };
            _logWindow.Show();
        }
        else
        {
            _logWindow.Activate();
        }
    }
}
