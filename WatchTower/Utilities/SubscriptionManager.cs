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
/// </remarks>
public class SubscriptionManager : IDisposable
{
    private readonly List<Action> _unsubscribeActions = new();
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

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SubscriptionManager));
        }

        _unsubscribeActions.Add(unsubscribe);
    }

    /// <summary>
    /// Subscribes to an event and tracks the subscription for automatic cleanup.
    /// </summary>
    /// <param name="subscribe">Action to perform subscription (should add the handler).</param>
    /// <param name="unsubscribe">Action to perform unsubscription (should remove the handler).</param>
    /// <exception cref="ArgumentNullException">Thrown when subscribe or unsubscribe is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when attempting to subscribe after disposal.</exception>
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

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SubscriptionManager));
        }

        subscribe();
        _unsubscribeActions.Add(unsubscribe);
    }

    /// <summary>
    /// Disposes the subscription manager and invokes all unsubscribe actions.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Unsubscribe in reverse order (LIFO) to handle dependencies correctly
        for (int i = _unsubscribeActions.Count - 1; i >= 0; i--)
        {
            try
            {
                _unsubscribeActions[i]();
            }
            catch
            {
                // Swallow exceptions during cleanup to ensure all unsubscriptions are attempted
                // In production scenarios, you may want to log these exceptions
            }
        }

        _unsubscribeActions.Clear();
    }
}
