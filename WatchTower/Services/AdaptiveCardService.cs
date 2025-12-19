using AdaptiveCards;
using Microsoft.Extensions.Logging;

namespace WatchTower.Services;

/// <summary>
/// Service for managing Adaptive Cards including loading and creating cards.
/// </summary>
public class AdaptiveCardService : IAdaptiveCardService
{
    private readonly ILogger<AdaptiveCardService> _logger;

    public AdaptiveCardService(ILogger<AdaptiveCardService> logger)
    {
        _logger = logger;
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
                    HorizontalAlignment = AdaptiveHorizontalAlignment.Center,
                    Color = AdaptiveTextColor.Light
                },
                new AdaptiveTextBlock
                {
                    Text = "AdaptiveCard Display Engine",
                    Size = AdaptiveTextSize.Medium,
                    Weight = AdaptiveTextWeight.Lighter,
                    HorizontalAlignment = AdaptiveHorizontalAlignment.Center,
                    Spacing = AdaptiveSpacing.None,
                    Color = AdaptiveTextColor.Light
                },
                new AdaptiveTextBlock
                {
                    Text = "This demonstrates the core functionality of rendering Adaptive Cards in the WatchTower application.",
                    Wrap = true,
                    Spacing = AdaptiveSpacing.Medium,
                    Color = AdaptiveTextColor.Light
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
                                    Weight = AdaptiveTextWeight.Bolder,
                                    Color = AdaptiveTextColor.Light
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
                                    Color = AdaptiveTextColor.Good
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
