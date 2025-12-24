using System;
using System.Threading.Tasks;

namespace WatchTower.Services;

/// <summary>
/// Interface for text-to-speech (TTS) services.
/// Converts text into spoken audio output.
/// </summary>
public interface ITextToSpeechService : IDisposable
{
    /// <summary>
    /// Event raised when speech synthesis starts.
    /// </summary>
    event EventHandler<SpeechSynthesisEventArgs>? SynthesisStarted;
    
    /// <summary>
    /// Event raised when speech synthesis completes.
    /// </summary>
    event EventHandler<SpeechSynthesisEventArgs>? SynthesisCompleted;
    
    /// <summary>
    /// Event raised when speech synthesis fails.
    /// </summary>
    event EventHandler<SpeechSynthesisErrorEventArgs>? SynthesisError;
    
    /// <summary>
    /// Gets whether the service is initialized and ready.
    /// </summary>
    bool IsInitialized { get; }
    
    /// <summary>
    /// Gets whether the service is currently speaking.
    /// </summary>
    bool IsSpeaking { get; }
    
    /// <summary>
    /// Gets the name of the TTS service (e.g., "Piper", "Azure").
    /// </summary>
    string ServiceName { get; }
    
    /// <summary>
    /// Initializes the text-to-speech service.
    /// </summary>
    /// <returns>True if initialization succeeded, false otherwise.</returns>
    Task<bool> InitializeAsync();
    
    /// <summary>
    /// Synthesizes speech from text and plays it.
    /// </summary>
    /// <param name="text">The text to speak.</param>
    Task SpeakAsync(string text);
    
    /// <summary>
    /// Stops the current speech synthesis.
    /// </summary>
    Task StopAsync();
}

/// <summary>
/// Event arguments for speech synthesis events.
/// </summary>
public class SpeechSynthesisEventArgs : EventArgs
{
    public string Text { get; }
    
    public SpeechSynthesisEventArgs(string text)
    {
        Text = text;
    }
}

/// <summary>
/// Event arguments for speech synthesis error events.
/// </summary>
public class SpeechSynthesisErrorEventArgs : EventArgs
{
    public string Text { get; }
    public string ErrorMessage { get; }
    public Exception? Exception { get; }
    
    public SpeechSynthesisErrorEventArgs(string text, string errorMessage, Exception? exception = null)
    {
        Text = text;
        ErrorMessage = errorMessage;
        Exception = exception;
    }
}
