using AdaptiveCards;
using Microsoft.Extensions.Logging;
using WatchTower.Services;

namespace WatchTower.ViewModels;

/// <summary>
/// ViewModel for the MainWindow, managing the display of Adaptive Cards.
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    private readonly IAdaptiveCardService _cardService;
    private readonly ILogger<MainWindowViewModel> _logger;
    private AdaptiveCard? _currentCard;

    public MainWindowViewModel(IAdaptiveCardService cardService, ILogger<MainWindowViewModel> logger)
    {
        _cardService = cardService;
        _logger = logger;
        
        // Load the sample card on initialization
        LoadSampleCard();
    }

    /// <summary>
    /// Gets or sets the current Adaptive Card to display.
    /// </summary>
    public AdaptiveCard? CurrentCard
    {
        get => _currentCard;
        set => SetProperty(ref _currentCard, value);
    }

    /// <summary>
    /// Loads a sample Adaptive Card for display.
    /// </summary>
    private void LoadSampleCard()
    {
        _logger.LogInformation("Loading sample Adaptive Card");
        CurrentCard = _cardService.CreateSampleCard();
    }

    /// <summary>
    /// Loads an Adaptive Card from JSON string.
    /// </summary>
    /// <param name="cardJson">The JSON representation of the card.</param>
    public void LoadCardFromJson(string cardJson)
    {
        _logger.LogInformation("Loading Adaptive Card from JSON");
        var card = _cardService.LoadCardFromJson(cardJson);
        if (card != null)
        {
            CurrentCard = card;
        }
        else
        {
            _logger.LogWarning("Failed to load card from JSON");
        }
    }
}
