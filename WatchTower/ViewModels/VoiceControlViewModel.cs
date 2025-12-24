using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WatchTower.Models;
using WatchTower.Services;

namespace WatchTower.ViewModels;

/// <summary>
/// ViewModel for voice control features.
/// Provides properties and commands for voice interaction.
/// </summary>
public class VoiceControlViewModel : INotifyPropertyChanged
{
    private readonly IVoiceOrchestrationService _voiceService;
    private string _recognizedText = string.Empty;
    private string _lastSpokenText = string.Empty;
    private bool _isListening;
    private bool _isSpeaking;
    private bool _isInitialized;
    private bool _voiceActivityDetected;
    private float _inputLevel;

    public event PropertyChangedEventHandler? PropertyChanged;

    public VoiceControlViewModel(IVoiceOrchestrationService voiceService)
    {
        _voiceService = voiceService;

        // Subscribe to voice service events
        _voiceService.StateChanged += OnVoiceStateChanged;
        _voiceService.SpeechRecognized += OnSpeechRecognized;
        _voiceService.Speaking += OnSpeaking;
    }

    /// <summary>
    /// Gets the recognized text from speech input.
    /// </summary>
    public string RecognizedText
    {
        get => _recognizedText;
        private set
        {
            if (_recognizedText != value)
            {
                _recognizedText = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets the last text that was spoken.
    /// </summary>
    public string LastSpokenText
    {
        get => _lastSpokenText;
        private set
        {
            if (_lastSpokenText != value)
            {
                _lastSpokenText = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets whether the voice service is currently listening.
    /// </summary>
    public bool IsListening
    {
        get => _isListening;
        private set
        {
            if (_isListening != value)
            {
                _isListening = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets whether the voice service is currently speaking.
    /// </summary>
    public bool IsSpeaking
    {
        get => _isSpeaking;
        private set
        {
            if (_isSpeaking != value)
            {
                _isSpeaking = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets whether the voice service is initialized.
    /// </summary>
    public bool IsInitialized
    {
        get => _isInitialized;
        private set
        {
            if (_isInitialized != value)
            {
                _isInitialized = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets whether voice activity is currently detected.
    /// </summary>
    public bool VoiceActivityDetected
    {
        get => _voiceActivityDetected;
        private set
        {
            if (_voiceActivityDetected != value)
            {
                _voiceActivityDetected = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets the current input audio level (0.0 to 1.0).
    /// </summary>
    public float InputLevel
    {
        get => _inputLevel;
        private set
        {
            if (Math.Abs(_inputLevel - value) > 0.01f)
            {
                _inputLevel = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Initializes the voice service if not already initialized.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_voiceService.IsInitialized)
        {
            IsInitialized = true;
            return;
        }

        IsInitialized = await _voiceService.InitializeAsync();
    }

    /// <summary>
    /// Starts listening for speech input.
    /// </summary>
    public async Task StartListeningAsync()
    {
        await _voiceService.StartListeningAsync();
    }

    /// <summary>
    /// Stops listening for speech input.
    /// </summary>
    public async Task StopListeningAsync()
    {
        await _voiceService.StopListeningAsync();
    }

    /// <summary>
    /// Speaks the given text.
    /// </summary>
    public async Task SpeakAsync(string text, bool interruptListening = false)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        LastSpokenText = text;
        await _voiceService.SpeakAsync(text, interruptListening);
    }

    /// <summary>
    /// Starts full-duplex voice mode (listening and ready to speak).
    /// </summary>
    public async Task StartFullDuplexAsync()
    {
        await _voiceService.StartFullDuplexAsync();
    }

    /// <summary>
    /// Stops full-duplex voice mode.
    /// </summary>
    public async Task StopFullDuplexAsync()
    {
        await _voiceService.StopFullDuplexAsync();
    }

    private void OnVoiceStateChanged(object? sender, VoiceStateChangedEventArgs e)
    {
        IsListening = e.State.IsListening;
        IsSpeaking = e.State.IsSpeaking;
        IsInitialized = e.State.IsInitialized;
        VoiceActivityDetected = e.State.VoiceActivityDetected;
        InputLevel = e.State.InputLevel;
    }

    private void OnSpeechRecognized(object? sender, VoiceRecognitionEventArgs e)
    {
        if (e.Result.IsFinal)
        {
            // Final result - update the recognized text
            RecognizedText = e.Result.Text;
        }
        else
        {
            // Partial result - could show in a separate property if needed
            // For now, we'll update the same property
            RecognizedText = $"{e.Result.Text} ...";
        }
    }

    private void OnSpeaking(object? sender, SpeechSynthesisEventArgs e)
    {
        // Event fired when speaking starts
        // LastSpokenText is already set before calling SpeakAsync
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
