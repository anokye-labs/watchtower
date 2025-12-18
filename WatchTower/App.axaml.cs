using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
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
        
        // Register logging service and expose ILoggerFactory
        var loggingService = new LoggingService();
        services.AddSingleton(loggingService);
        services.AddSingleton<ILoggerFactory>(loggingService.LoggerFactory);
        
        // Register ILogger<T> using the factory
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        
        // Register services
        services.AddSingleton<IAdaptiveCardService, AdaptiveCardService>();
        services.AddSingleton<IGameControllerService, GameControllerService>();
        
        // Register ViewModels
        services.AddTransient<MainWindowViewModel>();
        
        // Build service provider
        _serviceProvider = services.BuildServiceProvider();
        
        var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
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
            var viewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new Views.MainWindow
            {
                DataContext = viewModel
            };
            logger.LogInformation("Main window created with ViewModel");
            
            // Dispose service provider when application shuts down
            desktop.ShutdownRequested += (s, e) =>
            {
                _gamepadPollTimer?.Stop();
                _gameControllerService?.Dispose();
                _serviceProvider?.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}