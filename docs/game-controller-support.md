# Game Controller Support

WatchTower includes full hardware support for game controller input, allowing navigation and interaction using standard gamepads (Xbox, PlayStation, generic USB controllers).

## Features

### Implemented

✅ **SDL2 Hardware Support**
- Gamepad detection via Silk.NET.SDL
- SDL Game Controller Database for automatic button mapping
- Hot-plug support for controller connect/disconnect
- Support for Xbox, PlayStation, and generic USB controllers
- Windows compatibility

✅ **Input Processing**
- Event-based button press/release notifications
- Radial dead zone processing (configurable, default 15%)
- Analog stick and trigger support with proper axis orientation
- 60 FPS polling synchronized with UI rendering
- Real-time controller state tracking

✅ **MVVM Integration**
- Service registered in dependency injection container
- ViewModel demonstrates controller event handling
- UI binds to controller state and events
- Event logging for debugging

✅ **Developer Features**
- Comprehensive logging support
- Configurable dead zone threshold via appsettings.json
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
│  (SDL2-based implementation)        │
└────────────────┬────────────────────┘
                 │
┌────────────────▼────────────────────┐
│     Silk.NET.SDL (v2.22.0)          │
│        (SDL2 bindings)              │
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

### SDL2 Hardware Integration

The current implementation provides **full hardware gamepad support** using SDL2:
- SDL2 gamepad detection and enumeration
- SDL Game Controller Database for automatic button mapping
- Hot-plug detection for controller connect/disconnect events
- Windows support
- 60 FPS polling synchronized with UI rendering

**Hardware Tested:**
- Xbox controllers (360, One, Series X|S)
- PlayStation controllers (DualShock 4, DualSense)
- Generic USB controllers with SDL2 mapping

### Configuration

Dead zone threshold can be configured in `appsettings.json`:

```json
{
  "Gamepad": {
    "DeadZone": 0.15  // 15% radial dead zone (range: 0.0 - 1.0)
  }
}
```

## Future Enhancements

### Navigation Features

- [ ] **XYFocus Navigation** - D-Pad/analog stick controls UI element focus via Avalonia's XYFocus system
- [ ] **Button-to-Command Mapping** - Configurable button bindings for application actions
- [ ] **Haptic Feedback** - Vibration/rumble support via SDL2
- [ ] **Input Hold Detection** - Distinguish between tap, hold, and repeat

### Advanced Features

- [ ] **Multiple Controller Support** - Enhanced UI for managing multiple connected controllers
- [ ] **Controller Profiles** - Save/load custom button mappings per controller
- [ ] **Input Recording** - Record and replay controller input sequences
- [ ] **Gesture Recognition** - Complex button combination detection
- [ ] **Controller Customization UI** - Visual button remapping interface

## Development Guidelines

### Extending the Service

The `GameControllerService` is designed for extension. To add custom functionality:

1. Inherit from `GameControllerService` or implement `IGameControllerService`
2. Override `Update()` method for custom polling logic
3. Use existing event system for button/connection notifications
4. Register custom implementation in DI container
### SDL2 Technical Details

The implementation uses the following SDL2 APIs:

```csharp
// Initialization
SDL_Init(SDL_INIT_GAMECONTROLLER)
SDL_NumJoysticks()
SDL_IsGameController(deviceIndex)
SDL_GameControllerOpen(deviceIndex)

// Input Polling
SDL_PollEvent(&event)  // Connection events
SDL_GameControllerGetButton(controller, button)
SDL_GameControllerGetAxis(controller, axis)

// Cleanup
SDL_GameControllerClose(controller)
SDL_QuitSubSystem(SDL_INIT_GAMECONTROLLER)
```

**Dead Zone Algorithm:**
```csharp
magnitude = sqrt(x² + y²)
if (magnitude < deadZone) return (0, 0)
normalized = (magnitude - deadZone) / (1.0 - deadZone)
scale = normalized / magnitude
return (x * scale, y * scale)
```

**Y-Axis Inversion:**
SDL returns Y-axis values where positive = down. The service inverts this to standard convention (positive = up) for consistency with typical game engines.

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

**Possible causes:**
1. SDL2 native libraries not found
2. Controller not compatible with SDL Game Controller Database
3. Permissions issues

**Solutions:**
- Ensure SDL2 native libraries are deployed with the application
- Check controller compatibility at [SDL_GameControllerDB](https://github.com/gabomdq/SDL_GameControllerDB)
- Check logs for initialization errors

### Events Not Firing

**Check:**
1. Service is initialized successfully: Check logs for "SDL2 game controller service initialized"
2. Polling timer is running: Should see "Gamepad polling started at 60 FPS"
3. Event handlers are subscribed correctly in ViewModel
4. No exceptions in event handlers (check console output)
5. Controller is actually connected (check `ConnectedControllers` property)

### Controller Lag or Missed Inputs

**Possible causes:**
- Polling rate too low
- UI thread blocking

**Solutions:**
- Verify polling timer is at 60 FPS (16ms interval)
- Check for long-running operations on UI thread
- Ensure `Update()` completes quickly

### Incorrect Button Mapping

**Possible causes:**
- Controller not in SDL Game Controller Database
- Custom/modified controller

**Solutions:**
- Update to latest SDL2 binaries with current database
- Add custom mapping to SDL_GameControllerDB
- Test with known controllers (Xbox, PlayStation)

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

**Version:** 2.0.0 (SDL2 Hardware Support)  
**Status:** Production Ready - Full hardware integration  
**Last Updated:** 2025-12-18
