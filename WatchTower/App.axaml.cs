using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
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
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Load configuration for splash screen settings
            var configuration = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var hangThreshold = configuration.GetValue("Startup:HangThresholdSeconds", 30);

            // Create and show splash window immediately
            var splashViewModel = new SplashWindowViewModel(hangThreshold);
            var splashWindow = new Views.SplashWindow
            {
                DataContext = splashViewModel
            };

            // Handle exit request from splash
            splashViewModel.ExitRequested += (s, e) =>
            {
                desktop.Shutdown();
            };

            desktop.MainWindow = splashWindow;
            splashWindow.Show();

            // Start async startup workflow
            Task.Run(async () =>
            {
                try
                {
                    await ExecuteStartupAsync(splashViewModel, desktop, splashWindow);
                }
                catch (Exception ex)
                {
                    // Last resort error handling
                    splashViewModel.Error("Unhandled startup exception", ex);
                    splashViewModel.MarkStartupFailed();
                }
            });

            // Cleanup on shutdown
            desktop.ShutdownRequested += (s, e) =>
            {
                _gamepadPollTimer?.Stop();
                _gameControllerService?.Dispose();
                _serviceProvider?.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task ExecuteStartupAsync(
        SplashWindowViewModel splashViewModel,
        IClassicDesktopStyleApplicationLifetime desktop,
        Views.SplashWindow splashWindow)
    {
        try
        {
            // Execute startup workflow
            var orchestrator = new StartupOrchestrator();
            _serviceProvider = await orchestrator.ExecuteStartupAsync(splashViewModel);

            if (_serviceProvider == null)
            {
                // Startup failed, splash will remain open with diagnostics
                splashViewModel.MarkStartupFailed();
                return;
            }

            // Get logger from service provider
            var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
            logger.LogInformation("Application initialization completed");

            // Initialize game controller service polling
            _gameControllerService = _serviceProvider.GetRequiredService<IGameControllerService>();
            if (_gameControllerService.Initialize())
            {
                logger.LogInformation("Game controller service initialized successfully");
                
                // Start polling timer synchronized with rendering (60 FPS)
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _gamepadPollTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
                    };
                    _gamepadPollTimer.Tick += (s, e) => _gameControllerService.Update();
                    _gamepadPollTimer.Start();
                    logger.LogInformation("Gamepad polling started at 60 FPS");
                });
            }
            else
            {
                logger.LogWarning("Game controller service initialization failed");
            }

            // Mark startup as complete
            splashViewModel.MarkStartupComplete();

            // Small delay to show success state
            await Task.Delay(500);

            // Create and show main window on UI thread
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var mainViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
                var mainWindow = new Views.MainWindow
                {
                    DataContext = mainViewModel
                };

                desktop.MainWindow = mainWindow;
                mainWindow.Show();
                logger.LogInformation("Main window created and shown");

                // Close splash window
                splashWindow.Close();
            });
        }
        catch (Exception ex)
        {
            splashViewModel.Error("Startup workflow failed", ex);
            splashViewModel.MarkStartupFailed();
        }
    }
}