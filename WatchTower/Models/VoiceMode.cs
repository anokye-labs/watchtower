namespace WatchTower.Models;

/// <summary>
/// Defines the voice operation mode for speech recognition and synthesis.
/// </summary>
public enum VoiceMode
{
    /// <summary>
    /// Use only offline models (Vosk for ASR, Piper for TTS).
    /// No internet connection required.
    /// </summary>
    Offline,

    /// <summary>
    /// Use only online models (Azure Speech Services).
    /// Requires internet connection and API keys.
    /// </summary>
    Online,

    /// <summary>
    /// Prefer offline models, but fallback to online if offline fails or is unavailable.
    /// </summary>
    Hybrid
}
