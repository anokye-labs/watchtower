using AdaptiveCards;
using System;
using System.Collections.Generic;

namespace WatchTower.Services;

/// <summary>
/// Event arguments for adaptive card action events.
/// </summary>
public class AdaptiveCardActionEventArgs : EventArgs
{
    /// <summary>
    /// The action that was invoked.
    /// </summary>
    public AdaptiveAction Action { get; }

    /// <summary>
    /// Optional data associated with the action.
    /// </summary>
    public object? Data { get; }

    public AdaptiveCardActionEventArgs(AdaptiveAction action, object? data = null)
    {
        Action = action;
        Data = data;
    }
}

/// <summary>
/// Event arguments for adaptive card submit actions.
/// </summary>
public class AdaptiveCardSubmitEventArgs : AdaptiveCardActionEventArgs
{
    /// <summary>
    /// The input values collected from the card.
    /// </summary>
    public Dictionary<string, object> InputValues { get; }

    public AdaptiveCardSubmitEventArgs(AdaptiveSubmitAction action, Dictionary<string, object> inputValues)
        : base(action, inputValues)
    {
        InputValues = inputValues;
    }
}

/// <summary>
/// Service interface for managing Adaptive Cards.
/// </summary>
public interface IAdaptiveCardService
{
    /// <summary>
    /// Loads an Adaptive Card from JSON string.
    /// </summary>
    /// <param name="cardJson">The JSON representation of the Adaptive Card.</param>
    /// <returns>The parsed AdaptiveCard object, or null if parsing fails.</returns>
    AdaptiveCard? LoadCardFromJson(string cardJson);

    /// <summary>
    /// Creates a sample Adaptive Card for demonstration purposes.
    /// </summary>
    /// <returns>A sample AdaptiveCard.</returns>
    AdaptiveCard CreateSampleCard();

    /// <summary>
    /// Handles an action invoked from an adaptive card.
    /// </summary>
    /// <param name="action">The action that was invoked.</param>
    /// <param name="inputValues">Optional input values from the card.</param>
    void HandleAction(AdaptiveAction action, Dictionary<string, object>? inputValues = null);

    /// <summary>
    /// Event raised when any action is invoked.
    /// </summary>
    event EventHandler<AdaptiveCardActionEventArgs>? ActionInvoked;

    /// <summary>
    /// Event raised when a submit action is invoked.
    /// </summary>
    event EventHandler<AdaptiveCardSubmitEventArgs>? SubmitAction;

    /// <summary>
    /// Event raised when an open URL action is invoked.
    /// </summary>
    event EventHandler<AdaptiveCardActionEventArgs>? OpenUrlAction;

    /// <summary>
    /// Event raised when an execute action is invoked.
    /// </summary>
    event EventHandler<AdaptiveCardActionEventArgs>? ExecuteAction;

    /// <summary>
    /// Event raised when a show card action is invoked.
    /// </summary>
    event EventHandler<AdaptiveCardActionEventArgs>? ShowCardAction;
}
