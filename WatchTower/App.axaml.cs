using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Logging;
using WatchTower.Services;

namespace WatchTower;

public partial class App : Application
{
    private LoggingService? _loggingService;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Initialize logging service
        _loggingService = new LoggingService();
        var logger = _loggingService.CreateLogger<App>();
        logger.LogInformation("Application initialization completed");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            logger.LogInformation("Main window created");
        }

        base.OnFrameworkInitializationCompleted();
    }
}