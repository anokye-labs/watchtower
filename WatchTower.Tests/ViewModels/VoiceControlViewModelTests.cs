using Moq;
using WatchTower.Models;
using WatchTower.Services;
using WatchTower.Tests.TestHelpers;
using WatchTower.ViewModels;
using Xunit;

namespace WatchTower.Tests.ViewModels;

/// <summary>
/// Tests for VoiceControlViewModel, focusing on event unsubscription regression tests.
/// </summary>
public class VoiceControlViewModelTests
{
    [Fact]
    public void TestInfrastructure_WhenRunning_IsConfiguredCorrectly()
    {
        // This is a placeholder test to verify test infrastructure works
        Assert.True(true);
    }

    #region Event Unsubscription Regression Tests

    [Fact]
    public void Dispose_UnsubscribesFromStateChangedEvent()
    {
        // Arrange
        var mockVoiceService = ServiceMocks.CreateVoiceOrchestrationService();
        var viewModel = new VoiceControlViewModel(mockVoiceService.Object);

        var initialIsListening = viewModel.IsListening;
        var initialIsSpeaking = viewModel.IsSpeaking;

        // Act - Dispose the ViewModel
        viewModel.Dispose();

        // Raise the event after disposal
        var newState = new VoiceState
        {
            IsListening = true,
            IsSpeaking = true,
            IsInitialized = true,
            VoiceActivityDetected = true,
            InputLevel = 0.8f
        };
        mockVoiceService.Raise(s => s.StateChanged += null,
            new VoiceStateChangedEventArgs(newState));

        // Assert - Properties should not have changed after disposal
        Assert.Equal(initialIsListening, viewModel.IsListening);
        Assert.Equal(initialIsSpeaking, viewModel.IsSpeaking);
    }

    [Fact]
    public void Dispose_UnsubscribesFromSpeechRecognizedEvent()
    {
        // Arrange
        var mockVoiceService = ServiceMocks.CreateVoiceOrchestrationService();
        var viewModel = new VoiceControlViewModel(mockVoiceService.Object);

        var initialRecognizedText = viewModel.RecognizedText;

        // Act - Dispose the ViewModel
        viewModel.Dispose();

        // Raise the event after disposal
        var recognitionResult = new VoiceRecognitionResult
        {
            Text = "This should not appear",
            IsFinal = true,
            Confidence = 0.95f
        };
        mockVoiceService.Raise(s => s.SpeechRecognized += null,
            new VoiceRecognitionEventArgs(recognitionResult));

        // Assert - RecognizedText should not have changed
        Assert.Equal(initialRecognizedText, viewModel.RecognizedText);
        Assert.NotEqual("This should not appear", viewModel.RecognizedText);
    }

    [Fact]
    public void Dispose_UnsubscribesFromSpeakingEvent()
    {
        // Arrange
        var mockVoiceService = ServiceMocks.CreateVoiceOrchestrationService();
        var viewModel = new VoiceControlViewModel(mockVoiceService.Object);

        // Act - Dispose the ViewModel
        viewModel.Dispose();

        // Raise the event after disposal
        mockVoiceService.Raise(s => s.Speaking += null,
            new SpeechSynthesisEventArgs("Test text"));

        // Assert - The Speaking event handler is empty (no state changes), so this test
        // primarily verifies that no exception is thrown when the event is raised after disposal
        Assert.NotNull(viewModel);
    }

    [Fact]
    public void Dispose_PreventsPropertyChangesFromAllEvents()
    {
        // Arrange
        var mockVoiceService = ServiceMocks.CreateVoiceOrchestrationService();
        var viewModel = new VoiceControlViewModel(mockVoiceService.Object);

        // Record initial state
        var initialIsListening = viewModel.IsListening;
        var initialIsSpeaking = viewModel.IsSpeaking;
        var initialIsInitialized = viewModel.IsInitialized;
        var initialVoiceActivity = viewModel.VoiceActivityDetected;
        var initialInputLevel = viewModel.InputLevel;
        var initialRecognizedText = viewModel.RecognizedText;

        // Act - Dispose the ViewModel
        viewModel.Dispose();

        // Raise StateChanged event
        var newState = new VoiceState
        {
            IsListening = !initialIsListening,
            IsSpeaking = !initialIsSpeaking,
            IsInitialized = !initialIsInitialized,
            VoiceActivityDetected = !initialVoiceActivity,
            InputLevel = 0.99f
        };
        mockVoiceService.Raise(s => s.StateChanged += null,
            new VoiceStateChangedEventArgs(newState));

        // Raise SpeechRecognized event
        var recognitionResult = new VoiceRecognitionResult
        {
            Text = "Changed text",
            IsFinal = true,
            Confidence = 0.95f
        };
        mockVoiceService.Raise(s => s.SpeechRecognized += null,
            new VoiceRecognitionEventArgs(recognitionResult));

        // Assert - All properties should remain unchanged
        Assert.Equal(initialIsListening, viewModel.IsListening);
        Assert.Equal(initialIsSpeaking, viewModel.IsSpeaking);
        Assert.Equal(initialIsInitialized, viewModel.IsInitialized);
        Assert.Equal(initialVoiceActivity, viewModel.VoiceActivityDetected);
        Assert.Equal(initialInputLevel, viewModel.InputLevel);
        Assert.Equal(initialRecognizedText, viewModel.RecognizedText);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes_WithoutException()
    {
        // Arrange
        var mockVoiceService = ServiceMocks.CreateVoiceOrchestrationService();
        var viewModel = new VoiceControlViewModel(mockVoiceService.Object);

        // Act & Assert - Multiple dispose calls should not throw
        viewModel.Dispose();
        viewModel.Dispose();
        viewModel.Dispose();

        // Verify - Should still be able to raise events without exceptions
        var newState = new VoiceState { IsListening = true };
        mockVoiceService.Raise(s => s.StateChanged += null,
            new VoiceStateChangedEventArgs(newState));
    }

    [Fact]
    public void Dispose_WithRaceCondition_HandlesEventGracefully()
    {
        // Arrange
        var mockVoiceService = ServiceMocks.CreateVoiceOrchestrationService();
        var viewModel = new VoiceControlViewModel(mockVoiceService.Object);

        // Simulate a scenario where an event is raised during disposal
        // by checking the _disposed flag in event handlers
        viewModel.Dispose();

        // Act - Raise event immediately after disposal
        var recognitionResult = new VoiceRecognitionResult
        {
            Text = "Race condition test",
            IsFinal = true,
            Confidence = 0.9f
        };

        // Assert - Should not throw and should handle gracefully
        mockVoiceService.Raise(s => s.SpeechRecognized += null,
            new VoiceRecognitionEventArgs(recognitionResult));

        Assert.NotEqual("Race condition test", viewModel.RecognizedText);
    }

    #endregion
}
