namespace Avalonia.McpProxy.Models;

/// <summary>
/// Configuration for the MCP proxy server.
/// </summary>
public class ProxyConfiguration
{
    /// <summary>
    /// Gets or sets the bind address for the proxy server.
    /// Default: localhost:5100
    /// </summary>
    public string BindAddress { get; set; } = "localhost:5100";

    /// <summary>
    /// Gets or sets the log level (Trace, Debug, Information, Warning, Error, Critical).
    /// </summary>
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// Gets or sets the maximum number of concurrent app connections.
    /// </summary>
    public int MaxConnections { get; set; } = 50;

    /// <summary>
    /// Gets or sets the list of expected applications.
    /// </summary>
    public List<AppConfiguration> Apps { get; set; } = new();
}

/// <summary>
/// Configuration for an application that can connect to the proxy.
/// </summary>
public class AppConfiguration
{
    /// <summary>
    /// Gets or sets the application name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the endpoint where the app handler listens.
    /// Format: tcp://localhost:5000
    /// </summary>
    public required string Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the application description.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Root configuration model for .mcpproxy.json file.
/// </summary>
public class McpProxyConfig
{
    public ProxyConfiguration Proxy { get; set; } = new();
}
