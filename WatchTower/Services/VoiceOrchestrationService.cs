using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using WatchTower.Models;

namespace WatchTower.Services;

/// <summary>
/// Orchestrates full-duplex voice operations.
/// Coordinates speech recognition and synthesis to enable simultaneous listening and speaking.
/// </summary>
public class VoiceOrchestrationService : IVoiceOrchestrationService
{
    private readonly ILogger<VoiceOrchestrationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IVoiceRecognitionService _recognitionService;
    private readonly ITextToSpeechService _ttsService;
    private readonly VoiceState _state;
    private bool _isInitialized;
    private bool _disposed;

    public event EventHandler<VoiceStateChangedEventArgs>? StateChanged;
    public event EventHandler<VoiceRecognitionEventArgs>? SpeechRecognized;
    public event EventHandler<SpeechSynthesisEventArgs>? Speaking;

    public VoiceState State => _state;
    public bool IsInitialized => _isInitialized;

    public VoiceOrchestrationService(
        ILogger<VoiceOrchestrationService> logger,
        IConfiguration configuration,
        IVoiceRecognitionService recognitionService,
        ITextToSpeechService ttsService)
    {
        _logger = logger;
        _configuration = configuration;
        _recognitionService = recognitionService;
        _ttsService = ttsService;
        _state = new VoiceState();

        // Subscribe to service events
        _recognitionService.SpeechRecognized += OnSpeechRecognized;
        _recognitionService.VoiceActivityDetected += OnVoiceActivityDetected;
        _ttsService.SynthesisStarted += OnSynthesisStarted;
        _ttsService.SynthesisCompleted += OnSynthesisCompleted;
        _ttsService.SynthesisError += OnSynthesisError;
    }

    public async Task<bool> InitializeAsync()
    {
        if (_isInitialized)
        {
            _logger.LogWarning("VoiceOrchestrationService already initialized");
            return true;
        }

        try
        {
            _logger.LogInformation("Initializing voice orchestration service");

            // Determine voice mode from configuration
            var modeString = _configuration.GetValue<string>("Voice:Mode") ?? "offline";
            if (!Enum.TryParse<VoiceMode>(modeString, ignoreCase: true, out var mode))
            {
                _logger.LogWarning("Unknown voice mode '{Mode}', defaulting to Offline", modeString);
                mode = VoiceMode.Offline;
            }
            _state.Mode = mode;
            _logger.LogInformation("Voice mode: {Mode}", _state.Mode);

            // Initialize recognition service
            _logger.LogInformation("Initializing recognition service: {ServiceName}",
                _recognitionService.ServiceName);

            if (!await _recognitionService.InitializeAsync())
            {
                _logger.LogError("Failed to initialize recognition service");
                return false;
            }

            // Initialize TTS service
            _logger.LogInformation("Initializing TTS service: {ServiceName}",
                _ttsService.ServiceName);

            if (!await _ttsService.InitializeAsync())
            {
                _logger.LogError("Failed to initialize TTS service");
                return false;
            }

            _state.IsInitialized = true;
            _isInitialized = true;

            RaiseStateChanged();
            _logger.LogInformation("Voice orchestration service initialized successfully");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize voice orchestration service");
            return false;
        }
    }

    public async Task StartFullDuplexAsync()
    {
        if (!_isInitialized)
        {
            _logger.LogError("Cannot start full-duplex: service not initialized");
            return;
        }

        _logger.LogInformation("Starting full-duplex voice mode");

        // Start listening
        await StartListeningAsync();

        _state.IsFullDuplex = true;
        RaiseStateChanged();

        _logger.LogInformation("Full-duplex voice mode started");
    }

    public async Task StopFullDuplexAsync()
    {
        if (!_state.IsFullDuplex)
        {
            return;
        }

        _logger.LogInformation("Stopping full-duplex voice mode");

        // Stop all operations
        await StopListeningAsync();
        await StopSpeakingAsync();

        _state.IsFullDuplex = false;
        RaiseStateChanged();

        _logger.LogInformation("Full-duplex voice mode stopped");
    }

    public async Task StartListeningAsync()
    {
        if (!_isInitialized)
        {
            _logger.LogError("Cannot start listening: service not initialized");
            return;
        }

        if (_state.IsListening)
        {
            _logger.LogWarning("Already listening");
            return;
        }

        _logger.LogInformation("Starting voice recognition");

        await _recognitionService.StartListeningAsync();

        _state.IsListening = true;
        RaiseStateChanged();

        _logger.LogInformation("Voice recognition started");
    }

    public async Task StopListeningAsync()
    {
        if (!_state.IsListening)
        {
            return;
        }

        _logger.LogInformation("Stopping voice recognition");

        await _recognitionService.StopListeningAsync();

        _state.IsListening = false;
        _state.VoiceActivityDetected = false;
        _state.InputLevel = 0;
        RaiseStateChanged();

        _logger.LogInformation("Voice recognition stopped");
    }

    public async Task SpeakAsync(string text, bool interruptListening = false)
    {
        if (!_isInitialized)
        {
            _logger.LogError("Cannot speak: service not initialized");
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Cannot speak: text is empty");
            return;
        }

        try
        {
            // Optionally pause listening during speech to prevent echo/feedback
            bool wasListening = _state.IsListening;
            if (interruptListening && wasListening)
            {
                _logger.LogInformation("Pausing listening during speech (interrupt mode)");
                await StopListeningAsync();
            }

            _logger.LogInformation("Speaking: {Text}", text);
            await _ttsService.SpeakAsync(text);

            // Resume listening if it was active and we paused it
            if (interruptListening && wasListening)
            {
                _logger.LogInformation("Resuming listening after speech");
                await StartListeningAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during speech synthesis");
        }
    }

    public async Task StopSpeakingAsync()
    {
        if (!_state.IsSpeaking)
        {
            return;
        }

        _logger.LogInformation("Stopping speech synthesis");
        await _ttsService.StopAsync();
        _logger.LogInformation("Speech synthesis stopped");
    }

    private void OnSpeechRecognized(object? sender, VoiceRecognitionEventArgs e)
    {
        // Forward event to subscribers
        SpeechRecognized?.Invoke(this, e);
    }

    private void OnVoiceActivityDetected(object? sender, VoiceActivityEventArgs e)
    {
        _state.VoiceActivityDetected = e.IsActive;
        _state.InputLevel = e.Level;
        RaiseStateChanged();
    }

    private void OnSynthesisStarted(object? sender, SpeechSynthesisEventArgs e)
    {
        _state.IsSpeaking = true;
        RaiseStateChanged();

        // Forward event to subscribers
        Speaking?.Invoke(this, e);
    }

    private void OnSynthesisCompleted(object? sender, SpeechSynthesisEventArgs e)
    {
        _state.IsSpeaking = false;
        _state.OutputLevel = 0;
        RaiseStateChanged();
    }

    private void OnSynthesisError(object? sender, SpeechSynthesisErrorEventArgs e)
    {
        _state.IsSpeaking = false;
        _state.OutputLevel = 0;
        RaiseStateChanged();

        _logger.LogError("Speech synthesis error: {Error}", e.ErrorMessage);
    }

    private void RaiseStateChanged()
    {
        StateChanged?.Invoke(this, new VoiceStateChangedEventArgs(_state));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Unsubscribe from events first to prevent callbacks during disposal
        _recognitionService.SpeechRecognized -= OnSpeechRecognized;
        _recognitionService.VoiceActivityDetected -= OnVoiceActivityDetected;
        _ttsService.SynthesisStarted -= OnSynthesisStarted;
        _ttsService.SynthesisCompleted -= OnSynthesisCompleted;
        _ttsService.SynthesisError -= OnSynthesisError;

        // Stop all operations using Task.Run to avoid deadlock from synchronization context
        try
        {
            Task.Run(() => StopFullDuplexAsync()).Wait(TimeSpan.FromSeconds(2));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Timeout or error during voice orchestration shutdown");
        }

        _logger.LogInformation("VoiceOrchestrationService disposed");
    }
}
