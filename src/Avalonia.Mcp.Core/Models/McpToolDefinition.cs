using System.Text.Json.Serialization;

namespace Avalonia.Mcp.Core.Models;

/// <summary>
/// Defines an MCP tool that can be invoked by agents.
/// </summary>
public class McpToolDefinition
{
    /// <summary>
    /// Gets or sets the tool name (e.g., "ClickElement").
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the tool description for agent understanding.
    /// </summary>
    [JsonPropertyName("description")]
    public required string Description { get; set; }

    /// <summary>
    /// Gets or sets the JSON schema for input parameters.
    /// </summary>
    [JsonPropertyName("inputSchema")]
    public required object InputSchema { get; set; }
}

/// <summary>
/// Represents the result of a tool execution.
/// </summary>
public class McpToolResult
{
    /// <summary>
    /// Gets or sets whether the tool execution succeeded.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the result data (can be any JSON-serializable object).
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }

    /// <summary>
    /// Gets or sets the error message if execution failed.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static McpToolResult Ok(object? data = null) => new() 
    { 
        Success = true, 
        Data = data 
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static McpToolResult Fail(string error) => new() 
    { 
        Success = false, 
        Error = error 
    };
}

/// <summary>
/// Represents a tool invocation request.
/// </summary>
public class McpToolInvocation
{
    /// <summary>
    /// Gets or sets the tool name to invoke.
    /// </summary>
    [JsonPropertyName("tool")]
    public required string ToolName { get; set; }

    /// <summary>
    /// Gets or sets the parameters for the tool invocation.
    /// </summary>
    [JsonPropertyName("parameters")]
    public Dictionary<string, object>? Parameters { get; set; }
}
