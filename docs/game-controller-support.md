# Game Controller Support

WatchTower now includes foundational support for game controller input, allowing navigation and interaction using standard gamepads (Xbox, PlayStation, generic USB controllers).

## Features

### Implemented

✅ **Core Infrastructure**
- Cross-platform game controller service architecture
- Event-based button press/release notifications
- Controller connection/disconnection detection
- Standardized button mapping (Xbox/PlayStation compatible)
- Analog stick and trigger support
- Real-time controller state polling

✅ **MVVM Integration**
- Service registered in dependency injection container
- ViewModel demonstrates controller event handling
- UI binds to controller state and events
- Event logging for debugging

✅ **Developer Features**
- Comprehensive logging support
- Mock implementation for testing without hardware
- Clean service interface for easy extension

### Architecture

The game controller support follows WatchTower's MVVM architecture:

```
┌─────────────────────────────────────┐
│        Application Layer            │
│  (ViewModels subscribe to events)   │
└────────────────┬────────────────────┘
                 │
┌────────────────▼────────────────────┐
│    IGameControllerService           │
│  (Interface with events)            │
└────────────────┬────────────────────┘
                 │
┌────────────────▼────────────────────┐
│   GameControllerService             │
│  (Polling, state management)        │
└────────────────┬────────────────────┘
                 │
┌────────────────▼────────────────────┐
│  Platform Backend (Future)          │
│  (SDL2, XInput, DirectInput)        │
└─────────────────────────────────────┘
```

## Button Mapping

Standard button layout compatible with Xbox and PlayStation controllers:

| Button | Xbox | PlayStation | Function |
|--------|------|-------------|----------|
| A | A | Cross (✕) | Confirm/Select |
| B | B | Circle (○) | Back/Cancel |
| X | X | Square (□) | Alternative action |
| Y | Y | Triangle (△) | Menu |
| DPad Up/Down/Left/Right | D-Pad | D-Pad | Navigation |
| Left/Right Shoulder | LB/RB | L1/R1 | Quick actions |
| Left/Right Trigger | LT/RT | L2/R2 | Analog triggers |
| Start | Menu | Options | Pause/Menu |
| Back | View | Share | Secondary menu |
| Guide | Xbox button | PS button | System menu |

## Usage

### Basic Setup

The game controller service is automatically initialized at application startup:

```csharp
// In App.axaml.cs - Already configured
services.AddSingleton<IGameControllerService, GameControllerService>();
_gameControllerService = _serviceProvider.GetRequiredService<IGameControllerService>();
_gameControllerService.Initialize();
```

### Using in ViewModels

```csharp
public class MyViewModel : INotifyPropertyChanged
{
    private readonly IGameControllerService _gameControllerService;
    
    public MyViewModel(IGameControllerService gameControllerService)
    {
        _gameControllerService = gameControllerService;
        
        // Subscribe to button events
        _gameControllerService.ButtonPressed += OnButtonPressed;
        _gameControllerService.ButtonReleased += OnButtonReleased;
        _gameControllerService.ControllerConnected += OnControllerConnected;
        _gameControllerService.ControllerDisconnected += OnControllerDisconnected;
    }
    
    private void OnButtonPressed(object? sender, GameControllerButtonEventArgs e)
    {
        // Handle button press
        switch (e.Button)
        {
            case GameControllerButton.A:
                // Confirm action
                break;
            case GameControllerButton.B:
                // Cancel/Back
                break;
            case GameControllerButton.DPadUp:
                // Navigate up
                break;
            // ... more cases
        }
    }
}
```

### Checking Controller State

```csharp
// Get list of connected controllers
var controllers = _gameControllerService.ConnectedControllers;

// Get specific controller state
var state = _gameControllerService.GetControllerState(0);
if (state != null)
{
    // Check button states
    bool isAPressed = state.ButtonStates[GameControllerButton.A];
    
    // Check analog inputs
    float leftStickX = state.LeftStickX; // -1.0 to 1.0
    float leftStickY = state.LeftStickY;
    float leftTrigger = state.LeftTrigger; // 0.0 to 1.0
}
```

## Current Implementation Status

### Mock Implementation

The current implementation is a **mock/foundation** version that provides:
- Complete service architecture and interfaces
- Event system for button presses/releases
- Controller state management
- Logging and debugging support

**Note:** Actual hardware controller detection requires platform-specific backend implementation (SDL2, XInput, etc.).

### Testing Without Hardware

The service includes internal methods for simulating controller input:

```csharp
// For testing purposes (internal use)
gameControllerService.SimulateButtonPress(0, GameControllerButton.A);
gameControllerService.SimulateButtonRelease(0, GameControllerButton.A);
```

## Future Enhancements

### Platform-Specific Backends

- [ ] **SDL2 Backend** - Cross-platform support (Windows/macOS/Linux)
- [ ] **XInput Backend** - Windows native Xbox controller support
- [ ] **DirectInput Backend** - Legacy Windows controller support
- [ ] **IOKit Backend** - macOS native support
- [ ] **evdev Backend** - Linux native support

### Navigation Features

- [ ] **Focus Navigation** - D-Pad controls UI element focus
- [ ] **Button-to-Action Mapping** - Configurable button bindings
- [ ] **Haptic Feedback** - Vibration/rumble support
- [ ] **Dead Zone Configuration** - Analog stick dead zone settings

### Advanced Features

- [ ] **Multiple Controller Support** - Handle multiple connected controllers
- [ ] **Controller Profiles** - Save/load custom button mappings
- [ ] **Input Recording** - Record and replay controller input
- [ ] **Gesture Recognition** - Complex button combinations

## Development Guidelines

### Adding Platform-Specific Backend

1. Create platform-specific implementation inheriting from `GameControllerService`
2. Implement platform-specific controller detection
3. Call base class methods for event raising
4. Register appropriate implementation based on platform:

```csharp
#if WINDOWS
services.AddSingleton<IGameControllerService, XInputGameControllerService>();
#elif OSX
services.AddSingleton<IGameControllerService, IOKitGameControllerService>();
#elif LINUX
services.AddSingleton<IGameControllerService, EvdevGameControllerService>();
#else
services.AddSingleton<IGameControllerService, GameControllerService>(); // Mock
#endif
```

### Following MVVM Pattern

**DO:**
- ✅ Handle all controller logic in ViewModels
- ✅ Use data binding for UI updates
- ✅ Keep Views (XAML) presentation-only
- ✅ Subscribe to service events in ViewModel constructor

**DON'T:**
- ❌ Put controller logic in code-behind
- ❌ Access GameControllerService from Views
- ❌ Create tight coupling between UI and service

## Troubleshooting

### Controllers Not Detected

**Current behavior:** The mock implementation won't detect physical controllers.

**Solution:** 
- Platform-specific backend implementation is required
- Or use `SimulateButtonPress` for testing

### Events Not Firing

**Check:**
1. Service is initialized: `gameControllerService.Initialize()`
2. Update is being called regularly (automatic via timer)
3. Event handlers are subscribed correctly
4. No exceptions in event handlers (check logs)

## Logging

Set logging level in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": "verbose"  // minimal | normal | verbose
  }
}
```

Verbose logging will show:
- Controller connection/disconnection
- Button press/release events
- Service initialization status
- Update loop activity

## Related Files

- `Models/GameControllerButton.cs` - Button enumeration
- `Models/GameControllerState.cs` - Controller state model
- `Services/IGameControllerService.cs` - Service interface
- `Services/GameControllerService.cs` - Service implementation
- `ViewModels/MainWindowViewModel.cs` - Example usage
- `Views/MainWindow.axaml` - Example UI

## References

- [Avalonia Input Documentation](https://docs.avaloniaui.net/docs/input/)
- [Xbox Controller Button Layout](https://docs.microsoft.com/en-us/windows/uwp/gaming/gamepad-and-vibration)
- [SDL GameController API](https://wiki.libsdl.org/SDL_GameController)

---

**Version:** 1.0.0 (Foundation)  
**Status:** Ready for platform backend implementation  
**Last Updated:** 2025-12-18
