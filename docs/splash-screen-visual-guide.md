# Splash Screen Visual Guide

## Normal Loading State

```
╔═════════════════════════════════════════════════════════════╗
║                                                       ✕     ║
║                                                             ║
║                      WatchTower                             ║
║                   (Application Title)                       ║
║                                                             ║
║                                                             ║
║                        ⚪ ⚪                                ║
║                      (Pulsing)                              ║
║                                                             ║
║                      Loading...                             ║
║                                                             ║
║                                                             ║
║                        00:15                                ║
║                   (Elapsed Time)                            ║
║                                                             ║
║                                                             ║
║                                                             ║
║                                                             ║
║                                                             ║
║  Press 'D' for diagnostics | ESC or ✕ to exit             ║
╚═════════════════════════════════════════════════════════════╝
```

## With Diagnostics Panel Visible

```
╔═════════════════════════════════════════════════════════════╗
║                                                       ✕     ║
║                                                             ║
║                      WatchTower                             ║
║                                                             ║
║                        ⚪ ⚪                                ║
║                      Loading...                             ║
║                        00:15                                ║
║                                                             ║
║  ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓  ║
║  ┃ Diagnostics                                        ┃  ║
║  ┃                                                    ┃  ║
║  ┃ [14:23:45.123] INFO: === Application Startup ===  ┃  ║
║  ┃ [14:23:45.234] INFO: Runtime: .NET 10.0.1         ┃  ║
║  ┃ [14:23:45.345] INFO: Platform: Linux 6.x          ┃  ║
║  ┃ [14:23:45.456] INFO: Phase 1/4: Loading config... ┃  ║
║  ┃ [14:23:45.567] INFO: Configuration loaded         ┃  ║
║  ┃ [14:23:45.678] INFO: Phase 2/4: Configuring DI... ┃  ║
║  ┃ [14:23:45.789] INFO: Logging services registered  ┃  ║
║  ┃ [14:23:45.890] INFO: Phase 3/4: Registering...    ┃  ║
║  ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛  ║
║                                                             ║
║  Press 'D' for diagnostics | ESC or ✕ to exit             ║
╚═════════════════════════════════════════════════════════════╝
```

## Slow Startup Warning (>30 seconds)

```
╔═════════════════════════════════════════════════════════════╗
║                                                       ✕     ║
║                                                             ║
║                      WatchTower                             ║
║                                                             ║
║                        ⚪ ⚪                                ║
║         Startup is taking longer than expected...          ║
║                                                             ║
║          ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓            ║
║          ┃ ⚠  Taking longer than usual     ┃            ║
║          ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛            ║
║                    (Orange Background)                      ║
║                                                             ║
║                        01:15                                ║
║                                                             ║
║  Press 'D' for diagnostics | ESC or ✕ to exit             ║
╚═════════════════════════════════════════════════════════════╝
```

## Startup Failed State

```
╔═════════════════════════════════════════════════════════════╗
║                                                       ✕     ║
║                                                             ║
║                      WatchTower                             ║
║                                                             ║
║                        ✕                                   ║
║                 (No Animation)                              ║
║                                                             ║
║                   Startup failed                            ║
║                                                             ║
║          ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓            ║
║          ┃ ✕  See diagnostics for details  ┃            ║
║          ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛            ║
║                    (Red Background)                         ║
║                                                             ║
║                        00:05                                ║
║                                                             ║
║  ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓  ║
║  ┃ Diagnostics                                        ┃  ║
║  ┃                                                    ┃  ║
║  ┃ [14:23:45.123] INFO: Phase 3/4: Registering...    ┃  ║
║  ┃ [14:23:45.234] ERROR: Startup failed - Invalid... ┃  ║
║  ┃   Stack: at WatchTower.Services.Startup...        ┃  ║
║  ┃                                                    ┃  ║
║  ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛  ║
║                  (Auto-shown on failure)                    ║
║                                                             ║
║  Press 'D' for diagnostics | ESC or ✕ to exit             ║
╚═════════════════════════════════════════════════════════════╝
```

## Success State (Brief Display)

```
╔═════════════════════════════════════════════════════════════╗
║                                                       ✕     ║
║                                                             ║
║                      WatchTower                             ║
║                                                             ║
║                                                             ║
║                        ✓                                   ║
║                   (Green Checkmark)                         ║
║                                                             ║
║                 Startup complete!                           ║
║                                                             ║
║                                                             ║
║                                                             ║
║                        00:02                                ║
║                                                             ║
║                                                             ║
║                                                             ║
║                                                             ║
║  Press 'D' for diagnostics | ESC or ✕ to exit             ║
╚═════════════════════════════════════════════════════════════╝
       (Shows briefly, then transitions to main window)
```

## Color Scheme

### Window
- Background: `#EE0A0A0A` (Dark, slightly transparent)
- Border: `#4AFFFFFF` (Light border)
- Corner Radius: 12px
- Shadow: `0 8 32 8 #80000000`

### Elements
- **Title (WatchTower)**: White, 36pt, Bold
- **Status Text**: White, 16pt
- **Elapsed Time**: `#AAFFFFFF` (Light white), 24pt, Monospace
- **Pulsing Indicator**: `#4AFFFFFF` (Light blue-white), animates opacity
- **Success Checkmark**: `#4AFF4A` (Green)
- **Warning Box**: `#44FFA500` background, `#FFFFA500` text (Orange)
- **Error Box**: `#44FF4444` background, `#FFFF4444` text (Red)
- **Diagnostics Panel**: `#1AFFFFFF` background, `#CCFFFFFF` text
- **Hint Text**: `#66FFFFFF` (Faded white), 11pt, Italic

### Animation
- **Pulsing Circle**: Opacity cycles from 0.2 to 1.0 over 1.5 seconds, alternating
- **Smooth Transitions**: 300ms cubic-ease animations

## Interactions

### Keyboard
- `D` - Toggle diagnostics panel visibility
- `ESC` - Exit application immediately
- No other keys active during splash

### Mouse
- Click `✕` button (top-right) - Exit application
- No other clickable elements
- Window cannot be moved or resized

## Responsive Behavior

### Timer
- Updates every 100ms
- Format switches based on duration:
  - < 1 hour: `MM:SS` (e.g., "00:15", "05:42")
  - ≥ 1 hour: `HH:MM:SS` (e.g., "01:23:45")

### Diagnostics
- Maximum 500 messages (rolling window)
- Auto-scrolls to bottom
- Each message timestamped to millisecond
- Formatted as: `[HH:mm:ss.fff] LEVEL: Message`

### State Transitions
1. **Start**: Normal loading state
2. **30s+**: Slow startup warning appears
3. **Success**: Checkmark shows, 500ms delay, transition to main window
4. **Failure**: Error state, diagnostics auto-show, remains visible

## Accessibility

- High contrast text (white on dark)
- Clear status messages
- Keyboard navigation (D, ESC)
- Visual state indicators (colors, icons)
- Diagnostic messages for screen readers

## Performance

- Initial display: < 100ms
- Timer overhead: ~0.1% CPU (100ms ticks)
- Memory: ~2MB for window + diagnostics (500 messages max)
- No network calls
- No heavy rendering (simple shapes and text)
