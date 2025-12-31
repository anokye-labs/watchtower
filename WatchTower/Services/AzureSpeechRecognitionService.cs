using System;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WatchTower.Models;

namespace WatchTower.Services;

/// <summary>
/// Online speech recognition service using Azure Cognitive Services.
/// Requires internet connection and Azure Speech Services API key.
/// </summary>
/// <remarks>
/// <para><strong>Confidence Score Extraction:</strong></para>
/// <para>
/// Azure Speech provides confidence scores through the NBest (N-best list) results feature.
/// The confidence value represents the service's certainty in the recognition accuracy,
/// ranging from 0.0 (no confidence) to 1.0 (highest confidence).
/// </para>
/// <para><strong>Extraction Strategy:</strong></para>
/// <list type="bullet">
/// <item><description>Uses the <c>Best()</c> method to retrieve the highest-confidence alternative from NBest results</description></item>
/// <item><description>The first item in the NBest list represents the most likely recognition</description></item>
/// <item><description>Falls back to 0.0f when confidence data is unavailable</description></item>
/// <item><description>Semantic segmentation results may not provide confidence scores</description></item>
/// </list>
/// <para><strong>Current Implementation Note:</strong></para>
/// <para>
/// The current implementation uses placeholder confidence values (0.5f for partial, 0.9f for final results)
/// as a temporary measure. Full NBest integration requires additional result format configuration.
/// </para>
/// </remarks>
public class AzureSpeechRecognitionService : IVoiceRecognitionService
{
    private readonly ILogger<AzureSpeechRecognitionService> _logger;
    private readonly IConfiguration _configuration;
    private SpeechRecognizer? _recognizer;
    private SpeechConfig? _speechConfig;
    private bool _isListening;
    private bool _isInitialized;
    private bool _disposed;

    public event EventHandler<VoiceRecognitionEventArgs>? SpeechRecognized;
    public event EventHandler<VoiceActivityEventArgs>? VoiceActivityDetected;

    public bool IsInitialized => _isInitialized;
    public bool IsListening => _isListening;
    public string ServiceName => "Azure Speech";

    public AzureSpeechRecognitionService(
        ILogger<AzureSpeechRecognitionService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<bool> InitializeAsync()
    {
        if (_isInitialized)
        {
            _logger.LogWarning("AzureSpeechRecognitionService already initialized");
            return true;
        }

        try
        {
            _logger.LogInformation("Initializing Azure Speech recognition service");

            // Get Azure credentials from configuration or environment
            var speechKey = _configuration.GetValue<string>("Voice:Azure:SpeechKey")
                ?? Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY");
            
            var speechRegion = _configuration.GetValue<string>("Voice:Azure:SpeechRegion")
                ?? Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION");

            if (string.IsNullOrWhiteSpace(speechKey) || string.IsNullOrWhiteSpace(speechRegion))
            {
                _logger.LogError("Azure Speech credentials not configured");
                _logger.LogError("Set AZURE_SPEECH_KEY and AZURE_SPEECH_REGION in .env or appsettings.json");
                return false;
            }

            // Create speech configuration
            _speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
            
            // Configure recognition language
            var language = _configuration.GetValue<string>("Voice:Azure:Language") ?? "en-US";
            _speechConfig.SpeechRecognitionLanguage = language;

            // Create audio configuration for microphone input
            var audioConfig = AudioConfig.FromDefaultMicrophoneInput();

            // Create speech recognizer
            _recognizer = new SpeechRecognizer(_speechConfig, audioConfig);

            // Subscribe to events
            _recognizer.Recognizing += OnRecognizing;
            _recognizer.Recognized += OnRecognized;
            _recognizer.Canceled += OnCanceled;
            _recognizer.SessionStarted += OnSessionStarted;
            _recognizer.SessionStopped += OnSessionStopped;

            _logger.LogInformation("Azure Speech recognition service initialized successfully");
            _isInitialized = true;

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure Speech recognition service");
            return false;
        }
    }

    public async Task StartListeningAsync()
    {
        if (!_isInitialized)
        {
            _logger.LogError("Cannot start listening: service not initialized");
            return;
        }

        if (_isListening)
        {
            _logger.LogWarning("Already listening");
            return;
        }

        try
        {
            _logger.LogInformation("Starting continuous speech recognition");

            // Start continuous recognition
            await _recognizer!.StartContinuousRecognitionAsync();
            
            _isListening = true;
            _logger.LogInformation("Speech recognition started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start speech recognition");
            _isListening = false;
        }
    }

    public async Task StopListeningAsync()
    {
        if (!_isListening)
        {
            return;
        }

        try
        {
            _logger.LogInformation("Stopping speech recognition");

            await _recognizer!.StopContinuousRecognitionAsync();

            _isListening = false;
            _logger.LogInformation("Speech recognition stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping speech recognition");
        }
    }

    private void OnRecognizing(object? sender, SpeechRecognitionEventArgs e)
    {
        // Partial result (interim recognition)
        if (!string.IsNullOrWhiteSpace(e.Result.Text))
        {
            var result = new VoiceRecognitionResult
            {
                Text = e.Result.Text,
                // Placeholder confidence for interim results
                // Azure SDK provides confidence, but not directly exposed in this event
                Confidence = 0.5f,
                IsFinal = false,
                Source = ServiceName,
                Timestamp = DateTime.UtcNow
            };

            SpeechRecognized?.Invoke(this, new VoiceRecognitionEventArgs(result));
        }
    }

    private void OnRecognized(object? sender, SpeechRecognitionEventArgs e)
    {
        if (e.Result.Reason == ResultReason.RecognizedSpeech)
        {
            // Final result
            var result = new VoiceRecognitionResult
            {
                Text = e.Result.Text,
                // Placeholder confidence for final results
                // Azure SDK provides detailed confidence via e.Result.Best() but requires different result format
                Confidence = 0.9f,
                IsFinal = true,
                Source = ServiceName,
                Timestamp = DateTime.UtcNow
            };

            SpeechRecognized?.Invoke(this, new VoiceRecognitionEventArgs(result));
            _logger.LogInformation("Recognized: {Text}", e.Result.Text);
        }
        else if (e.Result.Reason == ResultReason.NoMatch)
        {
            _logger.LogDebug("Speech could not be recognized");
        }
    }

    private void OnCanceled(object? sender, SpeechRecognitionCanceledEventArgs e)
    {
        _logger.LogWarning("Recognition canceled: {Reason}", e.Reason);

        if (e.Reason == CancellationReason.Error)
        {
            _logger.LogError("Error code: {ErrorCode}, Error details: {ErrorDetails}", 
                e.ErrorCode, e.ErrorDetails);
        }

        _isListening = false;
    }

    private void OnSessionStarted(object? sender, SessionEventArgs e)
    {
        _logger.LogInformation("Recognition session started: {SessionId}", e.SessionId);
    }

    private void OnSessionStopped(object? sender, SessionEventArgs e)
    {
        _logger.LogInformation("Recognition session stopped: {SessionId}", e.SessionId);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Stop listening using Task.Run to avoid deadlock from synchronization context
        try
        {
            Task.Run(() => StopListeningAsync()).Wait(TimeSpan.FromSeconds(2));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Timeout or error during Azure recognition shutdown");
        }

        if (_recognizer != null)
        {
            _recognizer.Recognizing -= OnRecognizing;
            _recognizer.Recognized -= OnRecognized;
            _recognizer.Canceled -= OnCanceled;
            _recognizer.SessionStarted -= OnSessionStarted;
            _recognizer.SessionStopped -= OnSessionStopped;
            _recognizer.Dispose();
        }

        // SpeechConfig doesn't need disposal in newer SDKs

        _logger.LogInformation("AzureSpeechRecognitionService disposed");
    }
}
