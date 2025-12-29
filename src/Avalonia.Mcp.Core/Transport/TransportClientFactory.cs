using Microsoft.Extensions.Logging;

namespace Avalonia.Mcp.Core.Transport;

/// <summary>
/// Factory for creating transport clients based on endpoint URIs.
/// </summary>
public static class TransportClientFactory
{
    /// <summary>
    /// Creates a transport client from an endpoint URI.
    /// Supported formats:
    /// - tcp://host:port
    /// - pipe://pipename (Windows only)
    /// </summary>
    public static ITransportClient Create(string endpoint, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("Endpoint cannot be null or empty", nameof(endpoint));

        var uri = new Uri(endpoint);

        return uri.Scheme.ToLowerInvariant() switch
        {
            "tcp" => CreateTcpClient(uri, logger),
            "pipe" => CreateNamedPipeClient(uri, logger),
            _ => throw new NotSupportedException($"Transport scheme '{uri.Scheme}' is not supported")
        };
    }

    private static ITransportClient CreateTcpClient(Uri uri, ILogger? logger)
    {
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5000; // Default MCP proxy port

        return new TcpTransportClient(host, port, logger);
    }

    private static ITransportClient CreateNamedPipeClient(Uri uri, ILogger? logger)
    {
        // Named pipes implementation placeholder
        // Format: pipe://pipename
        throw new NotImplementedException("Named pipe transport is not yet implemented");
    }
}
