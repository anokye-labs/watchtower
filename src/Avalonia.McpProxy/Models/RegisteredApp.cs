using Avalonia.Mcp.Core.Models;

namespace Avalonia.McpProxy.Models;

/// <summary>
/// Represents a connected application in the proxy registry.
/// </summary>
public class RegisteredApp
{
    /// <summary>
    /// Gets or sets the application name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the connection ID.
    /// </summary>
    public required string ConnectionId { get; set; }

    /// <summary>
    /// Gets or sets the tools exposed by this application.
    /// </summary>
    public List<McpToolDefinition> Tools { get; set; } = new();

    /// <summary>
    /// Gets or sets when the application was registered.
    /// </summary>
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last activity timestamp.
    /// </summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether the application is currently connected.
    /// </summary>
    public bool IsConnected { get; set; } = true;
}

/// <summary>
/// MCP protocol message types.
/// </summary>
public class McpMessage
{
    public required string Type { get; set; }
    public object? Data { get; set; }
}

/// <summary>
/// Registration message from an application.
/// </summary>
public class RegistrationMessage
{
    public required string AppName { get; set; }
    public List<McpToolDefinition>? Tools { get; set; }
}
