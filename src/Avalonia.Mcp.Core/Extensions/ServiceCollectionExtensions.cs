using Avalonia.Mcp.Core.Handlers;
using Avalonia.Mcp.Core.Models;
using Avalonia.Mcp.Core.Services;
using Avalonia.Mcp.Core.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Avalonia.Mcp.Core.Extensions;

/// <summary>
/// Extension methods for registering MCP services with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds MCP handler services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The MCP handler configuration.</param>
    /// <param name="registerStandardTools">Whether to register standard UI interaction tools.</param>
    public static IServiceCollection AddMcpHandler(
        this IServiceCollection services,
        McpHandlerConfiguration configuration,
        bool registerStandardTools = true)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // Register configuration
        services.AddSingleton(configuration);

        // Register Avalonia UI service
        services.AddSingleton<IAvaloniaUiService, AvaloniaUiService>();

        // Register handler
        services.AddSingleton<IMcpHandler>(sp =>
        {
            var logger = sp.GetService<ILogger<McpHandler>>();
            var handler = new McpHandler(configuration, logger);

            // Register standard tools if requested
            if (registerStandardTools)
            {
                var uiService = sp.GetRequiredService<IAvaloniaUiService>();
                var toolsLogger = sp.GetService<ILogger<StandardUiTools>>();
                var standardTools = new StandardUiTools(handler, uiService, toolsLogger);
                standardTools.RegisterTools();
            }

            // Auto-connect if configured
            if (configuration.AutoConnect)
            {
                Console.WriteLine($"[MCP] Auto-connect enabled, will connect to {configuration.ProxyEndpoint}");
                _ = Task.Run(async () =>
                {
                    try
                    {
                        Console.WriteLine("[MCP] Auto-connect task starting, waiting 1 second...");
                        await Task.Delay(1000); // Small delay to let app initialize
                        Console.WriteLine("[MCP] Calling ConnectAsync...");
                        var result = await handler.ConnectAsync();
                        Console.WriteLine($"[MCP] ConnectAsync returned: {result}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[MCP] Auto-connect failed: {ex.Message}");
                    }
                });
            }

            return handler;
        });

        return services;
    }

    /// <summary>
    /// Adds MCP handler services with a configuration builder.
    /// </summary>
    public static IServiceCollection AddMcpHandler(
        this IServiceCollection services,
        Action<McpHandlerConfiguration> configureOptions,
        bool registerStandardTools = true)
    {
        var configuration = new McpHandlerConfiguration
        {
            ApplicationName = "AvaloniaApp",
            ProxyEndpoint = "tcp://localhost:5100"
        };

        configureOptions?.Invoke(configuration);

        return services.AddMcpHandler(configuration, registerStandardTools);
    }
}
