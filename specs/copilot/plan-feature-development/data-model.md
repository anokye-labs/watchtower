# Data Model: Federated Avalonia MCP Proxy Platform

**Feature Branch**: `002-federated-mcp-proxy`  
**Date**: 2025-11-29  
**Phase**: 1 - Design

## Overview

This document defines the data models for the Federated Avalonia MCP Proxy Platform based on the Key Entities from the feature specification.

---

## 1. MCP Protocol Models

### McpMessage

Base model for all MCP protocol communication (JSON-RPC 2.0).

```csharp
namespace Avalonia.Mcp.Protocol;

/// <summary>
/// Base MCP message following JSON-RPC 2.0 format.
/// </summary>
public abstract record McpMessage
{
    public required string JsonRpc { get; init; } = "2.0";
}

/// <summary>
/// Request message with method and parameters.
/// </summary>
public record McpRequest : McpMessage
{
    public required string Id { get; init; }
    public required string Method { get; init; }
    public JsonElement? Params { get; init; }
}

/// <summary>
/// Response message with result or error.
/// </summary>
public record McpResponse : McpMessage
{
    public required string Id { get; init; }
    public JsonElement? Result { get; init; }
    public McpError? Error { get; init; }
}

/// <summary>
/// Notification message (no response expected).
/// </summary>
public record McpNotification : McpMessage
{
    public required string Method { get; init; }
    public JsonElement? Params { get; init; }
}

/// <summary>
/// Error object per JSON-RPC 2.0.
/// </summary>
public record McpError
{
    public required int Code { get; init; }
    public required string Message { get; init; }
    public JsonElement? Data { get; init; }
}
```

**Validation Rules**:
- `JsonRpc` must be exactly "2.0"
- `Id` must be non-empty for requests/responses
- `Method` must be non-empty for requests/notifications

---

## 2. Tool Definition Models

### ToolDefinition

Schema for tool metadata exposed in `tools/list` response.

```csharp
namespace Avalonia.Mcp.Protocol;

/// <summary>
/// Tool definition following MCP tools/list schema.
/// </summary>
public record ToolDefinition
{
    /// <summary>
    /// Tool name in format "AppName:ToolName" for federated tools,
    /// or just "ToolName" for direct connections.
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Human-readable description of tool functionality.
    /// </summary>
    public required string Description { get; init; }
    
    /// <summary>
    /// JSON Schema describing the input parameters.
    /// </summary>
    public required JsonElement InputSchema { get; init; }
}

/// <summary>
/// Result of tool execution returned to agent.
/// </summary>
public record ToolResult
{
    /// <summary>
    /// Array of content items (text, image, etc.)
    /// </summary>
    public required IReadOnlyList<ToolContent> Content { get; init; }
    
    /// <summary>
    /// Whether the tool execution resulted in an error.
    /// </summary>
    public bool IsError { get; init; }
}

/// <summary>
/// Content item in tool result.
/// </summary>
public abstract record ToolContent
{
    public abstract string Type { get; }
}

/// <summary>
/// Text content in tool result.
/// </summary>
public record TextContent : ToolContent
{
    public override string Type => "text";
    public required string Text { get; init; }
}

/// <summary>
/// Image content in tool result (base64 encoded).
/// </summary>
public record ImageContent : ToolContent
{
    public override string Type => "image";
    public required string Data { get; init; }
    public required string MimeType { get; init; }
}
```

**Validation Rules**:
- Tool `Name` must match pattern `^[A-Za-z][A-Za-z0-9_]*(:?[A-Za-z][A-Za-z0-9_]*)?$`
- `Description` should be 1-500 characters
- `InputSchema` must be valid JSON Schema draft-07

---

## 3. UI Element Models

### ElementInfo

Representation of UI elements from accessibility tree (FR-033).

```csharp
namespace Avalonia.Mcp.Models;

/// <summary>
/// Hierarchical representation of a UI element from the accessibility tree.
/// </summary>
public record ElementInfo
{
    /// <summary>
    /// Control type name (e.g., "Button", "TextBox", "Panel").
    /// </summary>
    public required string Type { get; init; }
    
    /// <summary>
    /// Automation name/label of the element.
    /// </summary>
    public string? Name { get; init; }
    
    /// <summary>
    /// Index of this element among siblings of the same type.
    /// </summary>
    public required int Index { get; init; }
    
    /// <summary>
    /// Bounding rectangle in screen coordinates.
    /// </summary>
    public required ElementBounds Bounds { get; init; }
    
    /// <summary>
    /// Hierarchical path from root (FR-032 format).
    /// Example: "MainWindow/StackPanel[0]/Button[2]"
    /// </summary>
    public required string Path { get; init; }
    
    /// <summary>
    /// Additional accessibility properties.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Properties { get; init; }
    
    /// <summary>
    /// Child elements in the accessibility tree.
    /// </summary>
    public IReadOnlyList<ElementInfo>? Children { get; init; }
}

/// <summary>
/// Bounding rectangle for element positioning.
/// </summary>
public record ElementBounds
{
    public required double X { get; init; }
    public required double Y { get; init; }
    public required double Width { get; init; }
    public required double Height { get; init; }
    
    /// <summary>
    /// Center point for click targeting.
    /// </summary>
    public (double X, double Y) Center => (X + Width / 2, Y + Height / 2);
}
```

**Validation Rules**:
- `Type` must be non-empty
- `Index` must be >= 0
- `Bounds` values must be >= 0
- `Path` must match format `^[A-Za-z]+(\[[0-9]+\])?(/[A-Za-z]+(\[[0-9]+\])?)*$`

### ElementPath

Parser and builder for hierarchical element paths (FR-031-032).

```csharp
namespace Avalonia.Mcp.Models;

using System.Text.RegularExpressions;

/// <summary>
/// Parses and builds hierarchical element paths.
/// Format: "ParentType[index]/ChildType[index]"
/// </summary>
public record ElementPath
{
    public required IReadOnlyList<ElementPathSegment> Segments { get; init; }
    
    public static ElementPath Parse(string path)
    {
        var segments = path.Split('/')
            .Select(ElementPathSegment.Parse)
            .ToList();
        return new ElementPath { Segments = segments };
    }
    
    public override string ToString() => 
        string.Join("/", Segments.Select(s => s.ToString()));
}

/// <summary>
/// Single segment of an element path.
/// </summary>
public record ElementPathSegment
{
    public required string Type { get; init; }
    public required int Index { get; init; }
    
    public static ElementPathSegment Parse(string segment)
    {
        var match = Regex.Match(segment, @"^([A-Za-z]+)\[(\d+)\]$");
        if (!match.Success)
            throw new FormatException($"Invalid path segment: {segment}");
        
        return new ElementPathSegment
        {
            Type = match.Groups[1].Value,
            Index = int.Parse(match.Groups[2].Value)
        };
    }
    
    public override string ToString() => $"{Type}[{Index}]";
}
```

---

## 4. Application Registry Models

### ApplicationRegistration

Record of a connected application in the proxy (Key Entity from spec).

```csharp
namespace Avalonia.Mcp.Proxy.Models;

/// <summary>
/// Registration record for a connected Avalonia application.
/// </summary>
public record ApplicationRegistration
{
    /// <summary>
    /// Unique identifier for the application (e.g., "WatchTower", "AdminTool").
    /// Used as namespace prefix for tool names.
    /// </summary>
    public required string AppId { get; init; }
    
    /// <summary>
    /// Transport endpoint for communication (e.g., "tcp://localhost:5001").
    /// </summary>
    public required string Endpoint { get; init; }
    
    /// <summary>
    /// Current connection status.
    /// </summary>
    public required ConnectionStatus Status { get; init; }
    
    /// <summary>
    /// Timestamp when the application connected.
    /// </summary>
    public required DateTimeOffset ConnectedAt { get; init; }
    
    /// <summary>
    /// Timestamp of last successful tool call (used to detect stale connections).
    /// </summary>
    public DateTimeOffset? LastActivity { get; init; }
    
    /// <summary>
    /// Tools exposed by this application.
    /// </summary>
    public required IReadOnlyList<ToolDefinition> Tools { get; init; }
}

/// <summary>
/// Connection status for registered applications.
/// </summary>
public enum ConnectionStatus
{
    /// <summary>Connection is active and healthy.</summary>
    Connected,
    
    /// <summary>Connection lost, awaiting reconnection.</summary>
    Disconnected,
    
    /// <summary>Application is reconnecting after restart.</summary>
    Reconnecting,
    
    /// <summary>Application explicitly unregistered.</summary>
    Unregistered
}
```

**Validation Rules**:
- `AppId` must be unique across all registrations
- `AppId` must match pattern `^[A-Za-z][A-Za-z0-9_-]*$`
- `Endpoint` must be valid URI format
- `Tools` may be empty but not null

---

## 5. Tool Catalog Models

### ToolCatalogEntry

Aggregated tool entry with namespace prefix (Key Entity from spec).

```csharp
namespace Avalonia.Mcp.Proxy.Models;

/// <summary>
/// Entry in the aggregated tool catalog with namespace prefix.
/// </summary>
public record ToolCatalogEntry
{
    /// <summary>
    /// Application that owns this tool.
    /// </summary>
    public required string AppId { get; init; }
    
    /// <summary>
    /// Original tool name without namespace.
    /// </summary>
    public required string ToolName { get; init; }
    
    /// <summary>
    /// Namespaced tool name (AppId:ToolName) for agent-facing API.
    /// </summary>
    public string NamespacedName => $"{AppId}:{ToolName}";
    
    /// <summary>
    /// Tool definition with updated name for federation.
    /// </summary>
    public required ToolDefinition Definition { get; init; }
}
```

---

## 6. Transport Connection Models

### TransportConfiguration

Configuration for transport layer connections (Key Entity from spec).

```csharp
namespace Avalonia.Mcp.Transport;

/// <summary>
/// Configuration for transport connections.
/// </summary>
public record TransportConfiguration
{
    /// <summary>
    /// Transport type to use.
    /// </summary>
    public required TransportType Type { get; init; }
    
    /// <summary>
    /// Endpoint address (host:port for TCP, pipe name for Named Pipes, URL for HTTP).
    /// </summary>
    public required string Endpoint { get; init; }
    
    /// <summary>
    /// Connection timeout.
    /// </summary>
    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(10);
    
    /// <summary>
    /// Read/write timeout for operations.
    /// </summary>
    public TimeSpan OperationTimeout { get; init; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Supported transport types per FR-012-014.
/// </summary>
public enum TransportType
{
    /// <summary>TCP socket connection (FR-012).</summary>
    Tcp,
    
    /// <summary>Windows Named Pipes (FR-013).</summary>
    NamedPipe,
    
    /// <summary>HTTP with Server-Sent Events (FR-014).</summary>
    HttpSse
}
```

---

## 7. Security Models

### AuthenticationToken

Token for shared secret authentication (FR-022-025).

```csharp
namespace Avalonia.Mcp.Security;

/// <summary>
/// Authentication token for proxy/app/agent communication.
/// </summary>
public record AuthenticationToken
{
    /// <summary>
    /// Application or agent identifier.
    /// </summary>
    public required string ClientId { get; init; }
    
    /// <summary>
    /// Unix timestamp when token was generated.
    /// </summary>
    public required long Timestamp { get; init; }
    
    /// <summary>
    /// HMAC-SHA256 signature of ClientId:Timestamp.
    /// </summary>
    public required string Signature { get; init; }
    
    /// <summary>
    /// Serialized token format: ClientId:Timestamp:Signature
    /// </summary>
    public override string ToString() => $"{ClientId}:{Timestamp}:{Signature}";
    
    public static AuthenticationToken Parse(string token)
    {
        var parts = token.Split(':');
        if (parts.Length != 3)
            throw new FormatException("Invalid token format");
        
        return new AuthenticationToken
        {
            ClientId = parts[0],
            Timestamp = long.Parse(parts[1]),
            Signature = parts[2]
        };
    }
}
```

**Validation Rules**:
- `ClientId` must be non-empty
- `Timestamp` must be within valid time window (configurable, default 30 minutes)
- `Signature` must be valid Base64

---

## 8. Configuration Models

### ProxyConfiguration

Proxy server configuration. The proxy discovers running applications by scanning process environment 
variables for `AVALONIA_MCP_ENDPOINT` which contains the embedded MCP endpoint information.

```csharp
namespace Avalonia.Mcp.Configuration;

/// <summary>
/// Proxy server configuration.
/// </summary>
public record ProxyConfiguration
{
    /// <summary>
    /// Shared secret for authentication (base64 encoded).
    /// </summary>
    public required string SharedSecret { get; init; }
    
    /// <summary>
    /// Token expiry duration.
    /// </summary>
    public TimeSpan TokenExpiry { get; init; } = TimeSpan.FromMinutes(30);
    
    /// <summary>
    /// Default timeout for tool execution.
    /// </summary>
    public TimeSpan ToolTimeout { get; init; } = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// Interval for scanning processes for MCP endpoints.
    /// </summary>
    public TimeSpan DiscoveryScanInterval { get; init; } = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// Debounce interval for tool catalog updates (FR-Edge Case).
    /// </summary>
    public TimeSpan CatalogDebounce { get; init; } = TimeSpan.FromMilliseconds(500);
}

/// <summary>
/// Environment variable-based endpoint advertisement.
/// Applications set this in their process environment to be discovered by the proxy.
/// Format: "transport://endpoint" (e.g., "tcp://localhost:5001", "pipe://MyApp.Mcp")
/// </summary>
public static class McpEnvironment
{
    /// <summary>
    /// Environment variable name for MCP endpoint advertisement.
    /// </summary>
    public const string EndpointVariable = "AVALONIA_MCP_ENDPOINT";
    
    /// <summary>
    /// Environment variable name for application identifier.
    /// </summary>
    public const string AppIdVariable = "AVALONIA_MCP_APP_ID";
}

/// <summary>
/// Embedded handler configuration for applications.
/// </summary>
public record EmbeddedConfiguration
{
    /// <summary>
    /// Application identifier for namespace prefix.
    /// </summary>
    public required string AppId { get; init; }
    
    /// <summary>
    /// Shared secret for proxy authentication.
    /// </summary>
    public required string SharedSecret { get; init; }
    
    /// <summary>
    /// Transport type for the embedded endpoint.
    /// </summary>
    public TransportType Transport { get; init; } = TransportType.Tcp;
    
    /// <summary>
    /// Port or pipe name for the embedded endpoint (0 = auto-assign).
    /// </summary>
    public int Port { get; init; } = 0;
    
    /// <summary>
    /// Whether to enable headless mode support.
    /// </summary>
    public bool EnableHeadless { get; init; } = true;
    
    /// <summary>
    /// Whether to advertise endpoint via environment variable for proxy discovery.
    /// </summary>
    public bool AdvertiseEndpoint { get; init; } = true;
}
```

**Discovery Pattern**: The proxy acts like a debugger, scanning running processes for the 
`AVALONIA_MCP_ENDPOINT` environment variable. Applications that want to be discovered set this 
variable with their endpoint information when starting the embedded MCP handler.

---

## Entity Relationships

```text
┌─────────────────────────────────────────────────────────────────────┐
│                           MCP Proxy                                  │
│  ┌──────────────────┐    ┌──────────────────┐                       │
│  │ ApplicationRegistry│◄───┤ ToolCatalog      │                       │
│  │  - registrations[]│    │  - entries[]     │                       │
│  └────────┬─────────┘    └────────┬─────────┘                       │
│           │                       │                                  │
│           │ manages               │ aggregates                       │
│           ▼                       ▼                                  │
│  ┌──────────────────┐    ┌──────────────────┐                       │
│  │ ApplicationReg   │    │ ToolCatalogEntry │                       │
│  │  - appId         │◄───┤  - appId         │                       │
│  │  - endpoint      │    │  - toolName      │                       │
│  │  - tools[]       │    │  - definition    │                       │
│  └────────┬─────────┘    └──────────────────┘                       │
│           │                                                          │
│           │ connects via                                             │
│           ▼                                                          │
│  ┌──────────────────┐                                               │
│  │TransportConnection│                                               │
│  │  - type          │                                               │
│  │  - endpoint      │                                               │
│  └──────────────────┘                                               │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              │ routes to
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     Embedded MCP Handler                             │
│  ┌──────────────────┐    ┌──────────────────┐                       │
│  │ ToolRegistry     │◄───┤ ElementInfo      │                       │
│  │  - tools[]       │    │  - type          │                       │
│  │  - execute()     │    │  - path          │                       │
│  └────────┬─────────┘    │  - bounds        │                       │
│           │              │  - children[]    │                       │
│           │              └──────────────────┘                       │
│           │ registers                                                │
│           ▼                                                          │
│  ┌──────────────────┐                                               │
│  │ ToolDefinition   │                                               │
│  │  - name          │                                               │
│  │  - description   │                                               │
│  │  - inputSchema   │                                               │
│  └──────────────────┘                                               │
└─────────────────────────────────────────────────────────────────────┘
```

---

## State Transitions

### ApplicationRegistration Status

```text
                    ┌─────────────────────┐
                    │                     │
   proxy discovers  │                     │  process exits
        │           ▼                     │       │
        │    ┌─────────────┐              │       │
        └───►│  Connected  │──────────────┼───────┘
             └──────┬──────┘              │
                    │                     │
     process not    │  connection lost    │
       found        │       │             │
                    │       ▼             │
                    │ ┌─────────────┐     │
                    │ │ Disconnected│◄────┘
                    │ └──────┬──────┘
                    │        │
                    │        │ process rediscovered
                    │        ▼
                    │ ┌─────────────┐
                    └─┤ Reconnecting│
                      └──────┬──────┘
                             │
                             │ registration confirmed
                             ▼
                      ┌─────────────┐
                      │ Unregistered │
                      └─────────────┘
```

---

## Summary

| Entity | Purpose | Key Fields |
|--------|---------|------------|
| McpMessage | Protocol communication | jsonrpc, method, params |
| ToolDefinition | Tool metadata | name, description, inputSchema |
| ToolResult | Execution result | content, isError |
| ElementInfo | UI element data | type, path, bounds, children |
| ApplicationRegistration | Connected app record | appId, endpoint, status, tools |
| ToolCatalogEntry | Federated tool entry | appId, toolName, definition |
| TransportConfiguration | Connection settings | type, endpoint, timeouts |
| AuthenticationToken | Security token | clientId, timestamp, signature |
