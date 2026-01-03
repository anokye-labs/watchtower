using System;
using System.Text.Json.Serialization;

namespace WatchTower.Tools.McpServer.Models;

/// <summary>
/// Represents a JSON-RPC 2.0 request message for MCP tool execution.
/// </summary>
public class JsonRpcRequest
{
    /// <summary>
    /// Gets or sets the JSON-RPC protocol version (always "2.0").
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    /// <summary>
    /// Gets or sets the correlation ID that uniquely identifies this request.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the method name (should be "tools/call" for tool execution).
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parameters for the tool call.
    /// </summary>
    [JsonPropertyName("params")]
    public ToolCallParams? Params { get; set; }
}

/// <summary>
/// Represents the parameters for a tool call in a JSON-RPC 2.0 request.
/// </summary>
public class ToolCallParams
{
    /// <summary>
    /// Gets or sets the name of the tool to execute.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the arguments to pass to the tool.
    /// </summary>
    [JsonPropertyName("arguments")]
    public object? Arguments { get; set; }
}
