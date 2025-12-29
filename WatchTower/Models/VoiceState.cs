namespace WatchTower.Models;

/// <summary>
/// Represents the current state of the voice system.
/// </summary>
public class VoiceState
{
    /// <summary>
    /// Whether the voice system is currently listening for speech input.
    /// </summary>
    public bool IsListening { get; set; }
    
    /// <summary>
    /// Whether the voice system is currently speaking (outputting audio).
    /// </summary>
    public bool IsSpeaking { get; set; }
    
    /// <summary>
    /// Whether the voice system is initialized and ready.
    /// </summary>
    public bool IsInitialized { get; set; }
    
    /// <summary>
    /// The current voice mode being used.
    /// </summary>
    public VoiceMode Mode { get; set; }
    
    /// <summary>
    /// Whether the system is in full-duplex mode (listening and speaking simultaneously).
    /// </summary>
    public bool IsFullDuplex { get; set; }
    
    /// <summary>
    /// Whether voice activity is currently detected.
    /// </summary>
    public bool VoiceActivityDetected { get; set; }
    
    /// <summary>
    /// Current audio input level (0.0 to 1.0).
    /// </summary>
    public float InputLevel { get; set; }
    
    /// <summary>
    /// Current audio output level (0.0 to 1.0).
    /// </summary>
    public float OutputLevel { get; set; }
}
