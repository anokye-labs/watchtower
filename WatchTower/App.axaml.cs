using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using WatchTower.Services;
using WatchTower.ViewModels;

namespace WatchTower;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private IGameControllerService? _gameControllerService;
    private DispatcherTimer? _gamepadPollTimer;

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
        
        // Register configuration
        var loggingService = new LoggingService();
        var configuration = loggingService.GetConfiguration();
        services.AddSingleton<IConfiguration>(configuration);
        
        // Register game controller service
        services.AddSingleton<IGameControllerService>(sp => 
        {
            var logger = loggingService.CreateLogger<GameControllerService>();
            var config = sp.GetRequiredService<IConfiguration>();
            return new GameControllerService(logger, config);
        });
        
        // Register ViewModels
        services.AddTransient<MainWindowViewModel>();
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Initialize services
        var logger = loggingService.CreateLogger<App>();
        logger.LogInformation("Application initialization completed");
        
        // Initialize game controller service
        _gameControllerService = _serviceProvider.GetRequiredService<IGameControllerService>();
        if (_gameControllerService.Initialize())
        {
            logger.LogInformation("Game controller service initialized successfully");
            
            // Start polling timer synchronized with rendering (60 FPS)
            _gamepadPollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
            };
            _gamepadPollTimer.Tick += (s, e) => _gameControllerService.Update();
            _gamepadPollTimer.Start();
            logger.LogInformation("Gamepad polling started at 60 FPS");
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
                _gamepadPollTimer?.Stop();
                _gameControllerService?.Dispose();
                _serviceProvider?.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}