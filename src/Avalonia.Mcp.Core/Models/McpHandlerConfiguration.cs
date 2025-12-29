namespace Avalonia.Mcp.Core.Models;

/// <summary>
/// Configuration for the embedded MCP handler.
/// </summary>
public class McpHandlerConfiguration
{
    /// <summary>
    /// Gets or sets the application name (used for tool namespacing).
    /// </summary>
    public required string ApplicationName { get; set; }

    /// <summary>
    /// Gets or sets the endpoint to connect to the proxy.
    /// Format: tcp://host:port or pipe://pipename
    /// </summary>
    public required string ProxyEndpoint { get; set; }

    /// <summary>
    /// Gets or sets whether to auto-connect on startup.
    /// </summary>
    public bool AutoConnect { get; set; } = true;

    /// <summary>
    /// Gets or sets the reconnection interval in milliseconds.
    /// </summary>
    public int ReconnectIntervalMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets whether to run in headless mode.
    /// </summary>
    public bool HeadlessMode { get; set; } = false;
}
