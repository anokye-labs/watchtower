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

## Configuration

### Window Sizing & Animation

The splash window size and animation duration can be configured in `appsettings.json`:

```json
{
  "Startup": {
    "MinContentWidth": 400,
    "MinContentHeight": 300,
    "AnimationDurationMs": 1000
  }
}
```

**Configuration Parameters:**

- **`MinContentWidth`** (default: `400`)
  - Minimum width of the content area inside the frame (logical pixels)
  - Valid range: `100` to `2000` pixels
  - The actual window width will be: `FrameStaticWidth + Padding + MinContentWidth`
  - Example: `400` = comfortable space for splash content (logo, spinner, status)
  - Example: `600` = larger content area for more detailed splash screens
  - Application throws `InvalidOperationException` if value is outside valid range

- **`MinContentHeight`** (default: `300`)
  - Minimum height of the content area inside the frame (logical pixels)
  - Valid range: `100` to `2000` pixels
  - The actual window height will be: `FrameStaticHeight + Padding + MinContentHeight`
  - Example: `300` = comfortable space for vertical splash layout
  - Example: `450` = taller content area for additional information
  - Application throws `InvalidOperationException` if value is outside valid range

### Frame-Based Sizing

The application calculates the splash window size based on the **static (non-stretching) components** of the decorative frame, ensuring the frame always looks proportional without excessive stretching.

**Size Calculation Formula:**

```
WindowWidth  = (Col0 + Col2 + Col4) × FrameScale / DPI + PaddingLR + MinContentWidth
WindowHeight = (Row0 + Row2 + Row4) × FrameScale / DPI + PaddingTB + MinContentHeight
```

Where:
- **Col0, Col2, Col4**: Fixed-width columns (left edge, center, right edge) from source image
- **Row0, Row2, Row4**: Fixed-height rows (top edge, center, bottom edge) from source image
- **FrameScale**: Display scaling from `Frame.Scale` config (default: 0.20 = 20% of source)
- **DPI**: Screen DPI scaling factor (1.0, 1.5, 2.0, etc.)
- **PaddingLR/TB**: Frame padding from `Frame.Padding` config (default: 160px horizontal, 120px vertical)
- **MinContentWidth/Height**: Minimum content area from `Startup` config (default: 400×300)

**Example Calculation (1080p @ 100% DPI):**

With default config:
- Frame source: 6880×3800 pixels
- Frame slices: Col0=1330, Col2=1680, Col4=1320, Row0=955, Row2=1015, Row4=940
- FrameScale: 0.25
- DPI: 1.0

**Animation Behavior:**
1. Window opens at frame-based size (static components + min content area) centered on primary screen
2. Splash screen displays with loading animation
3. After startup completes, window animates to fullscreen over configured duration (default 1000ms)
4. Uses cubic ease-out easing for smooth deceleration

**Ctrl+F5 Replay:**
- Contracts from fullscreen to frame-based splash size (1000ms)
- Pauses for 200ms
- Expands back to fullscreen (1000ms)

**Monitor Switch:**
- Zooms down to frame-based splash size on new monitor (250ms)
- Recalculates frame size for new monitor's DPI
- Zooms back up to fullscreen on new monitor (250ms)
- Properly handles different DPI on different monitors

**Size Constraints:**
- Splash window is clamped to maximum 90% of screen size
- Ensures window never exceeds screen bounds even with large MinContent values
- Frame components and padding are always fully visible on all screen scaling settings

**DPI Scaling Considerations:**
- Calculating frame component sizes in logical pixels accounting for both FrameScale and DPI
- Converting back to physical pixels for window positioning
- Ensuring correct centering on multi-monitor setups with different DPI settings

**Example DPI Calculations (with frame-based sizing):**

| Display | DPI | Scaling | Frame Logical Size | Content Area | Total Window Size (Logical) |
|---------|-----|---------|-------------------|--------------|---------------------------|
| 1080p   | 96  | 1.0×    | 1082.5×727.5      | 400×300      | 1642.5×1147.5             |
| 1080p   | 144 | 1.5×    | 721.7×485.0       | 400×300      | 1281.7×905.0              |
| 4K      | 192 | 2.0×    | 541.25×363.75     | 400×300      | 1101.25×783.75            |

The window always appears at the correct size regardless of DPI scaling, with proper centering that accounts for:
- Screen offsets (multi-monitor setups)
- Taskbar/dock positions (uses `WorkingArea` not `Bounds`)
- Different DPI on different monitors (recalculates on monitor switch)
- Maximum size constraint (90% of screen to prevent oversized windows on small displays)

### Animation Behavior

**Initial Launch:**
1. Window opens at frame-based splash size (frame components + MinContentWidth/Height) centered on primary screen
2. Splash screen displays with loading animation
3. After startup completes, window animates to fullscreen over 500ms duration
4. Uses cubic ease-out easing for smooth deceleration

**Ctrl+F5 Replay:**
- Contracts from fullscreen to splash size (500ms)
- Pauses for 100ms
- Expands back to fullscreen (500ms)

**Monitor Switch:**
- Smoothly resizes directly to new screen's working area (300ms)
- Properly handles different DPI on different monitors

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
