using System;
using System.Threading.Tasks;
using Avalonia.Mcp.Core.Extensions;
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
        const int TotalSteps = 10;
        int currentStep = 0;

        try
        {
            logger.Info("=== Application Startup ===");
            logger.Info($"Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
            logger.Info($"Platform: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
            
            // Step 1: Initialize environment
            logger.ReportProgress(++currentStep, TotalSteps, "Initializing environment");
            await Task.Delay(100); // Small delay for visual feedback
            
            // Step 2: Load configuration
            logger.ReportProgress(++currentStep, TotalSteps, "Loading configuration");
            logger.Info("Configuration loaded from appsettings.json");
            
            // Step 3: Setup dependency injection
            logger.ReportProgress(++currentStep, TotalSteps, "Configuring dependency injection");
            var services = new ServiceCollection();
            
            // Register the shared configuration
            services.AddSingleton(configuration);
            
            // Step 4: Register logging services
            logger.ReportProgress(++currentStep, TotalSteps, "Registering logging services");
            var loggingService = new LoggingService();
            services.AddSingleton(loggingService);
            services.AddSingleton<ILoggerFactory>(loggingService.LoggerFactory);
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            
            // Step 5: Register core services
            logger.ReportProgress(++currentStep, TotalSteps, "Registering core services");
            services.AddSingleton<IUserPreferencesService, UserPreferencesService>();
            services.AddSingleton<IAdaptiveCardThemeService, AdaptiveCardThemeService>();
            services.AddSingleton<IAdaptiveCardService, AdaptiveCardService>();
            services.AddSingleton<IGameControllerService, GameControllerService>();
            logger.Info("Core services registered (UserPreferences, AdaptiveCard, GameController)");
            
            // Step 6: Register MCP handler
            logger.ReportProgress(++currentStep, TotalSteps, "Registering MCP handler");
            services.AddMcpHandler(config =>
            {
                config.ApplicationName = "WatchTower";
                config.ProxyEndpoint = "tcp://localhost:5100";
                config.AutoConnect = true;
                config.HeadlessMode = false;
            }, registerStandardTools: true);
            logger.Info("MCP handler registered");
            
            // Step 7: Register voice services
            logger.ReportProgress(++currentStep, TotalSteps, "Registering voice services");
            var voiceMode = configuration.GetValue<string>("Voice:Mode") ?? "offline";
            logger.Info($"Voice mode: {voiceMode}");
            
            if (voiceMode.Equals("offline", StringComparison.OrdinalIgnoreCase))
            {
                services.AddSingleton<IVoiceRecognitionService, VoskRecognitionService>();
                services.AddSingleton<ITextToSpeechService, PiperTextToSpeechService>();
                logger.Info("Offline voice services registered (Vosk + Piper)");
            }
            else if (voiceMode.Equals("online", StringComparison.OrdinalIgnoreCase))
            {
                services.AddSingleton<IVoiceRecognitionService, AzureSpeechRecognitionService>();
                services.AddSingleton<ITextToSpeechService, AzureSpeechSynthesisService>();
                logger.Info("Online voice services registered (Azure Speech)");
            }
            else if (voiceMode.Equals("hybrid", StringComparison.OrdinalIgnoreCase))
            {
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
            
            services.AddSingleton<IVoiceOrchestrationService, VoiceOrchestrationService>();
            logger.Info("VoiceOrchestrationService registered");
            
            // Register ViewModels
            services.AddTransient<ViewModels.MainWindowViewModel>();
            services.AddTransient<ViewModels.VoiceControlViewModel>();
            services.AddTransient<ViewModels.WelcomeContentViewModel>();
            logger.Info("ViewModels registered");
            
            // Step 8: Build service provider
            logger.ReportProgress(++currentStep, TotalSteps, "Building service provider");
            var serviceProvider = services.BuildServiceProvider();
            logger.Info("Service provider built successfully");
            
            // Step 9: Initialize game controller
            logger.ReportProgress(++currentStep, TotalSteps, "Initializing game controller");
            var msLogger = serviceProvider.GetRequiredService<ILogger<StartupOrchestrator>>();
            msLogger.LogInformation("Service provider initialized");
            
            var gameControllerService = serviceProvider.GetRequiredService<IGameControllerService>();
            if (gameControllerService.Initialize())
            {
                logger.Info("Game controller service initialized successfully");
            }
            else
            {
                logger.Warn("Game controller service initialization failed");
            }
            
            // Step 10: Initialize voice services
            logger.ReportProgress(++currentStep, TotalSteps, "Initializing voice services");
            var voiceService = serviceProvider.GetRequiredService<IVoiceOrchestrationService>();
            
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
