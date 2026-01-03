using Avalonia.McpProxy.Models;
using Avalonia.McpProxy.Server;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Avalonia.McpProxy;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Parse command line arguments
        var stdioMode = args.Contains("--stdio");
        var autoYes = args.Contains("--yes");
        var configPath = GetConfigPath(args);

        // Set up logging
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger<Program>();

        try
        {
            logger.LogInformation("Avalonia MCP Proxy v1.0.0");
            logger.LogInformation("Connecting Avalonia applications to AI agents via MCP");

            // Load configuration
            var config = LoadConfiguration(configPath, logger);

            // Create app registry
            var registryLogger = loggerFactory.CreateLogger<AppRegistry>();
            var registry = new AppRegistry(registryLogger);

            // Create and start proxy server
            var serverLogger = loggerFactory.CreateLogger<ProxyServer>();
            using var server = new ProxyServer(config, registry, serverLogger);

            // Start the server
            using var cts = new CancellationTokenSource();

            // Handle Ctrl+C gracefully
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                logger.LogInformation("Shutdown requested...");
                cts.Cancel();
            };

            await server.StartAsync(cts.Token);

            // Wait for cancellation
            await Task.Delay(-1, cts.Token);

            return 0;
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Proxy server shut down gracefully");
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error in proxy server");
            return 1;
        }
    }

    private static ProxyConfiguration LoadConfiguration(string configPath, ILogger logger)
    {
        try
        {
            if (File.Exists(configPath))
            {
                logger.LogInformation("Loading configuration from: {ConfigPath}", configPath);
                var json = File.ReadAllText(configPath);
                var rootConfig = JsonSerializer.Deserialize<McpProxyConfig>(json);
                return rootConfig?.Proxy ?? new ProxyConfiguration();
            }

            logger.LogWarning("Configuration file not found: {ConfigPath}, using defaults", configPath);
            return new ProxyConfiguration();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading configuration, using defaults");
            return new ProxyConfiguration();
        }
    }

    private static string GetConfigPath(string[] args)
    {
        // Check for --config argument
        var configIndex = Array.IndexOf(args, "--config");
        if (configIndex >= 0 && configIndex < args.Length - 1)
        {
            return args[configIndex + 1];
        }

        // Check environment variable
        var envConfig = Environment.GetEnvironmentVariable("MCP_CONFIG");
        if (!string.IsNullOrEmpty(envConfig))
        {
            return envConfig;
        }

        // Default
        return ".mcpproxy.json";
    }
}
