using AdaptiveCards;

namespace WatchTower.Services;

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
    /// Creates a dark theme HostConfig for rendering Adaptive Cards.
    /// </summary>
    /// <returns>A configured AdaptiveHostConfig for dark theme.</returns>
    AdaptiveHostConfig CreateDarkHostConfig();
}
