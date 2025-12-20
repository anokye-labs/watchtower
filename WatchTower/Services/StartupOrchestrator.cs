using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WatchTower.Services;

/// <summary>
/// Orchestrates the application startup workflow with clear phases.
/// Reports progress to the splash screen via IStartupLogger.
/// </summary>
public class StartupOrchestrator : IStartupOrchestrator
{
    public async Task<ServiceProvider?> ExecuteStartupAsync(IStartupLogger logger, IConfiguration configuration)
    {
        try
        {
            logger.Info("=== Application Startup ===");
            logger.Info($"Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
            logger.Info($"Platform: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
            
            // Phase 1: Initial preparation
            logger.Info("Phase 1/4: Preparing services...");
            logger.Info("Initial preparation complete");
            
            // Phase 2: Dependency Injection Setup
            logger.Info("Phase 2/4: Configuring dependency injection...");
            var services = new ServiceCollection();
            
            // Register the shared configuration
            services.AddSingleton(configuration);
            
            // Register logging service (it will use the shared configuration)
            var loggingService = new LoggingService();
            services.AddSingleton(loggingService);
            services.AddSingleton<ILoggerFactory>(loggingService.LoggerFactory);
            
            // Register ILogger<T> using the factory
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            
            logger.Info("Logging services registered");
            
            // Phase 3: Services Registration
            logger.Info("Phase 3/4: Registering application services...");
            
            // Register services
            services.AddSingleton<IAdaptiveCardService, AdaptiveCardService>();
            services.AddSingleton<IGameControllerService, GameControllerService>();
            
            logger.Info("AdaptiveCardService registered");
            logger.Info("GameControllerService registered");
            
            // Register ViewModels
            services.AddTransient<ViewModels.MainWindowViewModel>();
            logger.Info("ViewModels registered");
            
            // Build service provider
            var serviceProvider = services.BuildServiceProvider();
            logger.Info("Service provider built successfully");
            
            // Phase 4: Service Initialization
            logger.Info("Phase 4/4: Initializing services...");
            
            var msLogger = serviceProvider.GetRequiredService<ILogger<StartupOrchestrator>>();
            msLogger.LogInformation("Service provider initialized");
            
            // Initialize game controller service
            var gameControllerService = serviceProvider.GetRequiredService<IGameControllerService>();
            logger.Info("Initializing game controller service...");
            
            if (gameControllerService.Initialize())
            {
                logger.Info("Game controller service initialized successfully");
            }
            else
            {
                logger.Warn("Game controller service initialization failed");
            }
            
            logger.Info("=== Startup Complete ===");
            return serviceProvider;
        }
        catch (Exception ex)
        {
            logger.Error("Startup failed", ex);
            return null;
        }
    }
}
