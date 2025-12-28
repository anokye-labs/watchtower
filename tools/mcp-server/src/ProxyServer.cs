using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WatchTower.Tools.McpServer.Models;

namespace WatchTower.Tools.McpServer;

/// <summary>
/// Proxy server that manages request correlation and routing for MCP tool execution.
/// Provides thread-safe correlation ID generation and pending request tracking.
/// </summary>
public class ProxyServer
{
    private long _nextCorrelationId;
    private readonly ConcurrentDictionary<long, TaskCompletionSource<McpResponse>> _pendingRequests;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProxyServer"/> class.
    /// </summary>
    public ProxyServer()
    {
        _nextCorrelationId = 0;
        _pendingRequests = new ConcurrentDictionary<long, TaskCompletionSource<McpResponse>>();
    }

    /// <summary>
    /// Generates a new unique correlation ID in a thread-safe manner.
    /// </summary>
    /// <returns>A unique correlation ID.</returns>
    public long GenerateCorrelationId()
    {
        return Interlocked.Increment(ref _nextCorrelationId);
    }

    /// <summary>
    /// Registers a new pending request with the specified correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID for the request.</param>
    /// <returns>A TaskCompletionSource that will be completed when the response is received.</returns>
    public TaskCompletionSource<McpResponse> RegisterPendingRequest(long correlationId)
    {
        var tcs = new TaskCompletionSource<McpResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pendingRequests.TryAdd(correlationId, tcs))
        {
            throw new InvalidOperationException($"Correlation ID {correlationId} is already in use.");
        }
        return tcs;
    }

    /// <summary>
    /// Attempts to retrieve a pending request by its correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID to look up.</param>
    /// <param name="tcs">The TaskCompletionSource associated with the correlation ID, if found.</param>
    /// <returns>True if the pending request was found; otherwise, false.</returns>
    public bool TryGetPendingRequest(long correlationId, out TaskCompletionSource<McpResponse>? tcs)
    {
        return _pendingRequests.TryGetValue(correlationId, out tcs);
    }

    /// <summary>
    /// Removes a pending request from tracking.
    /// </summary>
    /// <param name="correlationId">The correlation ID to remove.</param>
    /// <returns>True if the request was found and removed; otherwise, false.</returns>
    public bool RemovePendingRequest(long correlationId)
    {
        return _pendingRequests.TryRemove(correlationId, out _);
    }

    /// <summary>
    /// Completes a pending request with the specified response.
    /// </summary>
    /// <param name="response">The response to complete the request with.</param>
    /// <returns>True if the request was found and completed; otherwise, false.</returns>
    public bool CompletePendingRequest(McpResponse response)
    {
        if (_pendingRequests.TryRemove(response.CorrelationId, out var tcs))
        {
            tcs.SetResult(response);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Cancels a pending request with the specified correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID of the request to cancel.</param>
    /// <param name="reason">The reason for cancellation.</param>
    /// <returns>True if the request was found and canceled; otherwise, false.</returns>
    public bool CancelPendingRequest(long correlationId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Cancellation reason cannot be null or empty.", nameof(reason));
        }

        if (_pendingRequests.TryRemove(correlationId, out var tcs))
        {
            return tcs.TrySetException(new OperationCanceledException(reason));
        }
        return false;
    }

    /// <summary>
    /// Gets the count of currently pending requests.
    /// </summary>
    public int PendingRequestCount => _pendingRequests.Count;

    /// <summary>
    /// Clears all pending requests, canceling them with the specified reason.
    /// </summary>
    /// <param name="reason">The reason for clearing all requests.</param>
    public void ClearPendingRequests(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Cancellation reason cannot be null or empty.", nameof(reason));
        }

        var keys = _pendingRequests.Keys.ToList();
        foreach (var key in keys)
        {
            if (_pendingRequests.TryRemove(key, out var tcs))
            {
                tcs.TrySetException(new OperationCanceledException(reason));
            }
        }
    }
}
