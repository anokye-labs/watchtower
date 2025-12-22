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
    public AdaptiveHostConfig CreateDarkHostConfig()
    {
        _logger.LogDebug("Creating dark theme HostConfig");
        
        var hostConfig = new AdaptiveHostConfig
        {
            ContainerStyles = new ContainerStylesConfig
            {
                Default = new ContainerStyleConfig
                {
                    BackgroundColor = "#2D2D2D",
                    ForegroundColors = new ForegroundColorsConfig
                    {
                        Default = new FontColorConfig
                        {
                            Default = "#FFFFFF",
                            Subtle = "#B0B0B0"
                        },
                        Dark = new FontColorConfig
                        {
                            Default = "#CCCCCC",
                            Subtle = "#999999"
                        },
                        Light = new FontColorConfig
                        {
                            Default = "#FFFFFF",
                            Subtle = "#EEEEEE"
                        },
                        Accent = new FontColorConfig
                        {
                            Default = "#0078D4",
                            Subtle = "#0063B1"
                        },
                        Good = new FontColorConfig
                        {
                            Default = "#92C353",
                            Subtle = "#7AA93C"
                        },
                        Warning = new FontColorConfig
                        {
                            Default = "#F2C811",
                            Subtle = "#D4AA00"
                        },
                        Attention = new FontColorConfig
                        {
                            Default = "#E74856",
                            Subtle = "#C42B1C"
                        }
                    }
                },
                Emphasis = new ContainerStyleConfig
                {
                    BackgroundColor = "#3A3A3A",
                    ForegroundColors = new ForegroundColorsConfig
                    {
                        Default = new FontColorConfig
                        {
                            Default = "#FFFFFF",
                            Subtle = "#B0B0B0"
                        }
                    }
                },
                Accent = new ContainerStyleConfig
                {
                    BackgroundColor = "#0078D4",
                    ForegroundColors = new ForegroundColorsConfig
                    {
                        Default = new FontColorConfig
                        {
                            Default = "#FFFFFF",
                            Subtle = "#E3E3E3"
                        }
                    }
                },
                Good = new ContainerStyleConfig
                {
                    BackgroundColor = "#92C353",
                    ForegroundColors = new ForegroundColorsConfig
                    {
                        Default = new FontColorConfig
                        {
                            Default = "#FFFFFF",
                            Subtle = "#E3E3E3"
                        }
                    }
                },
                Attention = new ContainerStyleConfig
                {
                    BackgroundColor = "#E74856",
                    ForegroundColors = new ForegroundColorsConfig
                    {
                        Default = new FontColorConfig
                        {
                            Default = "#FFFFFF",
                            Subtle = "#E3E3E3"
                        }
                    }
                },
                Warning = new ContainerStyleConfig
                {
                    BackgroundColor = "#F2C811",
                    ForegroundColors = new ForegroundColorsConfig
                    {
                        Default = new FontColorConfig
                        {
                            Default = "#000000",
                            Subtle = "#333333"
                        }
                    }
                }
            }
        };

        _logger.LogInformation("Dark theme HostConfig created successfully");
        return hostConfig;
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
                    HorizontalAlignment = AdaptiveHorizontalAlignment.Left
                },
                new AdaptiveTextBlock
                {
                    Text = "AdaptiveCard Display Engine",
                    Size = AdaptiveTextSize.Medium,
                    Weight = AdaptiveTextWeight.Lighter,
                    HorizontalAlignment = AdaptiveHorizontalAlignment.Left,
                    Spacing = AdaptiveSpacing.None
                },
                new AdaptiveTextBlock
                {
                    Text = "This demonstrates the core functionality of rendering Adaptive Cards in the WatchTower application.",
                    Wrap = true,
                    Spacing = AdaptiveSpacing.Medium
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
