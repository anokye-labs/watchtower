using Xunit;
using WatchTower.Services;
using WatchTower.Tests.TestHelpers;
using AdaptiveCards;
using System;
using System.Collections.Generic;

namespace WatchTower.Tests.Services;

/// <summary>
/// Tests for AdaptiveCardService including card loading and action handling.
/// </summary>
public class AdaptiveCardServiceTests : ServiceTestBase
{
    private AdaptiveCardService CreateService()
    {
        var logger = CreateLogger<AdaptiveCardService>();
        return new AdaptiveCardService(logger);
    }
    
    [Fact]
    public void Constructor_CreatesInstance_Successfully()
    {
        // Act
        var service = CreateService();
        
        // Assert
        Assert.NotNull(service);
    }
    
    [Fact]
    public void CreateSampleCard_ReturnsValidCard()
    {
        // Arrange
        var service = CreateService();
        
        // Act
        var card = service.CreateSampleCard();
        
        // Assert
        Assert.NotNull(card);
        Assert.NotNull(card.Body);
        Assert.NotEmpty(card.Body);
    }
    
    [Fact]
    public void LoadCardFromJson_WithValidJson_ReturnsCard()
    {
        // Arrange
        var service = CreateService();
        var json = TestUtilities.CreateSampleAdaptiveCardJson();
        
        // Act
        var card = service.LoadCardFromJson(json);
        
        // Assert
        Assert.NotNull(card);
        Assert.NotNull(card.Body);
        Assert.NotEmpty(card.Body);
    }
    
    [Fact]
    public void LoadCardFromJson_WithInvalidJson_ReturnsNull()
    {
        // Arrange
        var service = CreateService();
        var invalidJson = "{ invalid json }";
        
        // Act
        var card = service.LoadCardFromJson(invalidJson);
        
        // Assert
        Assert.Null(card);
    }
    
    [Fact]
    public void LoadCardFromJson_WithEmptyString_ReturnsNull()
    {
        // Arrange
        var service = CreateService();
        
        // Act
        var card = service.LoadCardFromJson(string.Empty);
        
        // Assert
        Assert.Null(card);
    }
    
    [Fact]
    public void HandleAction_WithSubmitAction_RaisesSubmitActionEvent()
    {
        // Arrange
        var service = CreateService();
        var eventRaised = false;
        AdaptiveCardSubmitEventArgs? eventArgs = null;
        
        service.SubmitAction += (sender, args) =>
        {
            eventRaised = true;
            eventArgs = args;
        };
        
        var submitAction = new AdaptiveSubmitAction
        {
            Title = "Test Submit",
            Data = "test-data"
        };
        var inputValues = new Dictionary<string, object> { { "key", "value" } };
        
        // Act
        service.HandleAction(submitAction, inputValues);
        
        // Assert
        Assert.True(eventRaised);
        Assert.NotNull(eventArgs);
        Assert.Equal(submitAction, eventArgs.Action);
        Assert.NotEmpty(eventArgs.InputValues);
    }
    
    [Fact]
    public void HandleAction_WithOpenUrlAction_RaisesOpenUrlActionEvent()
    {
        // Arrange
        var service = CreateService();
        var eventRaised = false;
        AdaptiveCardActionEventArgs? eventArgs = null;
        
        service.OpenUrlAction += (sender, args) =>
        {
            eventRaised = true;
            eventArgs = args;
        };
        
        var openUrlAction = new AdaptiveOpenUrlAction
        {
            Title = "Open Link",
            Url = new Uri("https://example.com")
        };
        
        // Act
        service.HandleAction(openUrlAction);
        
        // Assert
        Assert.True(eventRaised);
        Assert.NotNull(eventArgs);
        Assert.Equal(openUrlAction, eventArgs.Action);
    }
    
    [Fact]
    public void HandleAction_WithExecuteAction_RaisesExecuteActionEvent()
    {
        // Arrange
        var service = CreateService();
        var eventRaised = false;
        AdaptiveCardActionEventArgs? eventArgs = null;
        
        service.ExecuteAction += (sender, args) =>
        {
            eventRaised = true;
            eventArgs = args;
        };
        
        var executeAction = new AdaptiveExecuteAction
        {
            Title = "Execute",
            Verb = "doSomething"
        };
        
        // Act
        service.HandleAction(executeAction);
        
        // Assert
        Assert.True(eventRaised);
        Assert.NotNull(eventArgs);
        Assert.Equal(executeAction, eventArgs.Action);
    }
    
    [Fact]
    public void HandleAction_WithShowCardAction_RaisesShowCardActionEvent()
    {
        // Arrange
        var service = CreateService();
        var eventRaised = false;
        AdaptiveCardActionEventArgs? eventArgs = null;
        
        service.ShowCardAction += (sender, args) =>
        {
            eventRaised = true;
            eventArgs = args;
        };
        
        var showCardAction = new AdaptiveShowCardAction
        {
            Title = "Show Card",
            Card = new AdaptiveCard("1.5")
        };
        
        // Act
        service.HandleAction(showCardAction);
        
        // Assert
        Assert.True(eventRaised);
        Assert.NotNull(eventArgs);
        Assert.Equal(showCardAction, eventArgs.Action);
    }
    
    [Fact]
    public void HandleAction_AlwaysRaisesActionInvokedEvent()
    {
        // Arrange
        var service = CreateService();
        var eventRaised = false;
        
        service.ActionInvoked += (sender, args) => eventRaised = true;
        
        var submitAction = new AdaptiveSubmitAction { Title = "Test" };
        
        // Act
        service.HandleAction(submitAction);
        
        // Assert
        Assert.True(eventRaised);
    }
    
    [Fact]
    public void HandleAction_WithMultipleSubscribers_NotifiesAll()
    {
        // Arrange
        var service = CreateService();
        var generalEventCount = 0;
        var specificEventCount = 0;
        
        service.ActionInvoked += (sender, args) => generalEventCount++;
        service.SubmitAction += (sender, args) => specificEventCount++;
        
        var submitAction = new AdaptiveSubmitAction { Title = "Test" };
        
        // Act
        service.HandleAction(submitAction);
        
        // Assert
        Assert.Equal(1, generalEventCount);
        Assert.Equal(1, specificEventCount);
    }
    
    [Fact]
    public void HandleAction_WithNullInputValues_DoesNotThrow()
    {
        // Arrange
        var service = CreateService();
        var submitAction = new AdaptiveSubmitAction { Title = "Test" };
        
        // Act & Assert
        var exception = Record.Exception(() => service.HandleAction(submitAction, null));
        Assert.Null(exception);
    }
    
    [Fact]
    public void LoadCardFromJson_WithComplexCard_ParsesSuccessfully()
    {
        // Arrange
        var service = CreateService();
        var json = @"{
            ""type"": ""AdaptiveCard"",
            ""version"": ""1.5"",
            ""body"": [
                {
                    ""type"": ""TextBlock"",
                    ""text"": ""Title"",
                    ""size"": ""Large"",
                    ""weight"": ""Bolder""
                },
                {
                    ""type"": ""Input.Text"",
                    ""id"": ""userInput"",
                    ""placeholder"": ""Enter text""
                }
            ],
            ""actions"": [
                {
                    ""type"": ""Action.Submit"",
                    ""title"": ""Submit""
                }
            ]
        }";
        
        // Act
        var card = service.LoadCardFromJson(json);
        
        // Assert
        Assert.NotNull(card);
        Assert.Equal(2, card.Body.Count);
        Assert.Single(card.Actions);
    }
}
