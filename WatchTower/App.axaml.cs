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

            // Validate hang threshold is within reasonable bounds
            if (hangThreshold < 5 || hangThreshold > 300)
            {
                throw new InvalidOperationException(
                    $"Startup:HangThresholdSeconds must be between 5 and 300 seconds. Current value: {hangThreshold}");
            }

            // Create early UserPreferencesService for window position restoration
            // Use NullLogger to avoid creating a separate LoggingService instance before DI container
            var userPreferencesService = new UserPreferencesService(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<UserPreferencesService>.Instance);

            // Create and show shell window immediately with splash content
            var splashViewModel = new SplashWindowViewModel(hangThreshold);
            var shellViewModel = new ShellWindowViewModel(splashViewModel);
            var shellWindow = new Views.ShellWindow
            {
                DataContext = shellViewModel
            };

            // Pass configuration to shell window for frame settings
            shellWindow.SetConfiguration(configuration);

            // Pass user preferences service to shell window for position restoration
            shellWindow.SetUserPreferencesService(userPreferencesService);

            // Handle exit request from splash
            EventHandler? exitHandler = null;
            exitHandler = (s, e) =>
            {
                desktop.Shutdown();
            };
            shellViewModel.ExitRequested += exitHandler;

            desktop.MainWindow = shellWindow;
            shellWindow.Show();

            // Start async startup workflow
            Task.Run(async () =>
            {
                try
                {
                    await ExecuteStartupAsync(shellViewModel, desktop, shellWindow, configuration, userPreferencesService);
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
                shellViewModel.ExitRequested -= exitHandler;
                shellViewModel?.Cleanup();
                _gamepadPollTimer?.Stop();
                _gameControllerService?.Dispose();
                _serviceProvider?.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task ExecuteStartupAsync(
        ShellWindowViewModel shellViewModel,
        IClassicDesktopStyleApplicationLifetime desktop,
        Views.ShellWindow shellWindow,
        IConfiguration configuration,
        IUserPreferencesService userPreferencesService)
    {
        var splashViewModel = shellViewModel.SplashViewModel;

        try
        {
            // Execute startup workflow with shared configuration
            var orchestrator = new StartupOrchestrator();
            _serviceProvider = await orchestrator.ExecuteStartupAsync(splashViewModel, configuration, userPreferencesService);

            if (_serviceProvider == null)
            {
                // Startup failed, splash will remain open with diagnostics
                splashViewModel.MarkStartupFailed();
                return;
            }

            // Get logger from service provider
            var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
            logger.LogInformation("Application initialization completed");

            // Initialize MCP handler (triggers auto-connect if configured)
            var mcpHandler = _serviceProvider.GetService<Avalonia.Mcp.Core.Handlers.IMcpHandler>();
            if (mcpHandler != null)
            {
                logger.LogInformation("MCP handler initialized, auto-connect will start in background");
            }

            // Start game controller service polling (service already initialized in StartupOrchestrator)
            _gameControllerService = _serviceProvider.GetRequiredService<IGameControllerService>();

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

            // Mark startup as complete
            splashViewModel.MarkStartupComplete();

            // Small delay to show success state
            await Task.Delay(500);

            // Animate window expansion from splash size to full-screen
            await shellWindow.AnimateExpansionAsync();
            logger.LogInformation("Shell window expansion animation completed");

            // Transition to main content on UI thread
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var mainViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
                shellViewModel.TransitionToMainContent(mainViewModel);
                logger.LogInformation("Transitioned to main content");
            });
        }
        catch (Exception ex)
        {
            splashViewModel.Error("Startup workflow failed", ex);
            splashViewModel.MarkStartupFailed();
        }
    }
}
