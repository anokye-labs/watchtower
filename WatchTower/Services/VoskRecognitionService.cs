using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using Vosk;
using WatchTower.Models;

namespace WatchTower.Services;

/// <summary>
/// Offline speech recognition service using Vosk.
/// Provides cross-platform speech-to-text without requiring internet connection.
/// </summary>
public class VoskRecognitionService : IVoiceRecognitionService
{
    private readonly ILogger<VoskRecognitionService> _logger;
    private readonly IConfiguration _configuration;
    private Model? _model;
    private VoskRecognizer? _recognizer;
    private WaveInEvent? _waveIn;
    private bool _isListening;
    private bool _isInitialized;
    private bool _disposed;
    private string _modelPath = string.Empty;

    public event EventHandler<VoiceRecognitionEventArgs>? SpeechRecognized;
    public event EventHandler<VoiceActivityEventArgs>? VoiceActivityDetected;

    public bool IsInitialized => _isInitialized;
    public bool IsListening => _isListening;
    public string ServiceName => "Vosk";

    public VoskRecognitionService(ILogger<VoskRecognitionService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<bool> InitializeAsync()
    {
        if (_isInitialized)
        {
            _logger.LogWarning("VoskRecognitionService already initialized");
            return true;
        }

        try
        {
            _logger.LogInformation("Initializing Vosk speech recognition service");

            // Get model path from configuration or use default
            _modelPath = _configuration.GetValue<string>("Voice:Vosk:ModelPath") 
                ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", "vosk-model-small-en-us-0.15");

            // Check if model exists
            if (!Directory.Exists(_modelPath))
            {
                _logger.LogError("Vosk model not found at: {ModelPath}", _modelPath);
                _logger.LogError("Please download a Vosk model from https://alphacephei.com/vosk/models");
                _logger.LogError("Extract it to: {ModelPath}", _modelPath);
                return false;
            }

            // Initialize Vosk model
            _logger.LogInformation("Loading Vosk model from: {ModelPath}", _modelPath);
            _model = new Model(_modelPath);

            // Create recognizer with 16kHz sample rate (standard for speech recognition)
            _recognizer = new VoskRecognizer(_model, 16000.0f);
            _recognizer.SetMaxAlternatives(0);
            _recognizer.SetWords(true);

            _logger.LogInformation("Vosk model loaded successfully");
            _isInitialized = true;
            
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Vosk recognition service");
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
            _logger.LogInformation("Starting speech recognition");

            // Initialize audio input with NAudio
            _waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(16000, 1) // 16kHz mono
            };

            _waveIn.DataAvailable += OnAudioDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStopped;

            _waveIn.StartRecording();
            _isListening = true;

            _logger.LogInformation("Speech recognition started");
            await Task.CompletedTask;
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

            if (_waveIn != null)
            {
                _waveIn.StopRecording();
                _waveIn.DataAvailable -= OnAudioDataAvailable;
                _waveIn.RecordingStopped -= OnRecordingStopped;
                _waveIn.Dispose();
                _waveIn = null;
            }

            _isListening = false;
            _logger.LogInformation("Speech recognition stopped");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping speech recognition");
        }
    }

    private void OnAudioDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_recognizer == null || !_isListening)
        {
            return;
        }

        try
        {
            // Feed audio data to Vosk
            if (_recognizer.AcceptWaveform(e.Buffer, e.BytesRecorded))
            {
                // Final result
                var result = _recognizer.Result();
                ProcessResult(result, true);
            }
            else
            {
                // Partial result
                var partialResult = _recognizer.PartialResult();
                ProcessResult(partialResult, false);
            }

            // Simple voice activity detection based on audio level
            float level = CalculateAudioLevel(e.Buffer, e.BytesRecorded);
            bool isActive = level > 0.01f; // Threshold for voice activity
            
            VoiceActivityDetected?.Invoke(this, new VoiceActivityEventArgs(isActive, level));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing audio data");
        }
    }

    private void ProcessResult(string jsonResult, bool isFinal)
    {
        try
        {
            // Parse Vosk JSON result
            // Simple parsing - in production, use JSON library
            var text = ExtractTextFromJson(jsonResult);
            
            if (!string.IsNullOrWhiteSpace(text))
            {
                var result = new VoiceRecognitionResult
                {
                    Text = text,
                    Confidence = 0.9f, // Vosk doesn't provide confidence in simple format
                    IsFinal = isFinal,
                    Source = ServiceName,
                    Timestamp = DateTime.UtcNow
                };

                SpeechRecognized?.Invoke(this, new VoiceRecognitionEventArgs(result));
                
                if (isFinal)
                {
                    _logger.LogInformation("Recognized: {Text}", text);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing recognition result");
        }
    }

    private string ExtractTextFromJson(string json)
    {
        // Simple JSON parsing for "text" field
        // Format: {"text": "recognized text"}
        var textStart = json.IndexOf("\"text\"", StringComparison.Ordinal);
        if (textStart < 0) return string.Empty;

        var colonIndex = json.IndexOf(':', textStart);
        if (colonIndex < 0) return string.Empty;

        var quoteStart = json.IndexOf('"', colonIndex);
        if (quoteStart < 0) return string.Empty;

        var quoteEnd = json.IndexOf('"', quoteStart + 1);
        if (quoteEnd < 0) return string.Empty;

        return json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1).Trim();
    }

    private float CalculateAudioLevel(byte[] buffer, int length)
    {
        // Calculate RMS (root mean square) audio level
        long sum = 0;
        for (int i = 0; i < length; i += 2)
        {
            if (i + 1 < length)
            {
                short sample = (short)(buffer[i] | (buffer[i + 1] << 8));
                sum += sample * sample;
            }
        }

        double rms = Math.Sqrt((double)sum / (length / 2));
        return (float)(rms / 32768.0); // Normalize to 0.0 - 1.0
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception != null)
        {
            _logger.LogError(e.Exception, "Recording stopped due to error");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        StopListeningAsync().Wait();

        _recognizer?.Dispose();
        _model?.Dispose();
        _waveIn?.Dispose();

        _disposed = true;
        _logger.LogInformation("VoskRecognitionService disposed");
    }
}
