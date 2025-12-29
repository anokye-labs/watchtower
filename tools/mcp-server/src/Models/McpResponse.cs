using System;

namespace WatchTower.Tools.McpServer.Models;

/// <summary>
/// Represents a response from an MCP tool execution.
/// Used to match responses with their originating requests via correlation ID.
/// </summary>
public class McpResponse
{
    /// <summary>
    /// Gets or sets the correlation ID that matches this response to its request.
    /// </summary>
    public long CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the response was created.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the tool execution was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the result data from the tool execution.
    /// </summary>
    public object? Result { get; set; }

    /// <summary>
    /// Gets or sets the error message if the tool execution failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="McpResponse"/> class.
    /// </summary>
    public McpResponse()
    {
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="McpResponse"/> class with the specified correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID that matches this response to its request.</param>
    public McpResponse(long correlationId) : this()
    {
        CorrelationId = correlationId;
    }
}
