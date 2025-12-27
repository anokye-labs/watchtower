using System;
using System.Collections.Generic;
using AdaptiveCards;
using Microsoft.Extensions.Logging;

namespace WatchTower.Services;

/// <summary>
/// Service for managing Adaptive Cards including loading and creating cards.
/// </summary>
public class AdaptiveCardService : IAdaptiveCardService
{
    private readonly ILogger<AdaptiveCardService> _logger;

    public event EventHandler<AdaptiveCardActionEventArgs>? ActionInvoked;
    public event EventHandler<AdaptiveCardSubmitEventArgs>? SubmitAction;
    public event EventHandler<AdaptiveCardActionEventArgs>? OpenUrlAction;
    public event EventHandler<AdaptiveCardActionEventArgs>? ExecuteAction;
    public event EventHandler<AdaptiveCardActionEventArgs>? ShowCardAction;

    public AdaptiveCardService(ILogger<AdaptiveCardService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public void HandleAction(AdaptiveAction action, Dictionary<string, object>? inputValues = null)
    {
        _logger.LogInformation("Adaptive card action invoked: {ActionType}", action.GetType().Name);

        // Raise the general action event
        var eventArgs = new AdaptiveCardActionEventArgs(action, inputValues);
        ActionInvoked?.Invoke(this, eventArgs);

        // Raise specific action events
        switch (action)
        {
            case AdaptiveSubmitAction submitAction:
                var submitArgs = new AdaptiveCardSubmitEventArgs(submitAction, inputValues ?? new Dictionary<string, object>());
                SubmitAction?.Invoke(this, submitArgs);
                _logger.LogDebug("Submit action handled with {Count} input values", inputValues?.Count ?? 0);
                break;

            case AdaptiveOpenUrlAction openUrlAction:
                OpenUrlAction?.Invoke(this, eventArgs);
                _logger.LogDebug("OpenUrl action handled: {Url}", openUrlAction.Url);
                break;

            case AdaptiveExecuteAction executeAction:
                ExecuteAction?.Invoke(this, eventArgs);
                _logger.LogDebug("Execute action handled: {Verb}", executeAction.Verb);
                break;

            case AdaptiveShowCardAction:
                ShowCardAction?.Invoke(this, eventArgs);
                _logger.LogDebug("ShowCard action handled");
                break;

            default:
                _logger.LogWarning("Unknown action type: {ActionType}", action.GetType().Name);
                break;
        }
    }

    /// <inheritdoc/>
    public AdaptiveCard? LoadCardFromJson(string cardJson)
    {
        try
        {
            _logger.LogDebug("Attempting to parse Adaptive Card from JSON");
            var parseResult = AdaptiveCard.FromJson(cardJson);
            
            if (parseResult.Card != null)
            {
                _logger.LogInformation("Successfully parsed Adaptive Card");
                return parseResult.Card;
            }
            
            _logger.LogWarning("Failed to parse Adaptive Card: Card is null");
            return null;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error parsing Adaptive Card from JSON");
            return null;
        }
    }

    /// <inheritdoc/>
    public AdaptiveCard CreateSampleCard()
    {
        _logger.LogDebug("Creating sample Adaptive Card");
        
        var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 5))
        {
            Body = new System.Collections.Generic.List<AdaptiveElement>
            {
                new AdaptiveTextBlock
                {
                    Text = "WatchTower",
                    Size = AdaptiveTextSize.ExtraLarge,
                    Weight = AdaptiveTextWeight.Bolder,
                    HorizontalAlignment = AdaptiveHorizontalAlignment.Center
                    // Color controlled by HostConfig
                },
                new AdaptiveTextBlock
                {
                    Text = "AdaptiveCard Display Engine",
                    Size = AdaptiveTextSize.Medium,
                    Weight = AdaptiveTextWeight.Lighter,
                    HorizontalAlignment = AdaptiveHorizontalAlignment.Center,
                    Spacing = AdaptiveSpacing.None,
                    IsSubtle = true
                    // Color controlled by HostConfig
                },
                new AdaptiveTextBlock
                {
                    Text = "This demonstrates the core functionality of rendering Adaptive Cards in the WatchTower application.",
                    Wrap = true,
                    Spacing = AdaptiveSpacing.Medium
                    // Color controlled by HostConfig
                },
                new AdaptiveColumnSet
                {
                    Columns = new System.Collections.Generic.List<AdaptiveColumn>
                    {
                        new AdaptiveColumn
                        {
                            Width = "auto",
                            Items = new System.Collections.Generic.List<AdaptiveElement>
                            {
                                new AdaptiveTextBlock
                                {
                                    Text = "Status:",
                                    Weight = AdaptiveTextWeight.Bolder
                                    // Color controlled by HostConfig
                                }
                            }
                        },
                        new AdaptiveColumn
                        {
                            Width = "stretch",
                            Items = new System.Collections.Generic.List<AdaptiveElement>
                            {
                                new AdaptiveTextBlock
                                {
                                    Text = "Ready",
                                    Color = AdaptiveTextColor.Good // Use semantic color
                                }
                            }
                        }
                    }
                }
            },
            Actions = new System.Collections.Generic.List<AdaptiveAction>
            {
                new AdaptiveOpenUrlAction
                {
                    Title = "Learn More",
                    Url = new System.Uri("https://adaptivecards.io/")
                }
            }
        };

        _logger.LogInformation("Sample Adaptive Card created successfully");
        return card;
    }
}
