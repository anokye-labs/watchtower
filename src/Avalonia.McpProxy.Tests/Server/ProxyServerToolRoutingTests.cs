using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Avalonia.Mcp.Core.Models;
using Avalonia.McpProxy.Models;
using Avalonia.McpProxy.Server;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Avalonia.McpProxy.Tests.Server;

/// <summary>
/// Tests for tool execution routing in ProxyServer.
/// </summary>
public class ProxyServerToolRoutingTests : IDisposable
{
    private readonly ILogger<ProxyServer> _proxyLogger;
    private readonly ILogger<AppRegistry> _registryLogger;
    private readonly ProxyConfiguration _config;
    private readonly AppRegistry _registry;
    private ProxyServer? _proxyServer;

    public ProxyServerToolRoutingTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _proxyLogger = loggerFactory.CreateLogger<ProxyServer>();
        _registryLogger = loggerFactory.CreateLogger<AppRegistry>();
        
        _config = new ProxyConfiguration
        {
            BindAddress = "localhost:0", // Use port 0 for dynamic port allocation
            MaxConnections = 10,
            Apps = new List<AppConfiguration>()
        };
        
        _registry = new AppRegistry(_registryLogger);
    }

    [Fact]
    public void ProxyServer_CanBeCreated()
    {
        // Arrange & Act
        _proxyServer = new ProxyServer(_config, _registry, _proxyLogger);

        // Assert
        Assert.NotNull(_proxyServer);
    }

    [Fact]
    public void AppRegistry_RegisterApp_StoresAppCorrectly()
    {
        // Arrange
        var connectionId = Guid.NewGuid().ToString();
        var appName = "TestApp";
        var tools = new List<McpToolDefinition>
        {
            new McpToolDefinition
            {
                Name = "TestApp:TestTool",
                Description = "A test tool",
                InputSchema = new { type = "object" }
            }
        };

        // Act
        var result = _registry.RegisterApp(connectionId, appName, tools);

        // Assert
        Assert.True(result);
        var registeredApp = _registry.GetApp(connectionId);
        Assert.NotNull(registeredApp);
        Assert.Equal(appName, registeredApp.Name);
        Assert.Equal(connectionId, registeredApp.ConnectionId);
        Assert.Single(registeredApp.Tools);
        Assert.Equal("TestApp:TestTool", registeredApp.Tools[0].Name);
    }

    [Fact]
    public void AppRegistry_FindAppByTool_ReturnsCorrectApp()
    {
        // Arrange
        var connectionId = Guid.NewGuid().ToString();
        var appName = "TestApp";
        var toolName = "TestApp:TestTool";
        var tools = new List<McpToolDefinition>
        {
            new McpToolDefinition
            {
                Name = toolName,
                Description = "A test tool",
                InputSchema = new { type = "object" }
            }
        };

        _registry.RegisterApp(connectionId, appName, tools);

        // Act
        var foundApp = _registry.FindAppByTool(toolName);

        // Assert
        Assert.NotNull(foundApp);
        Assert.Equal(appName, foundApp.Name);
        Assert.Equal(connectionId, foundApp.ConnectionId);
    }

    [Fact]
    public void AppRegistry_FindAppByTool_ReturnsNullForUnknownTool()
    {
        // Arrange
        var connectionId = Guid.NewGuid().ToString();
        var appName = "TestApp";
        var tools = new List<McpToolDefinition>
        {
            new McpToolDefinition
            {
                Name = "TestApp:TestTool",
                Description = "A test tool",
                InputSchema = new { type = "object" }
            }
        };

        _registry.RegisterApp(connectionId, appName, tools);

        // Act
        var foundApp = _registry.FindAppByTool("NonExistentTool");

        // Assert
        Assert.Null(foundApp);
    }

    [Fact]
    public void AppRegistry_GetAllTools_ReturnsAllToolsFromConnectedApps()
    {
        // Arrange
        var connectionId1 = Guid.NewGuid().ToString();
        var connectionId2 = Guid.NewGuid().ToString();
        
        var tools1 = new List<McpToolDefinition>
        {
            new McpToolDefinition
            {
                Name = "App1:Tool1",
                Description = "Tool 1",
                InputSchema = new { type = "object" }
            }
        };
        
        var tools2 = new List<McpToolDefinition>
        {
            new McpToolDefinition
            {
                Name = "App2:Tool1",
                Description = "Tool 2",
                InputSchema = new { type = "object" }
            }
        };

        _registry.RegisterApp(connectionId1, "App1", tools1);
        _registry.RegisterApp(connectionId2, "App2", tools2);

        // Act
        var allTools = _registry.GetAllTools();

        // Assert
        Assert.Equal(2, allTools.Count);
        Assert.Contains(allTools, t => t.Name == "App1:Tool1");
        Assert.Contains(allTools, t => t.Name == "App2:Tool1");
    }

    [Fact]
    public void AppRegistry_MarkDisconnected_UpdatesConnectionStatus()
    {
        // Arrange
        var connectionId = Guid.NewGuid().ToString();
        var appName = "TestApp";
        var tools = new List<McpToolDefinition>
        {
            new McpToolDefinition
            {
                Name = "TestApp:TestTool",
                Description = "A test tool",
                InputSchema = new { type = "object" }
            }
        };

        _registry.RegisterApp(connectionId, appName, tools);
        var app = _registry.GetApp(connectionId);
        Assert.NotNull(app);
        Assert.True(app.IsConnected);

        // Act
        _registry.MarkDisconnected(connectionId);

        // Assert
        var disconnectedApp = _registry.GetApp(connectionId);
        Assert.NotNull(disconnectedApp);
        Assert.False(disconnectedApp.IsConnected);
        
        // Disconnected apps should not appear in GetAllTools
        var allTools = _registry.GetAllTools();
        Assert.Empty(allTools);
    }

    public void Dispose()
    {
        _proxyServer?.Dispose();
    }
}
