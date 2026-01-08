using Avalonia.Mcp.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Avalonia.McpProxy.Server;

/// <summary>
/// Registry for managing connected applications and their tools.
/// </summary>
public class AppRegistry
{
    private readonly ConcurrentDictionary<string, Models.RegisteredApp> _apps = new();
    private readonly ILogger<AppRegistry> _logger;

    public AppRegistry(ILogger<AppRegistry> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers an application with the proxy.
    /// </summary>
    public bool RegisterApp(string connectionId, string appName, List<McpToolDefinition>? tools = null)
    {
        try
        {
            var app = new Models.RegisteredApp
            {
                Name = appName,
                ConnectionId = connectionId,
                Tools = tools ?? new(),
                RegisteredAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                IsConnected = true
            };

            if (_apps.TryAdd(connectionId, app))
            {
                _logger.LogInformation("Registered app '{AppName}' with {ToolCount} tools (connection: {ConnectionId})",
                    appName, app.Tools.Count, connectionId);
                return true;
            }

            _logger.LogWarning("Failed to register app '{AppName}' - connection ID already exists", appName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering app '{AppName}'", appName);
            return false;
        }
    }

    /// <summary>
    /// Unregisters an application.
    /// </summary>
    public bool UnregisterApp(string connectionId)
    {
        if (_apps.TryRemove(connectionId, out var app))
        {
            _logger.LogInformation("Unregistered app '{AppName}' (connection: {ConnectionId})",
                app.Name, connectionId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets all registered applications.
    /// </summary>
    public IReadOnlyList<Models.RegisteredApp> GetAllApps()
    {
        return _apps.Values.ToList();
    }

    /// <summary>
    /// Gets a specific app by connection ID.
    /// </summary>
    public Models.RegisteredApp? GetApp(string connectionId)
    {
        _apps.TryGetValue(connectionId, out var app);
        return app;
    }

    /// <summary>
    /// Gets all tools from all registered applications.
    /// </summary>
    public IReadOnlyList<McpToolDefinition> GetAllTools()
    {
        return _apps.Values
            .Where(a => a.IsConnected)
            .SelectMany(a => a.Tools)
            .ToList();
    }

    /// <summary>
    /// Finds which app owns a specific tool.
    /// </summary>
    public Models.RegisteredApp? FindAppByTool(string toolName)
    {
        return _apps.Values
            .FirstOrDefault(a => a.IsConnected && a.Tools.Any(t => t.Name == toolName));
    }

    /// <summary>
    /// Updates the last activity timestamp for an app.
    /// </summary>
    public void UpdateActivity(string connectionId)
    {
        if (_apps.TryGetValue(connectionId, out var app))
        {
            app.LastActivityAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Marks an app as disconnected.
    /// </summary>
    public void MarkDisconnected(string connectionId)
    {
        if (_apps.TryGetValue(connectionId, out var app))
        {
            app.IsConnected = false;
            _logger.LogInformation("Marked app '{AppName}' as disconnected", app.Name);
        }
    }
}
