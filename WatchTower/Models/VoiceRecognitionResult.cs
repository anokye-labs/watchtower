using System;

namespace WatchTower.Models;

/// <summary>
/// Represents the result of speech recognition.
/// </summary>
public class VoiceRecognitionResult
{
    /// <summary>
    /// The recognized text.
    /// </summary>
    public string Text { get; set; } = string.Empty;
    
    /// <summary>
    /// Confidence score (0.0 to 1.0), where 1.0 is highest confidence.
    /// </summary>
    public float Confidence { get; set; }
    
    /// <summary>
    /// Whether this is a final result or an intermediate (partial) result.
    /// </summary>
    public bool IsFinal { get; set; }
    
    /// <summary>
    /// Timestamp when the recognition occurred.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// The source of the recognition (e.g., "Vosk", "Azure").
    /// </summary>
    public string Source { get; set; } = string.Empty;
}
