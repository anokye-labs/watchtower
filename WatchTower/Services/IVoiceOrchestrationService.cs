using System;
using System.Threading.Tasks;
using WatchTower.Models;

namespace WatchTower.Services;

/// <summary>
/// Interface for voice orchestration service.
/// Coordinates full-duplex voice operations (simultaneous listening and speaking).
/// </summary>
public interface IVoiceOrchestrationService : IDisposable
{
    /// <summary>
    /// Event raised when the voice state changes.
    /// </summary>
    event EventHandler<VoiceStateChangedEventArgs>? StateChanged;
    
    /// <summary>
    /// Event raised when speech is recognized.
    /// </summary>
    event EventHandler<VoiceRecognitionEventArgs>? SpeechRecognized;
    
    /// <summary>
    /// Event raised when synthesis is speaking.
    /// </summary>
    event EventHandler<SpeechSynthesisEventArgs>? Speaking;
    
    /// <summary>
    /// Gets the current voice state.
    /// </summary>
    VoiceState State { get; }
    
    /// <summary>
    /// Gets whether the service is initialized and ready.
    /// </summary>
    bool IsInitialized { get; }
    
    /// <summary>
    /// Initializes the voice orchestration service.
    /// </summary>
    /// <returns>True if initialization succeeded, false otherwise.</returns>
    Task<bool> InitializeAsync();
    
    /// <summary>
    /// Starts full-duplex voice mode (listening and ready to speak).
    /// </summary>
    Task StartFullDuplexAsync();
    
    /// <summary>
    /// Stops full-duplex voice mode.
    /// </summary>
    Task StopFullDuplexAsync();
    
    /// <summary>
    /// Starts listening for speech input.
    /// </summary>
    Task StartListeningAsync();
    
    /// <summary>
    /// Stops listening for speech input.
    /// </summary>
    Task StopListeningAsync();
    
    /// <summary>
    /// Synthesizes and speaks the given text.
    /// In full-duplex mode, this can occur while listening.
    /// </summary>
    /// <param name="text">The text to speak.</param>
    /// <param name="interruptListening">Whether to temporarily pause listening while speaking (barge-in prevention).</param>
    Task SpeakAsync(string text, bool interruptListening = false);
    
    /// <summary>
    /// Stops the current speech synthesis.
    /// </summary>
    Task StopSpeakingAsync();
}

/// <summary>
/// Event arguments for voice state change events.
/// </summary>
public class VoiceStateChangedEventArgs : EventArgs
{
    public VoiceState State { get; }
    
    public VoiceStateChangedEventArgs(VoiceState state)
    {
        State = state;
    }
}
