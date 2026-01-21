using Avalonia.Mcp.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace WatchTower.Services;

/// <summary>
/// Orchestrates the application startup workflow with clear phases.
/// Reports progress to the splash screen via IStartupLogger.
/// </summary>
[SupportedOSPlatform("windows5.1.2600")]
public class StartupOrchestrator : IStartupOrchestrator
{
    public Task<ServiceProvider?> ExecuteStartupAsync(IStartupLogger logger, IConfiguration configuration)
    {
        return ExecuteStartupAsync(logger, configuration, null);
    }

    /// <summary>
    /// Executes the startup workflow asynchronously.
    /// </summary>
    /// <param name="logger">Logger for reporting progress and errors.</param>
    /// <param name="configuration">Application configuration to use.</param>
    /// <param name="userPreferencesService">Optional pre-created user preferences service (e.g., for early window positioning).</param>
    /// <returns>The configured ServiceProvider on success, null on failure.</returns>
    public async Task<ServiceProvider?> ExecuteStartupAsync(
        IStartupLogger logger,
        IConfiguration configuration,
        IUserPreferencesService? userPreferencesService)
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

            // Register core services
            if (userPreferencesService != null)
            {
                // Use the pre-created instance (e.g., for early window positioning)
                services.AddSingleton<IUserPreferencesService>(userPreferencesService);
            }
            else
            {
                services.AddSingleton<IUserPreferencesService, UserPreferencesService>();
            }
            services.AddSingleton<ICredentialStorageService, CredentialStorageService>();
            services.AddSingleton<IAdaptiveCardThemeService, AdaptiveCardThemeService>();
            services.AddSingleton<IAdaptiveCardService, AdaptiveCardService>();
            services.AddSingleton<IGameControllerService, GameControllerService>();
            services.AddSingleton<IBuildCacheService, BuildCacheService>();

            logger.Info("UserPreferencesService registered");
            logger.Info("CredentialStorageService registered");
            logger.Info("AdaptiveCardThemeService registered");
            logger.Info("AdaptiveCardService registered");
            logger.Info("GameControllerService registered");
            logger.Info("BuildCacheService registered");

            // Register MCP handler
            services.AddMcpHandler(config =>
            {
                config.ApplicationName = "WatchTower";
                config.ProxyEndpoint = "tcp://localhost:5100";
                config.AutoConnect = true;
                config.HeadlessMode = false;
            }, registerStandardTools: true);

            logger.Info("MCP handler registered");

            // Register voice services based on configured mode
            var voiceMode = configuration.GetValue<string>("Voice:Mode") ?? "offline";
            logger.Info($"Voice mode: {voiceMode}");

            if (voiceMode.Equals("offline", StringComparison.OrdinalIgnoreCase))
            {
                // Register offline voice services
                services.AddSingleton<IVoiceRecognitionService, VoskRecognitionService>();
                services.AddSingleton<ITextToSpeechService, PiperTextToSpeechService>();
                logger.Info("Offline voice services registered (Vosk + Piper)");
            }
            else if (voiceMode.Equals("online", StringComparison.OrdinalIgnoreCase))
            {
                // Register online voice services
                services.AddSingleton<IVoiceRecognitionService, AzureSpeechRecognitionService>();
                services.AddSingleton<ITextToSpeechService, AzureSpeechSynthesisService>();
                logger.Info("Online voice services registered (Azure Speech)");
            }
            else if (voiceMode.Equals("hybrid", StringComparison.OrdinalIgnoreCase))
            {
                // Hybrid mode is not yet implemented; falls back to offline behavior
                logger.Warn("Hybrid voice mode is not fully implemented; defaulting to offline voice services");
                services.AddSingleton<IVoiceRecognitionService, VoskRecognitionService>();
                services.AddSingleton<ITextToSpeechService, PiperTextToSpeechService>();
                logger.Info("Offline voice services registered (Vosk + Piper) for requested hybrid mode");
            }
            else
            {
                logger.Warn($"Unknown voice mode '{voiceMode}', defaulting to offline");
                services.AddSingleton<IVoiceRecognitionService, VoskRecognitionService>();
                services.AddSingleton<ITextToSpeechService, PiperTextToSpeechService>();
            }

            // Register voice orchestration service
            services.AddSingleton<IVoiceOrchestrationService, VoiceOrchestrationService>();
            logger.Info("VoiceOrchestrationService registered");

            // Register ViewModels
            services.AddTransient<ViewModels.MainWindowViewModel>();
            services.AddTransient<ViewModels.VoiceControlViewModel>();
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

            // Initialize voice orchestration service
            var voiceService = serviceProvider.GetRequiredService<IVoiceOrchestrationService>();
            logger.Info("Initializing voice orchestration service...");

            if (await voiceService.InitializeAsync())
            {
                logger.Info("Voice orchestration service initialized successfully");
            }
            else
            {
                logger.Warn("Voice orchestration service initialization failed");
                logger.Warn("Voice features may not be available");
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
