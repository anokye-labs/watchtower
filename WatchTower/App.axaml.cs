using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WatchTower.Services;
using WatchTower.ViewModels;
using System;

namespace WatchTower;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        
        // Register logging service and expose ILoggerFactory
        var loggingService = new LoggingService();
        services.AddSingleton(loggingService);
        services.AddSingleton(loggingService.LoggerFactory);
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        
        // Register services
        services.AddSingleton<IAdaptiveCardService, AdaptiveCardService>();
        
        // Register ViewModels
        services.AddTransient<MainWindowViewModel>();
        
        // Build service provider
        _serviceProvider = services.BuildServiceProvider();
        
        var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
        logger.LogInformation("Application initialization completed");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new Views.MainWindow
            {
                DataContext = viewModel
            };
            logger.LogInformation("Main window created with ViewModel");
            
            // Dispose service provider when application shuts down
            desktop.ShutdownRequested += (s, e) =>
            {
                _serviceProvider?.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}