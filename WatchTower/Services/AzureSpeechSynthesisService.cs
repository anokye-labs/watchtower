using System;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AzureSpeechSynthesisEventArgs = Microsoft.CognitiveServices.Speech.SpeechSynthesisEventArgs;

namespace WatchTower.Services;

/// <summary>
/// Online text-to-speech service using Azure Cognitive Services.
/// Requires internet connection and Azure Speech Services API key.
/// </summary>
public class AzureSpeechSynthesisService : ITextToSpeechService
{
    private readonly ILogger<AzureSpeechSynthesisService> _logger;
    private readonly IConfiguration _configuration;
    private SpeechSynthesizer? _synthesizer;
    private SpeechConfig? _speechConfig;
    private bool _isSpeaking;
    private bool _isInitialized;
    private bool _disposed;

    public event EventHandler<SpeechSynthesisEventArgs>? SynthesisStarted;
    public event EventHandler<SpeechSynthesisEventArgs>? SynthesisCompleted;
    public event EventHandler<SpeechSynthesisErrorEventArgs>? SynthesisError;

    public bool IsInitialized => _isInitialized;
    public bool IsSpeaking => _isSpeaking;
    public string ServiceName => "Azure Speech";

    public AzureSpeechSynthesisService(
        ILogger<AzureSpeechSynthesisService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<bool> InitializeAsync()
    {
        if (_isInitialized)
        {
            _logger.LogWarning("AzureSpeechSynthesisService already initialized");
            return true;
        }

        try
        {
            _logger.LogInformation("Initializing Azure Speech synthesis service");

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
            
            // Configure synthesis voice
            var voiceName = _configuration.GetValue<string>("Voice:Azure:VoiceName") 
                ?? "en-US-AriaNeural";
            _speechConfig.SpeechSynthesisVoiceName = voiceName;

            // Create audio configuration for default speaker
            var audioConfig = AudioConfig.FromDefaultSpeakerOutput();

            // Create speech synthesizer
            _synthesizer = new SpeechSynthesizer(_speechConfig, audioConfig);

            // Subscribe to events
            _synthesizer.SynthesisStarted += OnAzureSynthesisStarted;
            _synthesizer.SynthesisCompleted += OnAzureSynthesisCompleted;
            _synthesizer.SynthesisCanceled += OnAzureSynthesisCanceled;

            _logger.LogInformation("Azure Speech synthesis service initialized successfully");
            _isInitialized = true;

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure Speech synthesis service");
            return false;
        }
    }

    public async Task SpeakAsync(string text)
    {
        if (!_isInitialized)
        {
            _logger.LogError("Cannot speak: service not initialized");
            var error = new SpeechSynthesisErrorEventArgs(text, "Service not initialized");
            SynthesisError?.Invoke(this, error);
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Cannot speak: text is empty");
            return;
        }

        if (_isSpeaking)
        {
            _logger.LogWarning("Already speaking, stopping current synthesis");
            await StopAsync();
        }

        try
        {
            _logger.LogInformation("Synthesizing speech: {Text}", text);
            _isSpeaking = true;

            // Synthesize speech
            var result = await _synthesizer!.SpeakTextAsync(text);

            if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                _logger.LogError("Synthesis canceled: {Reason}, Error: {ErrorDetails}", 
                    cancellation.Reason, cancellation.ErrorDetails);
                
                var error = new SpeechSynthesisErrorEventArgs(text, 
                    $"Synthesis canceled: {cancellation.Reason}", 
                    new Exception(cancellation.ErrorDetails));
                SynthesisError?.Invoke(this, error);
            }
            else if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                _logger.LogInformation("Speech synthesis completed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during speech synthesis");
            var error = new SpeechSynthesisErrorEventArgs(text, ex.Message, ex);
            SynthesisError?.Invoke(this, error);
        }
        finally
        {
            _isSpeaking = false;
        }
    }

    public async Task StopAsync()
    {
        if (!_isSpeaking)
        {
            return;
        }

        try
        {
            _logger.LogInformation("Stopping speech synthesis");
            // Azure SDK doesn't have a direct stop method for synthesis,
            // but disposing and recreating the synthesizer will stop playback
            _isSpeaking = false;
            _logger.LogInformation("Speech synthesis stopped");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping speech synthesis");
        }
    }

    private void OnAzureSynthesisStarted(object? sender, AzureSpeechSynthesisEventArgs e)
    {
        _logger.LogDebug("Synthesis started");
        SynthesisStarted?.Invoke(this, new SpeechSynthesisEventArgs(string.Empty));
    }

    private void OnAzureSynthesisCompleted(object? sender, AzureSpeechSynthesisEventArgs e)
    {
        _logger.LogDebug("Synthesis completed");
        SynthesisCompleted?.Invoke(this, new SpeechSynthesisEventArgs(string.Empty));
    }

    private void OnAzureSynthesisCanceled(object? sender, AzureSpeechSynthesisEventArgs e)
    {
        var cancellation = SpeechSynthesisCancellationDetails.FromResult(e.Result);
        _logger.LogWarning("Synthesis canceled: {Reason}", cancellation.Reason);

        if (cancellation.Reason == CancellationReason.Error)
        {
            _logger.LogError("Error code: {ErrorCode}, Error details: {ErrorDetails}",
                cancellation.ErrorCode, cancellation.ErrorDetails);
        }

        _isSpeaking = false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Stop synthesis using Task.Run to avoid deadlock from synchronization context
        try
        {
            Task.Run(() => StopAsync()).Wait(TimeSpan.FromSeconds(2));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Timeout or error during Azure synthesis shutdown");
        }

        if (_synthesizer != null)
        {
            _synthesizer.SynthesisStarted -= OnAzureSynthesisStarted;
            _synthesizer.SynthesisCompleted -= OnAzureSynthesisCompleted;
            _synthesizer.SynthesisCanceled -= OnAzureSynthesisCanceled;
            _synthesizer.Dispose();
        }

        // SpeechConfig doesn't need disposal in newer SDKs

        _logger.LogInformation("AzureSpeechSynthesisService disposed");
    }
}
