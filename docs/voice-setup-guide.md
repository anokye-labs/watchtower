# Voice Feature Setup Guide

## Overview

WatchTower supports full-duplex voice capabilities with both **offline** and **online** modes:

- **Offline Mode** (default): Uses Vosk for speech recognition and Piper for text-to-speech. No internet required.
- **Online Mode**: Uses Azure Cognitive Services for both speech recognition and synthesis. Requires internet and API keys.
- **Hybrid Mode**: *(Not yet implemented)* Intended to prefer offline with online fallback; behaves the same as offline mode.

**Platform:** Voice features use NAudio for native Windows audio integration, providing reliable and high-performance audio capture/playback on Windows 10/11.

## Quick Start (Offline Mode)

### 1. Download Voice Models

#### Vosk Speech Recognition Model

1. Visit: https://alphacephei.com/vosk/models
2. Download the English model (recommended: `vosk-model-small-en-us-0.15.zip` - 40MB)
3. Extract to: `WatchTower/models/vosk-model-small-en-us-0.15/`

```bash
mkdir -p WatchTower/models
cd WatchTower/models
wget https://alphacephei.com/vosk/models/vosk-model-small-en-us-0.15.zip
unzip vosk-model-small-en-us-0.15.zip
```

#### Piper TTS Voice Model

1. Visit: https://huggingface.co/rhasspy/piper-voices/tree/main
2. Navigate to `en/en_US/lessac/medium/`
3. Download both files:
   - `en_US-lessac-medium.onnx`
   - `en_US-lessac-medium.onnx.json`
4. Place in: `WatchTower/models/piper/`

```bash
mkdir -p WatchTower/models/piper
cd WatchTower/models/piper
wget https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/lessac/medium/en_US-lessac-medium.onnx
wget https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/lessac/medium/en_US-lessac-medium.onnx.json
```

### 2. Configure Voice Settings

Edit `appsettings.json`:

```json
{
  "Voice": {
    "Mode": "offline",
    "Vosk": {
      "ModelPath": "models/vosk-model-small-en-us-0.15"
    },
    "Piper": {
      "ModelPath": "models/piper",
      "Voice": "en_US-lessac-medium"
    }
  }
}
```

### 3. Run the Application

```bash
dotnet run --project WatchTower/WatchTower.csproj
```

The voice services will initialize automatically on startup!

## Online Mode Setup (Azure)

### 1. Get Azure Speech Services Credentials

1. Sign up at: https://portal.azure.com
2. Create a "Speech Services" resource
3. Copy your **Key** and **Region** (e.g., "westus", "eastus")

### 2. Configure Credentials

**Option A: Using .env file (recommended)**

Create `.env` file in project root:

```env
AZURE_SPEECH_KEY=your_key_here
AZURE_SPEECH_REGION=your_region_here
```

**Option B: Using appsettings.json**

```json
{
  "Voice": {
    "Mode": "online",
    "Azure": {
      "SpeechKey": "your_key_here",
      "SpeechRegion": "your_region_here",
      "Language": "en-US",
      "VoiceName": "en-US-AriaNeural"
    }
  }
}
```

### 3. Update Service Registration

The orchestration service needs to be configured to use Azure services for online mode.

## Configuration Options

### Voice Modes

- `"offline"` - Use only Vosk and Piper (no internet required)
- `"online"` - Use only Azure Speech Services (requires API keys)
- `"hybrid"` - *(Not yet implemented)* Behaves the same as offline mode

### Vosk Configuration

```json
{
  "Voice": {
    "Vosk": {
      "ModelPath": "models/vosk-model-small-en-us-0.15"
    }
  }
}
```

**Available Models:**
- Small models (40-50MB): Good for commands, lower accuracy
- Medium models (1.5GB): Better accuracy for general speech
- Large models (3GB+): Highest accuracy, more resource-intensive

### Piper Configuration

```json
{
  "Voice": {
    "Piper": {
      "ModelPath": "models/piper",
      "Voice": "en_US-lessac-medium"
    }
  }
}
```

**Popular Voices:**
- `en_US-lessac-medium` - Clear, professional male voice
- `en_US-amy-medium` - Natural female voice
- `en_GB-alba-medium` - British English female voice

Browse all voices: https://rhasspy.github.io/piper-samples/

### Azure Configuration

```json
{
  "Voice": {
    "Azure": {
      "SpeechKey": "your_key_here",
      "SpeechRegion": "westus",
      "Language": "en-US",
      "VoiceName": "en-US-AriaNeural"
    }
  }
}
```

**Popular Neural Voices:**
- `en-US-AriaNeural` - Female, conversational
- `en-US-GuyNeural` - Male, clear
- `en-US-JennyNeural` - Female, warm
- `en-GB-SoniaNeural` - British female

Full voice list: https://learn.microsoft.com/en-us/azure/ai-services/speech-service/language-support

## Usage Example

Once configured, the voice system can be used in your ViewModels:

```csharp
// Inject the voice orchestration service
private readonly IVoiceOrchestrationService _voiceService;

// Initialize
await _voiceService.InitializeAsync();

// Start listening
await _voiceService.StartListeningAsync();

// Speak text
await _voiceService.SpeakAsync("Hello, I can speak!");

// Full-duplex mode (listen and speak simultaneously)
await _voiceService.StartFullDuplexAsync();
```

## Troubleshooting

### "Model not found" Error

- Verify the model files exist in the specified paths
- Check that the paths in `appsettings.json` are correct
- Ensure model files are not corrupted

### No Audio Input/Output

- Check microphone permissions in your OS
- Verify default audio devices are set correctly
- Try running with `sudo` on Linux if audio access is denied

### Azure Authentication Failed

- Verify your API key and region are correct
- Check that your Azure subscription is active
- Ensure the Speech Services resource is not rate-limited

### Poor Recognition Accuracy

- Use a better quality microphone
- Reduce background noise
- Try a larger Vosk model for better accuracy
- For Azure, speak clearly and at a moderate pace

## Performance Notes

### Offline Mode
- **Vosk**: ~100-200ms latency, 100-500MB RAM
- **Piper**: ~200-500ms synthesis time, 100-300MB RAM
- **Total**: Suitable for real-time conversation

### Online Mode
- **Azure Speech**: ~300-800ms latency (depends on internet)
- **Bandwidth**: ~1-2 Kbps for recognition, 5-10 Kbps for synthesis
- **Quality**: Higher accuracy than most offline models

## Security Best Practices

- **Never commit** API keys to git
- Use `.env` files (already in `.gitignore`)
- Rotate Azure keys regularly
- Use managed identities in production if deployed to Azure

## Next Steps

- [ ] Integrate voice services in StartupOrchestrator
- [ ] Create a ViewModel for voice control
- [ ] Add UI controls for voice features
- [ ] Test full-duplex mode
- [ ] Add voice command recognition
- [ ] Implement voice activity visualization

## Resources

- [Vosk Documentation](https://alphacephei.com/vosk/)
- [Piper TTS Documentation](https://github.com/rhasspy/piper)
- [Azure Speech Services](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/)
- [NAudio Documentation](https://github.com/naudio/NAudio)
