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
    /// Recognition confidence score (0.0 to 1.0).
    /// Higher values indicate greater confidence in the recognition result.
    /// A value of 0.0f indicates confidence data is unavailable.
    /// </summary>
    /// <remarks>
    /// Confidence scores vary by recognition service:
    /// <list type="bullet">
    /// <item><description>Azure Speech: Extracted from NBest results when available</description></item>
    /// <item><description>Vosk: Aggregated from word-level confidences</description></item>
    /// <item><description>0.0f: Indicates the service could not provide confidence data</description></item>
    /// </list>
    /// </remarks>
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
