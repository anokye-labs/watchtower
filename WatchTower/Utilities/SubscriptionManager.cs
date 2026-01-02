using System;
using System.Collections.Generic;

namespace WatchTower.Utilities;

/// <summary>
/// Manages event subscriptions using a CompositeDisposable-style pattern.
/// Allows centralized tracking of subscription actions and automatic unsubscription on disposal.
/// </summary>
/// <remarks>
/// This class simplifies subscription management in ViewModels by maintaining a list
/// of unsubscribe actions. When disposed, it automatically calls all unsubscribe actions,
/// preventing memory leaks and reducing boilerplate code.
/// <para>
/// This class is thread-safe and can be used from multiple threads. However, it is typically
/// used from a single thread (the UI thread) in ViewModels.
/// </para>
/// </remarks>
public class SubscriptionManager : IDisposable
{
    private readonly List<Action> _unsubscribeActions = new();
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Adds an unsubscribe action to be invoked when the manager is disposed.
    /// </summary>
    /// <param name="unsubscribe">The action to invoke for unsubscribing.</param>
    /// <exception cref="ArgumentNullException">Thrown when unsubscribe is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when attempting to add after disposal.</exception>
    public void Add(Action unsubscribe)
    {
        if (unsubscribe == null)
        {
            throw new ArgumentNullException(nameof(unsubscribe));
        }

        lock (_lock)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SubscriptionManager));
            }

            _unsubscribeActions.Add(unsubscribe);
        }
    }

    /// <summary>
    /// Subscribes to an event and tracks the subscription for automatic cleanup.
    /// </summary>
    /// <param name="subscribe">Action to perform subscription (should add the handler).</param>
    /// <param name="unsubscribe">Action to perform unsubscription (should remove the handler).</param>
    /// <exception cref="ArgumentNullException">Thrown when subscribe or unsubscribe is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when attempting to subscribe after disposal.</exception>
    /// <remarks>
    /// If the subscribe action throws an exception, the unsubscribe action is added before subscribing
    /// so that cleanup can be attempted even if the subscription fails. The unsubscribe action is removed
    /// only if the subscription completes successfully. If subscription fails, unsubscribe is called to
    /// clean up any partial state before rethrowing the exception.
    /// </remarks>
    public void Subscribe(Action subscribe, Action unsubscribe)
    {
        if (subscribe == null)
        {
            throw new ArgumentNullException(nameof(subscribe));
        }

        if (unsubscribe == null)
        {
            throw new ArgumentNullException(nameof(unsubscribe));
        }

        lock (_lock)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SubscriptionManager));
            }

            // Add the unsubscribe action before subscribing so that cleanup can be tracked
            _unsubscribeActions.Add(unsubscribe);
        }

        try
        {
            subscribe();
        }
        catch
        {
            // Remove the unsubscribe action since the subscription did not complete successfully
            // Use RemoveAt with Count-1 for O(1) removal since we just added it
            lock (_lock)
            {
                _unsubscribeActions.RemoveAt(_unsubscribeActions.Count - 1);
            }

            try
            {
                // Attempt to clean up any partial subscription state
                unsubscribe();
            }
            catch
            {
                // Swallow exceptions during cleanup; original exception will be rethrown
            }

            throw;
        }
    }

    /// <summary>
    /// Disposes the subscription manager and invokes all unsubscribe actions.
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        // Unsubscribe in reverse order (LIFO) to handle dependencies correctly
        // Note: We don't hold the lock while calling unsubscribe actions to avoid potential deadlocks
        Action[] actionsToDispose;
        lock (_lock)
        {
            actionsToDispose = _unsubscribeActions.ToArray();
        }

        for (int i = actionsToDispose.Length - 1; i >= 0; i--)
        {
            try
            {
                actionsToDispose[i]();
            }
            catch
            {
                // Swallow exceptions during cleanup to ensure all unsubscriptions are attempted
                // In production scenarios, you may want to log these exceptions
            }
        }

        lock (_lock)
        {
            _unsubscribeActions.Clear();
        }
    }
}
