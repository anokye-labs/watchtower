using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using PiperSharp;
using PiperSharp.Models;

namespace WatchTower.Services;

/// <summary>
/// Offline text-to-speech service using Piper.
/// Provides high-quality speech synthesis without requiring internet connection.
/// </summary>
public class PiperTextToSpeechService : ITextToSpeechService
{
    private readonly ILogger<PiperTextToSpeechService> _logger;
    private readonly IConfiguration _configuration;
    private PiperProvider? _piperProvider;
    private WaveOutEvent? _waveOut;
    private bool _isSpeaking;
    private bool _isInitialized;
    private bool _disposed;
    private string _modelPath = string.Empty;
    private VoiceModel? _model;

    public event EventHandler<SpeechSynthesisEventArgs>? SynthesisStarted;
    public event EventHandler<SpeechSynthesisEventArgs>? SynthesisCompleted;
    public event EventHandler<SpeechSynthesisErrorEventArgs>? SynthesisError;

    public bool IsInitialized => _isInitialized;
    public bool IsSpeaking => _isSpeaking;
    public string ServiceName => "Piper";

    public PiperTextToSpeechService(ILogger<PiperTextToSpeechService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<bool> InitializeAsync()
    {
        if (_isInitialized)
        {
            _logger.LogWarning("PiperTextToSpeechService already initialized");
            return true;
        }

        try
        {
            _logger.LogInformation("Initializing Piper text-to-speech service");

            // Get model path from configuration or use default
            _modelPath = _configuration.GetValue<string>("Voice:Piper:ModelPath")
                ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", "piper");

            // Check if model directory exists
            if (!Directory.Exists(_modelPath))
            {
                _logger.LogError("Piper model directory not found at: {ModelPath}", _modelPath);
                _logger.LogError("Please download a Piper voice model from https://huggingface.co/rhasspy/piper-voices");
                _logger.LogError("Extract it to: {ModelPath}", _modelPath);
                return false;
            }

            // Get voice name from configuration or use default
            var voiceName = _configuration.GetValue<string>("Voice:Piper:Voice") ?? "en_US-lessac-medium";
            var onnxPath = Path.Combine(_modelPath, $"{voiceName}.onnx");
            var configPath = Path.Combine(_modelPath, $"{voiceName}.onnx.json");

            if (!File.Exists(onnxPath))
            {
                _logger.LogError("Piper model file not found: {OnnxPath}", onnxPath);
                return false;
            }

            if (!File.Exists(configPath))
            {
                _logger.LogError("Piper config file not found: {ConfigPath}", configPath);
                return false;
            }

            // Initialize Piper model and provider
            _logger.LogInformation("Loading Piper model: {Voice}", voiceName);
            
            // Try to load the model from the model path
            _model = await VoiceModel.LoadModel(onnxPath);
            
            _piperProvider = new PiperProvider(new PiperConfiguration
            {
                Model = _model,
                UseCuda = false
            });

            _logger.LogInformation("Piper model loaded successfully");
            _isInitialized = true;

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Piper text-to-speech service");
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

            SynthesisStarted?.Invoke(this, new SpeechSynthesisEventArgs(text));

            // Synthesize audio with Piper
            var audioData = await _piperProvider!.InferAsync(text, AudioOutputType.Raw);

            if (audioData == null || audioData.Length == 0)
            {
                _logger.LogError("Failed to synthesize audio: empty result");
                var error = new SpeechSynthesisErrorEventArgs(text, "Empty audio result");
                SynthesisError?.Invoke(this, error);
                _isSpeaking = false;
                return;
            }

            // Play audio using NAudio
            await PlayAudioAsync(audioData);

            SynthesisCompleted?.Invoke(this, new SpeechSynthesisEventArgs(text));
            _logger.LogInformation("Speech synthesis completed");
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

            if (_waveOut != null)
            {
                _waveOut.Stop();
                _waveOut.Dispose();
                _waveOut = null;
            }

            _isSpeaking = false;
            _logger.LogInformation("Speech synthesis stopped");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping speech synthesis");
        }
    }

    private async Task PlayAudioAsync(byte[] audioData)
    {
        WaveOutEvent? waveOut = null;
        
        try
        {
            // Piper outputs raw PCM audio at 22050 Hz, 16-bit, mono
            using var memoryStream = new MemoryStream(audioData);
            using var rawSource = new RawSourceWaveStream(memoryStream, new WaveFormat(22050, 16, 1));

            // Create wave output for playback
            waveOut = new WaveOutEvent();
            _waveOut = waveOut; // Store reference for StopAsync to access
            
            waveOut.Init(rawSource);

            // Set up completion handler
            var tcs = new TaskCompletionSource<bool>();
            waveOut.PlaybackStopped += (s, e) =>
            {
                if (e.Exception != null)
                {
                    _logger.LogError(e.Exception, "Playback error");
                    tcs.TrySetException(e.Exception);
                }
                else
                {
                    tcs.TrySetResult(true);
                }
            };

            // Start playback
            waveOut.Play();

            // Wait for completion
            await tcs.Task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error playing audio");
            throw;
        }
        finally
        {
            // Clean up - dispose if not already disposed by StopAsync
            if (_waveOut == waveOut)
            {
                _waveOut?.Dispose();
                _waveOut = null;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Stop playback using Task.Run to avoid deadlock from synchronization context
        try
        {
            Task.Run(() => StopAsync()).Wait(TimeSpan.FromSeconds(2));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Timeout or error during Piper TTS shutdown");
        }

        // Clean up resources
        _waveOut?.Dispose();
        (_piperProvider as IDisposable)?.Dispose();
        (_model as IDisposable)?.Dispose();

        _logger.LogInformation("PiperTextToSpeechService disposed");
    }
}
