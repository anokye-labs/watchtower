# Adaptive Card Full-Screen Theme System - Implementation Plan

## Executive Summary

This plan outlines the implementation of a full-screen adaptive card theme system for WatchTower with integrated gamepad navigation. The system will transform adaptive cards from embedded UI elements into immersive, full-screen experiences that align with the "Ancestral Futurism" design language while maintaining strict MVVM architecture and Windows-native focus.

## Design Goals

1. **Full-Screen Card Experiences** - Cards fill the entire content area with bottom-aligned action buttons (console UI pattern)
2. **Ancestral Futurism Theming** - Apply holographic cyan, Ashanti gold, mahogany, and void black color scheme
3. **Gamepad-First Navigation** - XYFocus integration with visual feedback for D-Pad/analog stick control
4. **MVVM Compliance** - All logic in ViewModels/Services, Views remain presentation-only
5. **Windows-Native** - Optimized for Windows with consistent behavior

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                         │
│  (ViewModels orchestrate services, handle user actions)     │
└────────────────┬────────────────────────────────────────────┘
                 │
┌────────────────┴────────────────────────────────────────────┐
│                    Service Layer                             │
│  ┌──────────────────────┐  ┌──────────────────────────┐    │
│  │ IAdaptiveCardTheme   │  │ IFocusNavigation         │    │
│  │ Service              │  │ Service                  │    │
│  │ - Generate HostConfig│  │ - XYFocus management     │    │
│  │ - Apply theme colors │  │ - Gamepad → Focus        │    │
│  │ - Custom fonts       │  │ - Visual indicators      │    │
│  └──────────────────────┘  └──────────────────────────┘    │
│  ┌──────────────────────┐  ┌──────────────────────────┐    │
│  │ IAdaptiveCardService │  │ IGameControllerService   │    │
│  │ - Load/create cards  │  │ - SDL2 input polling     │    │
│  │ - Action handling    │  │ - Button events          │    │
│  │ - Template library   │  │ - Analog stick state     │    │
│  └──────────────────────┘  └──────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
                 │
┌────────────────┴────────────────────────────────────────────┐
│                    View Layer                                │
│  - AdaptiveCardView with custom HostConfig                  │
│  - XYFocus-enabled containers                               │
│  - Custom Adinkra icon controls                             │
│  - Focus visual styles (holographic cyan glow)              │
└─────────────────────────────────────────────────────────────┘
```

## Implementation Phases

### Phase 1: Theme Infrastructure (Foundation)

**Goal:** Establish the theming system for adaptive cards with Ancestral Futurism design language.

#### 1.1 Create Theme Service Interface

**File:** `WatchTower/Services/IAdaptiveCardThemeService.cs`

```csharp
public interface IAdaptiveCardThemeService
{
    /// <summary>
    /// Generates a HostConfig with Ancestral Futurism theme applied.
    /// </summary>
    AdaptiveHostConfig GetHostConfig();
    
    /// <summary>
    /// Gets the current theme configuration.
    /// </summary>
    AdaptiveCardThemeConfig GetThemeConfig();
    
    /// <summary>
    /// Applies theme to an existing HostConfig.
    /// </summary>
    void ApplyTheme(AdaptiveHostConfig hostConfig);
}
```

#### 1.2 Implement Theme Service

**File:** `WatchTower/Services/AdaptiveCardThemeService.cs`

**Key Responsibilities:**
- Load theme configuration from `appsettings.json`
- Generate `AdaptiveHostConfig` with custom colors, fonts, spacing
- Apply Ancestral Futurism color palette:
  - **Foreground Colors:** Light text on dark backgrounds
  - **Container Styles:** Mahogany borders, void black backgrounds
  - **Accent Colors:** Holographic cyan for interactive elements
  - **Attention Colors:** Ashanti gold for important elements
  - **Warning Colors:** Alert amber (#FF8C00)
- Configure font families:
  - **Headers:** Rajdhani/Orbitron (bold, uppercase)
  - **Body:** Inter/Roboto (readable)
  - **Monospace:** JetBrains Mono/Fira Code (for data/logs)
- Set spacing and sizing for full-screen layouts

**Configuration Structure:**

```json
{
  "AdaptiveCardTheme": {
    "Colors": {
      "Default": {
        "Foreground": "#FFFFFF",
        "Background": "#050508",
        "Subtle": "#AAAAAA",
        "Accent": "#00F0FF",
        "Attention": "#FFD700",
        "Good": "#4AFF4A",
        "Warning": "#FF8C00",
        "Dark": "#4A1812"
      }
    },
    "Fonts": {
      "Default": {
        "FontFamily": "Inter, Roboto, sans-serif",
        "FontSizes": {
          "Small": 12,
          "Default": 14,
          "Medium": 16,
          "Large": 20,
          "ExtraLarge": 28
        },
        "FontWeights": {
          "Lighter": 200,
          "Default": 400,
          "Bolder": 700
        }
      },
      "Monospace": {
        "FontFamily": "JetBrains Mono, Fira Code, Consolas, monospace"
      }
    },
    "Spacing": {
      "Small": 4,
      "Default": 8,
      "Medium": 16,
      "Large": 24,
      "ExtraLarge": 40,
      "Padding": 20
    },
    "ContainerStyles": {
      "Default": {
        "BackgroundColor": "#1A050508",
        "BorderColor": "#4AFFFFFF",
        "BorderThickness": 1
      },
      "Emphasis": {
        "BackgroundColor": "#2A4A1812",
        "BorderColor": "#FFD700",
        "BorderThickness": 2
      }
    },
    "Actions": {
      "ShowCard": {
        "ActionMode": "Inline",
        "InlineTopMargin": 16
      },
      "ActionsOrientation": "Horizontal",
      "ActionAlignment": "Right",
      "ButtonSpacing": 8,
      "MaxActions": 5,
      "Spacing": "Default",
      "IconPlacement": "LeftOfTitle",
      "IconSize": 24
    }
  }
}
```

#### 1.3 Update Dependency Injection

**File:** `WatchTower/App.axaml.cs`

```csharp
// In ConfigureServices method
services.AddSingleton<IAdaptiveCardThemeService, AdaptiveCardThemeService>();
```

#### 1.4 Apply Theme to Card Rendering

**File:** `WatchTower/ViewModels/MainWindowViewModel.cs`

Update to use theme service when rendering cards:

```csharp
private readonly IAdaptiveCardThemeService _themeService;

public MainWindowViewModel(
    IAdaptiveCardService cardService,
    IAdaptiveCardThemeService themeService,
    ILogger<MainWindowViewModel> logger)
{
    _cardService = cardService;
    _themeService = themeService;
    _logger = logger;
}

// Apply theme when loading cards
private void ApplyThemeToRenderer()
{
    var hostConfig = _themeService.GetHostConfig();
    // Apply to AdaptiveCardView renderer
}
```

**File:** `WatchTower/Views/MainWindow.axaml`

Update AdaptiveCardView to use themed HostConfig:

```xml
<ac:AdaptiveCardView Card="{Binding CurrentCard}"
                     HostConfig="{Binding HostConfig}"
                     HorizontalAlignment="Stretch"
                     VerticalAlignment="Stretch" />
```

### Phase 2: Gamepad Navigation Integration

**Goal:** Enable full gamepad control of adaptive card elements with visual feedback.

#### 2.1 Create Focus Navigation Service Interface

**File:** `WatchTower/Services/IFocusNavigationService.cs`

```csharp
public interface IFocusNavigationService
{
    /// <summary>
    /// Initializes focus navigation for a container.
    /// </summary>
    void InitializeFocusScope(Control container);
    
    /// <summary>
    /// Moves focus in the specified direction.
    /// </summary>
    void MoveFocus(FocusNavigationDirection direction);
    
    /// <summary>
    /// Activates the currently focused element.
    /// </summary>
    void ActivateFocusedElement();
    
    /// <summary>
    /// Cancels/goes back from the current focus context.
    /// </summary>
    void CancelFocus();
    
    /// <summary>
    /// Gets the currently focused element.
    /// </summary>
    Control? GetFocusedElement();
    
    /// <summary>
    /// Event raised when focus changes.
    /// </summary>
    event EventHandler<FocusChangedEventArgs>? FocusChanged;
}

public enum FocusNavigationDirection
{
    Up,
    Down,
    Left,
    Right,
    Next,
    Previous
}
```

#### 2.2 Implement Focus Navigation Service

**File:** `WatchTower/Services/FocusNavigationService.cs`

**Key Responsibilities:**
- Integrate with Avalonia's XYFocus system
- Map gamepad inputs to focus navigation:
  - **D-Pad/Left Stick:** Move focus (Up/Down/Left/Right)
  - **A Button:** Activate focused element (click/submit)
  - **B Button:** Cancel/go back
  - **Y Button:** Alternative action (if available)
  - **Start Button:** Open menu/options
- Apply visual focus indicators (holographic cyan glow)
- Handle focus scope management for nested cards (Action.ShowCard)
- Provide smooth analog stick navigation with dead zone handling

**Implementation Notes:**
- Use Avalonia's `KeyboardNavigation.TabNavigation` and `KeyboardNavigation.DirectionalNavigation`
- Apply custom focus visual styles via attached properties
- Integrate with existing `GameControllerService` events

#### 2.3 Create Focus Visual Styles

**File:** `WatchTower/Styles/FocusStyles.axaml`

```xml
<Styles xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <!-- Gamepad Focus Indicator -->
    <Style Selector="Button:focus-visible">
        <Setter Property="BorderBrush" Value="#00F0FF"/>
        <Setter Property="BorderThickness" Value="2"/>
        <Setter Property="BoxShadow" Value="0 0 12 2 #8000F0FF"/>
    </Style>
    
    <Style Selector="TextBox:focus-visible">
        <Setter Property="BorderBrush" Value="#00F0FF"/>
        <Setter Property="BorderThickness" Value="2"/>
        <Setter Property="BoxShadow" Value="0 0 12 2 #8000F0FF"/>
    </Style>
    
    <!-- Adaptive Card Action Button Focus -->
    <Style Selector="ac|AdaptiveActionButton:focus-visible">
        <Setter Property="BorderBrush" Value="#00F0FF"/>
        <Setter Property="BorderThickness" Value="2"/>
        <Setter Property="BoxShadow" Value="0 0 16 4 #AA00F0FF"/>
    </Style>
    
    <!-- Focus animation (pulsing glow) -->
    <Style Selector="Button:focus-visible">
        <Style.Animations>
            <Animation Duration="0:0:1.5" 
                       IterationCount="Infinite"
                       PlaybackDirection="Alternate">
                <KeyFrame Cue="0%">
                    <Setter Property="BoxShadow" Value="0 0 12 2 #4000F0FF"/>
                </KeyFrame>
                <KeyFrame Cue="100%">
                    <Setter Property="BoxShadow" Value="0 0 16 4 #AA00F0FF"/>
                </KeyFrame>
            </Animation>
        </Style.Animations>
    </Style>
</Styles>
```

#### 2.4 Wire Gamepad Events to Focus Navigation

**File:** `WatchTower/ViewModels/MainWindowViewModel.cs`

```csharp
private readonly IFocusNavigationService _focusNavigation;
private readonly IGameControllerService _gameController;

private void OnButtonPressed(object? sender, GameControllerButtonEventArgs e)
{
    switch (e.Button)
    {
        case GameControllerButton.A:
            _focusNavigation.ActivateFocusedElement();
            break;
        case GameControllerButton.B:
            _focusNavigation.CancelFocus();
            break;
        case GameControllerButton.DPadUp:
            _focusNavigation.MoveFocus(FocusNavigationDirection.Up);
            break;
        case GameControllerButton.DPadDown:
            _focusNavigation.MoveFocus(FocusNavigationDirection.Down);
            break;
        case GameControllerButton.DPadLeft:
            _focusNavigation.MoveFocus(FocusNavigationDirection.Left);
            break;
        case GameControllerButton.DPadRight:
            _focusNavigation.MoveFocus(FocusNavigationDirection.Right);
            break;
        // ... more mappings
    }
}

// Handle analog stick for smooth navigation
private void OnControllerUpdate()
{
    var state = _gameController.GetControllerState(0);
    if (state != null)
    {
        // Threshold for analog stick navigation (e.g., 0.5)
        const float threshold = 0.5f;
        
        if (Math.Abs(state.LeftStickY) > threshold)
        {
            var direction = state.LeftStickY > 0 
                ? FocusNavigationDirection.Up 
                : FocusNavigationDirection.Down;
            _focusNavigation.MoveFocus(direction);
        }
        
        if (Math.Abs(state.LeftStickX) > threshold)
        {
            var direction = state.LeftStickX > 0 
                ? FocusNavigationDirection.Right 
                : FocusNavigationDirection.Left;
            _focusNavigation.MoveFocus(direction);
        }
    }
}
```

### Phase 3: Adaptive Card Action Handling

**Goal:** Wire adaptive card actions (Submit, OpenUrl, Execute, ShowCard) to MVVM commands.

#### 3.1 Create Action Event Models

**File:** `WatchTower/Models/AdaptiveCardActionEventArgs.cs`

```csharp
public class AdaptiveCardActionEventArgs : EventArgs
{
    public AdaptiveAction Action { get; }
    public object? Data { get; }
    
    public AdaptiveCardActionEventArgs(AdaptiveAction action, object? data = null)
    {
        Action = action;
        Data = data;
    }
}

public class AdaptiveCardSubmitEventArgs : AdaptiveCardActionEventArgs
{
    public Dictionary<string, object> InputValues { get; }
    
    public AdaptiveCardSubmitEventArgs(
        AdaptiveSubmitAction action, 
        Dictionary<string, object> inputValues)
        : base(action, inputValues)
    {
        InputValues = inputValues;
    }
}
```

#### 3.2 Extend Adaptive Card Service

**File:** `WatchTower/Services/IAdaptiveCardService.cs`

```csharp
public interface IAdaptiveCardService
{
    // Existing methods...
    AdaptiveCard? LoadCardFromJson(string cardJson);
    AdaptiveCard CreateSampleCard();
    
    // New action handling
    event EventHandler<AdaptiveCardActionEventArgs>? ActionInvoked;
    event EventHandler<AdaptiveCardSubmitEventArgs>? SubmitAction;
    event EventHandler<AdaptiveCardActionEventArgs>? OpenUrlAction;
    event EventHandler<AdaptiveCardActionEventArgs>? ExecuteAction;
    event EventHandler<AdaptiveCardActionEventArgs>? ShowCardAction;
    
    /// <summary>
    /// Handles action invocation from the renderer.
    /// </summary>
    void HandleAction(AdaptiveAction action, object? data = null);
}
```

#### 3.3 Implement Action Handling

**File:** `WatchTower/Services/AdaptiveCardService.cs`

```csharp
public event EventHandler<AdaptiveCardActionEventArgs>? ActionInvoked;
public event EventHandler<AdaptiveCardSubmitEventArgs>? SubmitAction;
public event EventHandler<AdaptiveCardActionEventArgs>? OpenUrlAction;
public event EventHandler<AdaptiveCardActionEventArgs>? ExecuteAction;
public event EventHandler<AdaptiveCardActionEventArgs>? ShowCardAction;

public void HandleAction(AdaptiveAction action, object? data = null)
{
    _logger.LogInformation("Adaptive card action invoked: {ActionType}", action.Type);
    
    var eventArgs = new AdaptiveCardActionEventArgs(action, data);
    ActionInvoked?.Invoke(this, eventArgs);
    
    switch (action)
    {
        case AdaptiveSubmitAction submitAction:
            var submitArgs = new AdaptiveCardSubmitEventArgs(
                submitAction, 
                data as Dictionary<string, object> ?? new());
            SubmitAction?.Invoke(this, submitArgs);
            break;
            
        case AdaptiveOpenUrlAction openUrlAction:
            OpenUrlAction?.Invoke(this, eventArgs);
            break;
            
        case AdaptiveExecuteAction executeAction:
            ExecuteAction?.Invoke(this, eventArgs);
            break;
            
        case AdaptiveShowCardAction showCardAction:
            ShowCardAction?.Invoke(this, eventArgs);
            break;
    }
}
```

#### 3.4 Wire Actions in ViewModel

**File:** `WatchTower/ViewModels/MainWindowViewModel.cs`

```csharp
private void SubscribeToCardActions()
{
    _cardService.SubmitAction += OnCardSubmit;
    _cardService.OpenUrlAction += OnCardOpenUrl;
    _cardService.ExecuteAction += OnCardExecute;
    _cardService.ShowCardAction += OnCardShowCard;
}

private void OnCardSubmit(object? sender, AdaptiveCardSubmitEventArgs e)
{
    _logger.LogInformation("Card submitted with {Count} inputs", e.InputValues.Count);
    
    // Process submitted data
    foreach (var kvp in e.InputValues)
    {
        _logger.LogDebug("Input: {Key} = {Value}", kvp.Key, kvp.Value);
    }
    
    // Execute business logic based on submission
    // e.g., save data, trigger workflow, navigate to next card
}

private void OnCardOpenUrl(object? sender, AdaptiveCardActionEventArgs e)
{
    if (e.Action is AdaptiveOpenUrlAction openUrlAction)
    {
        _logger.LogInformation("Opening URL: {Url}", openUrlAction.Url);
        // Open URL in browser or handle internally
    }
}

private void OnCardExecute(object? sender, AdaptiveCardActionEventArgs e)
{
    if (e.Action is AdaptiveExecuteAction executeAction)
    {
        _logger.LogInformation("Executing action: {Verb}", executeAction.Verb);
        // Execute custom command based on verb
    }
}

private void OnCardShowCard(object? sender, AdaptiveCardActionEventArgs e)
{
    if (e.Action is AdaptiveShowCardAction showCardAction)
    {
        _logger.LogInformation("Showing nested card");
        // Handle nested card display (inline or modal)
    }
}
```

### Phase 4: Full-Screen Card Layout Patterns

**Goal:** Create reusable card templates optimized for full-screen display with bottom-aligned actions.

#### 4.1 Create Template Library Service

**File:** `WatchTower/Services/AdaptiveCardTemplates.cs`

```csharp
public static class AdaptiveCardTemplates
{
    /// <summary>
    /// Full-screen card with header, content area, and bottom action bar.
    /// </summary>
    public static AdaptiveCard CreateFullScreenTemplate(
        string title,
        string subtitle,
        List<AdaptiveElement> contentElements,
        List<AdaptiveAction> actions)
    {
        var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 5))
        {
            Body = new List<AdaptiveElement>
            {
                // Header section
                new AdaptiveContainer
                {
                    Items = new List<AdaptiveElement>
                    {
                        new AdaptiveTextBlock
                        {
                            Text = title,
                            Size = AdaptiveTextSize.ExtraLarge,
                            Weight = AdaptiveTextWeight.Bolder,
                            Color = AdaptiveTextColor.Accent,
                            HorizontalAlignment = AdaptiveHorizontalAlignment.Center
                        },
                        new AdaptiveTextBlock
                        {
                            Text = subtitle,
                            Size = AdaptiveTextSize.Medium,
                            Weight = AdaptiveTextWeight.Lighter,
                            Color = AdaptiveTextColor.Default,
                            HorizontalAlignment = AdaptiveHorizontalAlignment.Center,
                            Spacing = AdaptiveSpacing.None
                        }
                    },
                    Style = AdaptiveContainerStyle.Emphasis,
                    Bleed = true
                },
                
                // Spacer
                new AdaptiveContainer
                {
                    Height = AdaptiveHeight.Auto,
                    MinHeight = 20
                },
                
                // Content area (scrollable)
                new AdaptiveContainer
                {
                    Items = contentElements,
                    Style = AdaptiveContainerStyle.Default
                }
            },
            Actions = actions,
            VerticalContentAlignment = AdaptiveVerticalContentAlignment.Top
        };
        
        return card;
    }
    
    /// <summary>
    /// Council Chamber view - radial agent layout.
    /// </summary>
    public static AdaptiveCard CreateCouncilChamberCard()
    {
        // Implementation for circular agent visualization
    }
    
    /// <summary>
    /// Treasury Dashboard - resource metrics.
    /// </summary>
    public static AdaptiveCard CreateTreasuryDashboardCard()
    {
        // Implementation for resource gauges and charts
    }
    
    /// <summary>
    /// Linguist Interface - dual-pane chat view.
    /// </summary>
    public static AdaptiveCard CreateLinguistInterfaceCard()
    {
        // Implementation for protocol/translation split view
    }
    
    /// <summary>
    /// Asafo Deployment Grid - tactical map.
    /// </summary>
    public static AdaptiveCard CreateAsafoDeploymentCard()
    {
        // Implementation for agent company status grid
    }
    
    /// <summary>
    /// Spiritual Shield - system health dashboard.
    /// </summary>
    public static AdaptiveCard CreateSpiritualShieldCard()
    {
        // Implementation for security/health monitoring
    }
}
```

#### 4.2 Example Full-Screen Card JSON

**File:** `WatchTower/Assets/Cards/council-chamber-example.json`

```json
{
  "type": "AdaptiveCard",
  "version": "1.5",
  "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
  "body": [
    {
      "type": "Container",
      "style": "emphasis",
      "bleed": true,
      "items": [
        {
          "type": "TextBlock",
          "text": "COUNCIL SESSION: IN PROGRESS",
          "size": "extraLarge",
          "weight": "bolder",
          "color": "accent",
          "horizontalAlignment": "center"
        },
        {
          "type": "Image",
          "url": "avares://WatchTower/Assets/Adinkra/sika-dwa.png",
          "size": "small",
          "horizontalAlignment": "center",
          "spacing": "none"
        }
      ]
    },
    {
      "type": "Container",
      "minHeight": "20px"
    },
    {
      "type": "Container",
      "items": [
        {
          "type": "TextBlock",
          "text": "Active Agents",
          "size": "large",
          "weight": "bolder",
          "color": "attention"
        },
        {
          "type": "ColumnSet",
          "columns": [
            {
              "type": "Column",
              "width": "auto",
              "items": [
                {
                  "type": "Image",
                  "url": "avares://WatchTower/Assets/Adinkra/gye-nyame.png",
                  "size": "small"
                }
              ]
            },
            {
              "type": "Column",
              "width": "stretch",
              "items": [
                {
                  "type": "TextBlock",
                  "text": "Security Agent",
                  "weight": "bolder"
                },
                {
                  "type": "TextBlock",
                  "text": "Status: Active | Consensus: 85%",
                  "size": "small",
                  "color": "good",
                  "spacing": "none"
                }
              ]
            }
          ]
        },
        {
          "type": "ColumnSet",
          "columns": [
            {
              "type": "Column",
              "width": "auto",
              "items": [
                {
                  "type": "Image",
                  "url": "avares://WatchTower/Assets/Adinkra/dwennimmen.png",
                  "size": "small"
                }
              ]
            },
            {
              "type": "Column",
              "width": "stretch",
              "items": [
                {
                  "type": "TextBlock",
                  "text": "Warrior Agent",
                  "weight": "bolder"
                },
                {
                  "type": "TextBlock",
                  "text": "Status: Deploying | Consensus: 92%",
                  "size": "small",
                  "color": "good",
                  "spacing": "none"
                }
              ]
            }
          ]
        }
      ]
    },
    {
      "type": "Container",
      "minHeight": "20px"
    },
    {
      "type": "Container",
      "items": [
        {
          "type": "TextBlock",
          "text": "Consensus Strength",
          "weight": "bolder"
        },
        {
          "type": "ProgressBar",
          "value": 88,
          "color": "accent"
        }
      ]
    }
  ],
  "actions": [
    {
      "type": "Action.Submit",
      "title": "Approve Decision",
      "style": "positive",
      "data": {
        "action": "approve"
      }
    },
    {
      "type": "Action.ShowCard",
      "title": "View Dissent Logs",
      "card": {
        "type": "AdaptiveCard",
        "body": [
          {
            "type": "TextBlock",
            "text": "Dissenting Opinions",
            "weight": "bolder"
          },
          {
            "type": "TextBlock",
            "text": "Agent 3: Recommends alternative approach...",
            "wrap": true
          }
        ]
      }
    },
    {
      "type": "Action.Execute",
      "title": "Pause Session",
      "verb": "pause",
      "data": {
        "sessionId": "council-001"
      }
    }
  ]
}
```

### Phase 5: Custom Adinkra Elements

**Goal:** Integrate Adinkra symbols as custom adaptive card elements and icons.

#### 5.1 Create Adinkra Icon Control

**File:** `WatchTower/Controls/AdinkraIcon.cs`

```csharp
public class AdinkraIcon : Control
{
    public static readonly StyledProperty<AdinkraSymbol> SymbolProperty =
        AvaloniaProperty.Register<AdinkraIcon, AdinkraSymbol>(nameof(Symbol));
    
    public static readonly StyledProperty<double> SizeProperty =
        AvaloniaProperty.Register<AdinkraIcon, double>(nameof(Size), 24.0);
    
    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<AdinkraIcon, IBrush?>(nameof(Foreground));
    
    public AdinkraSymbol Symbol
    {
        get => GetValue(SymbolProperty);
        set => SetValue(SymbolProperty, value);
    }
    
    public double Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }
    
    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }
    
    public override void Render(DrawingContext context)
    {
        // Render Adinkra symbol as vector path or load from asset
        var symbolPath = GetSymbolPath(Symbol);
        // ... rendering logic
    }
    
    private Geometry GetSymbolPath(AdinkraSymbol symbol)
    {
        // Return vector path for symbol
        return symbol switch
        {
            AdinkraSymbol.GyeNyame => LoadSymbolGeometry("gye-nyame"),
            AdinkraSymbol.Sankofa => LoadSymbolGeometry("sankofa"),
            AdinkraSymbol.Dwennimmen => LoadSymbolGeometry("dwennimmen"),
            // ... more symbols
            _ => Geometry.Empty
        };
    }
}

public enum AdinkraSymbol
{
    GyeNyame,      // Security/System Health
    Sankofa,       // History/Logs/Undo
    Dwennimmen,    // Strength/Conflict
    BiNkaBi,       // Harmony/Consensus
    DameDame,      // Strategy/Intelligence
    Poma,          // Staff/Authority
    AdakaKese,     // Treasury/Resources
    SikaDwa,       // Council/Leadership
    Frankaa        // Deployment/Companies
}
```

#### 5.2 Register Custom Element with Renderer

**File:** `WatchTower/Services/AdaptiveCardRendererExtensions.cs`

```csharp
public static class AdaptiveCardRendererExtensions
{
    public static void RegisterAdinkraElements(this AdaptiveCardRenderer renderer)
    {
        // Register custom element type
        renderer.ElementRenderers.Set("AdinkraIcon", (element, context) =>
        {
            if (element is AdaptiveCustomElement customElement)
            {
                var symbolName = customElement.Properties["symbol"]?.ToString();
                var size = customElement.Properties.TryGetValue("size", out var sizeValue)
                    ? Convert.ToDouble(sizeValue)
                    : 24.0;
                
                var icon = new AdinkraIcon
                {
                    Symbol = ParseSymbol(symbolName),
                    Size = size,
                    Foreground = context.ForegroundColors.Default.Default
                };
                
                return icon;
            }
            
            return null;
        });
    }
    
    private static AdinkraSymbol ParseSymbol(string? name)
    {
        return name?.ToLowerInvariant() switch
        {
            "gye-nyame" => AdinkraSymbol.GyeNyame,
            "sankofa" => AdinkraSymbol.Sankofa,
            "dwennimmen" => AdinkraSymbol.Dwennimmen,
            // ... more mappings
            _ => AdinkraSymbol.GyeNyame
        };
    }
}
```

#### 5.3 Usage in Adaptive Card JSON

```json
{
  "type": "AdaptiveCard",
  "version": "1.5",
  "body": [
    {
      "type": "AdinkraIcon",
      "symbol": "gye-nyame",
      "size": 48,
      "horizontalAlignment": "center"
    },
    {
      "type": "TextBlock",
      "text": "System Health: Excellent",
      "horizontalAlignment": "center"
    }
  ]
}
```

### Phase 6: Configuration and Testing

#### 6.1 Update appsettings.json

**File:** `WatchTower/appsettings.json`

Add complete theme configuration (see Phase 1.2 for full structure).

#### 6.2 Create Unit Tests

**File:** `WatchTower.Tests/Services/AdaptiveCardThemeServiceTests.cs`

```csharp
public class AdaptiveCardThemeServiceTests
{
    [Fact]
    public void GetHostConfig_ReturnsConfigWithAncestralFuturismColors()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var logger = Mock.Of<ILogger<AdaptiveCardThemeService>>();
        var service = new AdaptiveCardThemeService(logger, config);
        
        // Act
        var hostConfig = service.GetHostConfig();
        
        // Assert
        Assert.Equal("#00F0FF", hostConfig.ContainerStyles.Default.ForegroundColors.Accent.Default);
        Assert.Equal("#FFD700", hostConfig.ContainerStyles.Default.ForegroundColors.Attention.Default);
    }
    
    [Fact]
    public void ApplyTheme_UpdatesExistingHostConfig()
    {
        // Test theme application to existing config
    }
}
```

**File:** `WatchTower.Tests/Services/FocusNavigationServiceTests.cs`

```csharp
public class FocusNavigationServiceTests
{
    [Fact]
    public void MoveFocus_WithUpDirection_MovesFocusToAboveElement()
    {
        // Test focus navigation logic
    }
    
    [Fact]
    public void ActivateFocusedElement_InvokesButtonClick()
    {
        // Test element activation
    }
}
```

#### 6.3 Create Integration Tests

**File:** `WatchTower.Tests/Integration/AdaptiveCardGamepadNavigationTests.cs`

```csharp
[Collection("Avalonia")]
public class AdaptiveCardGamepadNavigationTests
{
    [Fact]
    public async Task GamepadDPadDown_MovesCardFocus()
    {
        // Arrange
        using var app = AvaloniaApp.GetApp();
        var window = new MainWindow();
        var viewModel = new MainWindowViewModel(/* dependencies */);
        window.DataContext = viewModel;
        
        // Act
        // Simulate gamepad D-Pad down press
        await SimulateGamepadInput(GameControllerButton.DPadDown);
        
        // Assert
        // Verify focus moved to next element
    }
}
```

#### 6.4 Create Documentation

**File:** `docs/adaptive-card-theme-system.md`

Comprehensive documentation covering:
- Theme configuration reference
- Gamepad navigation patterns
- Custom element creation guide
- Card template library
- Action handling examples
- Troubleshooting guide

## Implementation Timeline

### Week 1: Foundation
- [ ] Phase 1.1-1.4: Theme infrastructure
- [ ] Basic HostConfig generation
- [ ] Configuration structure
- [ ] Initial testing

### Week 2: Navigation
- [ ] Phase 2.1-2.2: Focus navigation service
- [ ] Phase 2.3: Visual styles
- [ ] Phase 2.4: Gamepad integration
- [ ] Navigation testing

### Week 3: Actions & Templates
- [ ] Phase 3.1-3.4: Action handling
- [ ] Phase 4.1-4.2: Template library
- [ ] Example cards for each view type
- [ ] Action testing

### Week 4: Custom Elements & Polish
- [ ] Phase 5.1-5.3: Adinkra elements
- [ ] Phase 6: Testing and documentation
- [ ] Performance optimization
- [ ] Cross-platform validation

## Technical Considerations

### 1. HostConfig Limitations

**Challenge:** Iciclecreek.AdaptiveCards.Rendering.Avalonia may have limited HostConfig support compared to official renderers.

**Solution:**
- Test HostConfig properties incrementally
- Fall back to custom styles for unsupported properties
- Consider contributing improvements to the library

### 2. XYFocus in Avalonia

**Challenge:** Avalonia's XYFocus system may differ from UWP/WinUI implementations.

**Solution:**
- Use `KeyboardNavigation.TabNavigation` and `KeyboardNavigation.DirectionalNavigation` attached properties
- Implement custom focus logic if needed
- Test thoroughly on all platforms

### 3. Analog Stick Navigation

**Challenge:** Smooth analog stick navigation requires debouncing and threshold handling.

**Solution:**
- Implement rate limiting (e.g., max one navigation event per 200ms)
- Use dead zone threshold (0.5) to prevent accidental navigation
- Provide visual feedback during navigation

### 4. Action Button Layout

**Challenge:** Bottom-aligned action buttons may require custom container rendering.

**Solution:**
- Use `VerticalContentAlignment` in card
- Apply custom styles to `ActionSet` container
- Consider custom action bar control if needed

### 5. Performance with Complex Cards

**Challenge:** Full-screen cards with many elements may impact render performance.

**Solution:**
- Implement virtualization for list-based content
- Lazy-load images and heavy elements
- Profile with Avalonia DevTools
- Set performance budgets (e.g., <16ms render time)

### 6. Font Availability

**Challenge:** Custom fonts (Rajdhani, Orbitron) may need to be embedded for consistent rendering.

**Solution:**
- Embed fonts as resources in the application
- Provide fallback font stacks
- Test font rendering on Windows

## Success Metrics

1. **Theme Consistency:** All adaptive cards render with Ancestral Futurism colors and fonts
2. **Gamepad Navigation:** 100% of interactive elements are gamepad-accessible
3. **Performance:** Card rendering completes in <100ms for typical cards
4. **Test Coverage:** >80% coverage for theme and navigation services
5. **Windows-Native:** Optimized behavior on Windows
6. **Accessibility:** Keyboard-only navigation works alongside gamepad

## Future Enhancements

1. **Dynamic Theme Switching:** Allow runtime theme changes (light/dark modes)
2. **Card Animation:** Smooth transitions between cards
3. **Voice Input Integration:** Voice commands for card actions
4. **Haptic Feedback:** Controller vibration for important actions
5. **Card Designer Tool:** Visual editor for creating custom cards
6. **Template Marketplace:** Share and download community card templates
7. **Localization:** Multi-language support for card content
8. **Analytics:** Track card interaction patterns and performance

## References

- [Adaptive Cards Schema](https://adaptivecards.io/explorer/)
- [Adaptive Cards Host Config](https://docs.microsoft.com/en-us/adaptive-cards/rendering-cards/host-config)
- [Avalonia XYFocus Documentation](https://docs.avaloniaui.net/docs/input/keyboard)
- [Iciclecreek.AdaptiveCards.Rendering.Avalonia](https://github.com/tomlm/Iciclecreek.AdaptiveCards.Rendering.Avalonia)
- [WatchTower Design Language](../concept-art/design-language.md)
- [WatchTower Game Controller Support](./game-controller-support.md)

---

**Document Version:** 1.0.0  
**Last Updated:** 2024-12-24  
**Status:** Planning Phase  
**Owner:** WatchTower Development Team
