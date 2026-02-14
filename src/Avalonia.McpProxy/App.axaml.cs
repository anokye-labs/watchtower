using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.McpProxy.Services;
using Avalonia.McpProxy.ViewModels;
using Avalonia.McpProxy.Views;
using Avalonia.Platform;
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
            // Create registry and view model
            var registry = new AppRegistry(msg => Console.Error.WriteLine($"[Registry] {msg}"));
            ViewModel = new ProxyViewModel(registry);
            
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
        
        var showLogsItem = new NativeMenuItem("Show Dashboard");
        showLogsItem.Click += (_, _) => ShowLogWindow();
        menu.Add(showLogsItem);
        
        menu.Add(new NativeMenuItemSeparator());
        
        var exitItem = new NativeMenuItem("Exit");
        exitItem.Click += (_, _) =>
        {
            ViewModel?.Stop();
            desktop.Shutdown();
        };
        menu.Add(exitItem);

        // Load tray icon from embedded asset
        WindowIcon? icon = null;
        try
        {
            var iconUri = new Uri("avares://Avalonia.McpProxy/Assets/proxy-icon.png");
            var asset = AssetLoader.Open(iconUri);
            icon = new WindowIcon(asset);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Proxy] Could not load tray icon: {ex.Message}");
        }

        _trayIcon = new TrayIcon
        {
            ToolTipText = "MCP Proxy",
            Icon = icon,
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
