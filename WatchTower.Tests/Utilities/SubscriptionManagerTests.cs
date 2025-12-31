using System;
using Xunit;
using WatchTower.Utilities;

namespace WatchTower.Tests.Utilities;

public class SubscriptionManagerTests
{
    [Fact]
    public void Add_WithValidAction_AddsActionToList()
    {
        // Arrange
        var manager = new SubscriptionManager();
        var unsubscribeCalled = false;

        // Act
        manager.Add(() => unsubscribeCalled = true);
        manager.Dispose();

        // Assert
        Assert.True(unsubscribeCalled);
    }

    [Fact]
    public void Add_WithNullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = new SubscriptionManager();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => manager.Add(null!));
    }

    [Fact]
    public void Add_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var manager = new SubscriptionManager();
        manager.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => manager.Add(() => { }));
    }

    [Fact]
    public void Subscribe_WithValidActions_ExecutesSubscribeAndTracksUnsubscribe()
    {
        // Arrange
        var manager = new SubscriptionManager();
        var subscribeCalled = false;
        var unsubscribeCalled = false;

        // Act
        manager.Subscribe(
            subscribe: () => subscribeCalled = true,
            unsubscribe: () => unsubscribeCalled = true
        );

        // Assert
        Assert.True(subscribeCalled);
        Assert.False(unsubscribeCalled);

        // Dispose and verify unsubscribe
        manager.Dispose();
        Assert.True(unsubscribeCalled);
    }

    [Fact]
    public void Subscribe_WithNullSubscribe_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = new SubscriptionManager();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            manager.Subscribe(null!, () => { }));
    }

    [Fact]
    public void Subscribe_WithNullUnsubscribe_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = new SubscriptionManager();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            manager.Subscribe(() => { }, null!));
    }

    [Fact]
    public void Subscribe_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var manager = new SubscriptionManager();
        manager.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => 
            manager.Subscribe(() => { }, () => { }));
    }

    [Fact]
    public void Dispose_WithMultipleSubscriptions_UnsubscribesInReverseOrder()
    {
        // Arrange
        var manager = new SubscriptionManager();
        var unsubscribeOrder = new System.Collections.Generic.List<int>();

        manager.Add(() => unsubscribeOrder.Add(1));
        manager.Add(() => unsubscribeOrder.Add(2));
        manager.Add(() => unsubscribeOrder.Add(3));

        // Act
        manager.Dispose();

        // Assert - should unsubscribe in LIFO order (3, 2, 1)
        Assert.Equal(new[] { 3, 2, 1 }, unsubscribeOrder);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_OnlyExecutesOnce()
    {
        // Arrange
        var manager = new SubscriptionManager();
        var unsubscribeCallCount = 0;

        manager.Add(() => unsubscribeCallCount++);

        // Act
        manager.Dispose();
        manager.Dispose();
        manager.Dispose();

        // Assert
        Assert.Equal(1, unsubscribeCallCount);
    }

    [Fact]
    public void Dispose_WithExceptionInUnsubscribe_ContinuesWithOtherUnsubscriptions()
    {
        // Arrange
        var manager = new SubscriptionManager();
        var firstUnsubscribeCalled = false;
        var thirdUnsubscribeCalled = false;

        manager.Add(() => firstUnsubscribeCalled = true);
        manager.Add(() => throw new InvalidOperationException("Test exception"));
        manager.Add(() => thirdUnsubscribeCalled = true);

        // Act
        manager.Dispose();

        // Assert - both first and third should be called despite exception in second
        Assert.True(firstUnsubscribeCalled);
        Assert.True(thirdUnsubscribeCalled);
    }

    [Fact]
    public void RealWorldScenario_EventSubscriptionPattern_WorksCorrectly()
    {
        // Arrange - simulate a typical event subscription scenario
        var eventSource = new TestEventSource();
        var manager = new SubscriptionManager();
        var eventReceivedCount = 0;

        void OnEventReceived(object? sender, EventArgs e) => eventReceivedCount++;

        // Act - subscribe using the manager
        manager.Subscribe(
            subscribe: () => eventSource.TestEvent += OnEventReceived,
            unsubscribe: () => eventSource.TestEvent -= OnEventReceived
        );

        // Raise event and verify it's received
        eventSource.RaiseEvent();
        Assert.Equal(1, eventReceivedCount);

        // Dispose and raise event again
        manager.Dispose();
        eventSource.RaiseEvent();

        // Assert - event should not be received after disposal
        Assert.Equal(1, eventReceivedCount);
    }

    [Fact]
    public void RealWorldScenario_MultipleEventSources_AllUnsubscribeOnDispose()
    {
        // Arrange
        var eventSource1 = new TestEventSource();
        var eventSource2 = new TestEventSource();
        var manager = new SubscriptionManager();
        var event1Count = 0;
        var event2Count = 0;

        void OnEvent1(object? sender, EventArgs e) => event1Count++;
        void OnEvent2(object? sender, EventArgs e) => event2Count++;

        // Act - subscribe to multiple event sources
        manager.Subscribe(
            subscribe: () => eventSource1.TestEvent += OnEvent1,
            unsubscribe: () => eventSource1.TestEvent -= OnEvent1
        );

        manager.Subscribe(
            subscribe: () => eventSource2.TestEvent += OnEvent2,
            unsubscribe: () => eventSource2.TestEvent -= OnEvent2
        );

        // Raise events and verify they're received
        eventSource1.RaiseEvent();
        eventSource2.RaiseEvent();
        Assert.Equal(1, event1Count);
        Assert.Equal(1, event2Count);

        // Dispose and raise events again
        manager.Dispose();
        eventSource1.RaiseEvent();
        eventSource2.RaiseEvent();

        // Assert - events should not be received after disposal
        Assert.Equal(1, event1Count);
        Assert.Equal(1, event2Count);
    }

    // Helper class for testing event subscriptions
    private class TestEventSource
    {
        public event EventHandler? TestEvent;

        public void RaiseEvent()
        {
            TestEvent?.Invoke(this, EventArgs.Empty);
        }
    }
}
