# Research: Federated Avalonia MCP Proxy Platform

**Feature Branch**: `002-federated-mcp-proxy`  
**Date**: 2025-11-29  
**Phase**: 0 - Research

## Overview

This document captures research findings and technology decisions for implementing the Federated Avalonia MCP Proxy Platform. Each section addresses key technical unknowns from the plan's Technical Context.

---

## 1. MCP Protocol Implementation in .NET

### Decision

Implement MCP protocol natively in .NET using System.Text.Json for serialization and System.IO.Pipelines for efficient streaming.

### Rationale

- **No official .NET MCP SDK**: As of late 2025, the Model Context Protocol has official SDKs for TypeScript/JavaScript and Python, but no official .NET SDK
- **Protocol simplicity**: MCP uses JSON-RPC 2.0 over stdio, which is straightforward to implement
- **Control over implementation**: Native implementation allows optimization for Avalonia-specific use cases
- **Minimal dependencies**: Avoids runtime dependency on Node.js or Python

### Alternatives Considered

| Alternative | Rejected Because |
|-------------|------------------|
| Wrap TypeScript SDK via Node.js | Additional runtime dependency, IPC overhead, deployment complexity |
| Use Python SDK via IronPython | Performance concerns, Avalonia interop complexity |
| Community .NET MCP libraries | Limited maturity, uncertain maintenance |

### Implementation Notes

- Use `System.Text.Json` with source generators for AOT-compatible serialization
- Implement JSON-RPC 2.0 message framing per MCP spec
- Support both stdio (agent-facing) and TCP/Named Pipes (app-to-proxy)
- Message types: `initialize`, `tools/list`, `tools/call`, notifications for tool changes

---

## 2. Avalonia Headless Platform for Screenshots

### Decision

Use `Avalonia.Headless` package with `SkiaSharp` rendering for headless screenshot capture.

### Rationale

- **Built-in support**: Avalonia 11.x provides first-class headless platform support
- **SkiaSharp rendering**: Full visual fidelity without display server
- **CI/CD compatible**: Works in environments without X11/Wayland/Windows desktop
- **Performance**: Direct memory bitmap capture without window system overhead

### Alternatives Considered

| Alternative | Rejected Because |
|-------------|------------------|
| Virtual framebuffer (Xvfb) | Linux-only, adds infrastructure complexity |
| Window capture APIs | Platform-specific, requires GUI session |
| Mock rendering | No visual fidelity, defeats testing purpose |

### Implementation Notes

```csharp
// Headless platform initialization
AppBuilder.Configure<App>()
    .UseHeadless(new AvaloniaHeadlessPlatformOptions
    {
        UseHeadlessDrawing = true,
        FrameBufferFormat = PixelFormat.Rgba8888
    });

// Screenshot capture
var bitmap = new RenderTargetBitmap(new PixelSize(width, height));
bitmap.Render(control);
return bitmap.ToByteArray();
```

- Headless mode detected via `Application.Current.ApplicationLifetime`
- Same code path for GUI and headless screenshot capture via `RenderTargetBitmap`
- FR-005 satisfied: Screenshot capture works identically in both modes

---

## 3. Avalonia Accessibility Tree Traversal

### Decision

Use Avalonia's `AutomationPeer` API for accessibility tree traversal, with hierarchical path generation per FR-031-033.

### Rationale

- **Native API**: Avalonia provides `AutomationPeer` similar to WPF/UWP
- **Rich metadata**: Access to control type, name, bounding rect, children
- **Standard pattern**: Follows accessibility patterns used by screen readers
- **Testability**: Well-documented API with predictable behavior

### Alternatives Considered

| Alternative | Rejected Because |
|-------------|------------------|
| Visual tree traversal | No semantic information, harder to identify elements |
| Custom element tagging | Requires app modification, non-standard |
| X11/Windows accessibility APIs | Platform-specific, inconsistent across OS |

### Implementation Notes

```csharp
// Element tree traversal
public ElementInfo BuildElementTree(Control root)
{
    var peer = AutomationPeer.GetOrCreate(root);
    return new ElementInfo
    {
        Type = peer.GetClassName(),
        Name = peer.GetName(),
        Bounds = peer.GetBoundingRectangle(),
        Children = peer.GetChildren()
            .Select(BuildElementTree)
            .ToList()
    };
}

// Hierarchical path format: "MainWindow/StackPanel[0]/Button[2]"
public string GetElementPath(Control element)
{
    var path = new Stack<string>();
    while (element != null)
    {
        var peer = AutomationPeer.GetOrCreate(element);
        var parent = element.Parent as Control;
        var index = parent?.GetVisualChildren()
            .OfType<Control>()
            .Where(c => c.GetType() == element.GetType())
            .ToList()
            .IndexOf(element) ?? 0;
        
        path.Push($"{peer.GetClassName()}[{index}]");
        element = parent;
    }
    return string.Join("/", path);
}
```

- FR-031: Hierarchical path identification via accessibility tree
- FR-032: Path format "ParentType[index]/ChildType[index]"
- FR-033: GetElementTree returns sufficient metadata for path construction

---

## 4. Transport Layer Architecture

### Decision

Implement transport abstraction with TCP as default, Named Pipes for Windows, and HTTP/SSE for remote scenarios.

### Rationale

- **Flexibility**: Different scenarios require different transports
- **Performance**: TCP/Named Pipes for low-latency local communication
- **Security**: Named Pipes provide Windows ACL-based security
- **Remote**: HTTP/SSE enables cloud/remote debugging scenarios

### Transport Selection Matrix

| Scenario | Transport | Rationale |
|----------|-----------|-----------|
| Local development (any OS) | TCP (localhost) | Universal, simple, sub-ms latency |
| Windows IPC | Named Pipes | Secure by default, no port conflicts |
| Remote debugging | HTTP/SSE | Firewall-friendly, works across networks |
| CI/CD | TCP or Named Pipes | Localhost-only, no external deps |

### Alternatives Considered

| Alternative | Rejected Because |
|-------------|------------------|
| gRPC | Additional dependency, overkill for simple messaging |
| WebSockets | More complex than SSE for server-to-client streaming |
| Unix domain sockets | Not available on Windows |
| Shared memory | Complex synchronization, limited portability |

### Implementation Notes

```csharp
// Transport abstraction
public interface ITransportConnection
{
    Task ConnectAsync(string endpoint, CancellationToken ct);
    Task SendAsync(McpMessage message, CancellationToken ct);
    IAsyncEnumerable<McpMessage> ReceiveAsync(CancellationToken ct);
    Task DisconnectAsync(CancellationToken ct);
}

// Factory for transport selection
public class TransportFactory
{
    public ITransportConnection Create(TransportType type) => type switch
    {
        TransportType.Tcp => new TcpTransportConnection(),
        TransportType.NamedPipe => new NamedPipeTransportConnection(),
        TransportType.HttpSse => new HttpSseTransportConnection(),
        _ => throw new NotSupportedException()
    };
}
```

- FR-012: TCP transport via `System.Net.Sockets.TcpClient`
- FR-013: Named Pipes via `System.IO.Pipes.NamedPipeClientStream`
- FR-014: HTTP/SSE via `System.Net.Http.HttpClient` with SSE parsing
- FR-015: Transport transparent via `ITransportConnection` abstraction

---

## 5. Security: Shared Secret Authentication

### Decision

Use configuration-file-based shared secret with HMAC-SHA256 for token generation per FR-022-025.

### Rationale

- **Simplicity**: No PKI infrastructure required
- **Localhost focus**: Adequate security for development scenarios
- **Configuration-driven**: Easy to rotate secrets, no code changes
- **Industry standard**: HMAC-SHA256 is well-understood and secure

### Alternatives Considered

| Alternative | Rejected Because |
|-------------|------------------|
| mTLS | Overkill for localhost, complex certificate management |
| OAuth 2.0 | Requires external identity provider |
| No authentication | Spec requires authentication (FR-022) |
| API keys | Less secure than HMAC-based tokens |

### Implementation Notes

```json
// appsettings.json
{
  "McpProxy": {
    "SharedSecret": "base64-encoded-32-byte-secret",
    "TokenExpiry": "00:30:00"
  }
}
```

```csharp
// Token generation
public string GenerateToken(string appId)
{
    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    var payload = $"{appId}:{timestamp}";
    var hmac = HMACSHA256.HashData(_secret, Encoding.UTF8.GetBytes(payload));
    return $"{payload}:{Convert.ToBase64String(hmac)}";
}

// Token validation
public bool ValidateToken(string token)
{
    var parts = token.Split(':');
    if (parts.Length != 3) return false;
    
    var payload = $"{parts[0]}:{parts[1]}";
    var expectedHmac = HMACSHA256.HashData(_secret, Encoding.UTF8.GetBytes(payload));
    return CryptographicOperations.FixedTimeEquals(
        expectedHmac, 
        Convert.FromBase64String(parts[2]));
}
```

- FR-022: Shared secret in appsettings.json
- FR-023: Agent includes token in initialize request
- FR-024: App includes token in registration request
- FR-025: Failed auth logged with error response

---

## 6. Logging and Observability

### Decision

Use `Microsoft.Extensions.Logging` with structured JSON output per FR-026-030.

### Rationale

- **Consistent with WatchTower**: Existing LoggingService uses same framework
- **Structured logging**: JSON format enables log aggregation and search
- **Configurable**: Verbosity controlled via appsettings.json
- **Cross-platform**: Works identically on all target platforms

### Implementation Notes

```csharp
// Structured logging for tool calls (FR-029)
public async Task<ToolResult> ExecuteToolAsync(string tool, object args)
{
    var stopwatch = Stopwatch.StartNew();
    try
    {
        var result = await _router.RouteAsync(tool, args);
        _logger.LogInformation(
            "Tool executed: {Application}:{Tool} in {Duration}ms - {Status}",
            result.Application, result.Tool, stopwatch.ElapsedMilliseconds, "Success");
        return result;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex,
            "Tool failed: {Application}:{Tool} in {Duration}ms - {Status}",
            appId, tool, stopwatch.ElapsedMilliseconds, "Error");
        throw;
    }
}
```

- FR-026: JSON structured logging via `JsonConsoleFormatter`
- FR-027: Log levels configurable in appsettings.json
- FR-028: Connection events, auth attempts, tool routing logged
- FR-029: Tool call logs include timestamp, app, tool, duration, status
- FR-030: Embedded handlers use same logging configuration

---

## Summary of Decisions

| Area | Decision | Key Benefit |
|------|----------|-------------|
| MCP Protocol | Native .NET implementation | No external runtime dependencies |
| Screenshots | Avalonia.Headless + SkiaSharp | CI/CD compatible, full visual fidelity |
| UI Inspection | AutomationPeer API | Rich semantic metadata, standard pattern |
| Transport | TCP/Named Pipes/HTTP abstraction | Flexibility for different scenarios |
| Authentication | HMAC-SHA256 shared secret | Simple, secure for localhost |
| Logging | Microsoft.Extensions.Logging | Consistent with existing WatchTower |

All research items resolved. No NEEDS CLARIFICATION items remain. Ready to proceed to Phase 1.
