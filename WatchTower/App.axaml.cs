using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WatchTower.Services;
using WatchTower.ViewModels;

namespace WatchTower;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private IGameControllerService? _gameControllerService;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        
        // Register logging service
        services.AddSingleton<LoggingService>();
        services.AddSingleton<ILogger>(sp => 
            sp.GetRequiredService<LoggingService>().CreateLogger<App>());
        
        // Register game controller service
        services.AddSingleton<IGameControllerService, GameControllerService>();
        services.AddSingleton(sp => 
            sp.GetRequiredService<LoggingService>().CreateLogger<GameControllerService>());
        
        // Register ViewModels
        services.AddTransient<MainWindowViewModel>();
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Initialize services
        var logger = _serviceProvider.GetRequiredService<ILogger>();
        logger.LogInformation("Application initialization completed");
        
        // Initialize game controller service
        _gameControllerService = _serviceProvider.GetRequiredService<IGameControllerService>();
        if (_gameControllerService.Initialize())
        {
            logger.LogInformation("Game controller service initialized successfully");
        }
        else
        {
            logger.LogWarning("Game controller service initialization failed");
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new Views.MainWindow
            {
                DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>()
            };
            desktop.MainWindow = mainWindow;
            logger.LogInformation("Main window created with game controller support");
            
            // Cleanup on exit
            desktop.Exit += (s, e) =>
            {
                _gameControllerService?.Dispose();
                _serviceProvider?.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}