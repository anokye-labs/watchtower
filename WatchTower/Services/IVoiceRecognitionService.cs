using System;
using System.Threading.Tasks;
using WatchTower.Models;

namespace WatchTower.Services;

/// <summary>
/// Interface for speech recognition services.
/// Provides speech-to-text (ASR) functionality from audio input.
/// </summary>
public interface IVoiceRecognitionService : IDisposable
{
    /// <summary>
    /// Event raised when speech is recognized (partial or final result).
    /// </summary>
    event EventHandler<VoiceRecognitionEventArgs>? SpeechRecognized;
    
    /// <summary>
    /// Event raised when voice activity is detected.
    /// </summary>
    event EventHandler<VoiceActivityEventArgs>? VoiceActivityDetected;
    
    /// <summary>
    /// Gets whether the service is initialized and ready.
    /// </summary>
    bool IsInitialized { get; }
    
    /// <summary>
    /// Gets whether the service is currently listening.
    /// </summary>
    bool IsListening { get; }
    
    /// <summary>
    /// Gets the name of the recognition service (e.g., "Vosk", "Azure").
    /// </summary>
    string ServiceName { get; }
    
    /// <summary>
    /// Initializes the voice recognition service.
    /// </summary>
    /// <returns>True if initialization succeeded, false otherwise.</returns>
    Task<bool> InitializeAsync();
    
    /// <summary>
    /// Starts listening for speech input.
    /// </summary>
    Task StartListeningAsync();
    
    /// <summary>
    /// Stops listening for speech input.
    /// </summary>
    Task StopListeningAsync();
}

/// <summary>
/// Event arguments for speech recognition events.
/// </summary>
public class VoiceRecognitionEventArgs : EventArgs
{
    public VoiceRecognitionResult Result { get; }
    
    public VoiceRecognitionEventArgs(VoiceRecognitionResult result)
    {
        Result = result;
    }
}

/// <summary>
/// Event arguments for voice activity detection events.
/// </summary>
public class VoiceActivityEventArgs : EventArgs
{
    /// <summary>
    /// Whether voice activity is detected.
    /// </summary>
    public bool IsActive { get; }
    
    /// <summary>
    /// Audio level (0.0 to 1.0).
    /// </summary>
    public float Level { get; }
    
    public VoiceActivityEventArgs(bool isActive, float level)
    {
        IsActive = isActive;
        Level = level;
    }
}
