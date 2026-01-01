# Shell Window Frame Interaction Guide

## Overview

The Shell Window frame system provides an interactive, decorative frame around the application content with alpha-based click-through and window management capabilities. This guide explains how the frame interaction system works and how to configure it.

## Features

### 1. Alpha-Based Click-Through

The frame uses alpha transparency to determine whether clicks should:
- **Pass through** to content below (transparent regions)
- **Be captured** for window interaction (opaque regions)

This is controlled by the `ClickThroughAlphaThreshold` configuration value (default: 0.75). Pixels with alpha values above this threshold are considered opaque and capture clicks.

### 2. Window Resizing

When clicking on opaque frame regions near window edges or corners, the window can be resized:

- **Corner regions**: Enable diagonal resize (both width and height)
  - Top-Left, Top-Right, Bottom-Left, Bottom-Right
- **Edge regions**: Enable single-axis resize
  - Top/Bottom: Height only
  - Left/Right: Width only

The resize handle size is configurable via `ResizeHandleSize` (default: 8 logical pixels).

### 3. Window Dragging

Clicking and holding on opaque frame regions that are not in resize zones allows dragging the window to reposition it on the screen.

### 4. Cursor Feedback

The cursor automatically changes to indicate the available interaction:
- **Resize cursors**: Shown when hovering over resize zones
  - TopLeftCorner, TopRightCorner, BottomLeftCorner, BottomRightCorner
  - TopSide, BottomSide, LeftSide, RightSide
- **Arrow cursor**: Shown over non-interactive regions

## Configuration

Frame interaction is configured in `appsettings.json` under the `Frame` section:

```json
{
  "Frame": {
    "SourceUri": "avares://WatchTower/Assets/main-frame.png",
    "Scale": 0.20,
    "BackgroundColor": "#261208",
    "ClickThroughAlphaThreshold": 0.75,
    "EnableWindowedMode": true,
    "ResizeHandleSize": 8,
    "Padding": {
      "Left": 80,
      "Top": 60,
      "Right": 80,
      "Bottom": 60
    },
    "Slice": {
      "Left": 1330,
      "LeftInner": 2600,
      "RightInner": 4280,
      "Right": 5560,
      "Top": 955,
      "TopInner": 1400,
      "BottomInner": 2415,
      "Bottom": 2860
    }
  }
}
```

### Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ClickThroughAlphaThreshold` | double | 0.75 | Alpha threshold (0.0-1.0) above which pixels are considered opaque and capture clicks |
| `EnableWindowedMode` | bool | true | Enables/disables frame-based window resizing and dragging |
| `ResizeHandleSize` | double | 8.0 | Size of resize handle zones in logical pixels (minimum: 4.0) |

## Architecture

### Components

1. **IFrameHitTestService** / **FrameHitTestService**
   - Performs hit testing on frame regions
   - Determines if a point is on an opaque frame region
   - Identifies resize zones and appropriate cursors

2. **ShellWindowViewModel**
   - Manages frame configuration properties
   - Provides access to frame slices and slice definitions
   - Exposes alpha threshold, windowed mode, and resize handle size

3. **ShellWindow.axaml.cs**
   - Handles pointer events (Pressed, Moved, Released)
   - Implements window dragging and resizing logic
   - Updates cursor based on frame region
   - Respects minimum window size constraints

4. **ShellWindow.axaml**
   - Frame grid with pointer event handlers
   - 5x5 grid layout with 16 border pieces

### Hit Testing Flow

```
1. Pointer Event on Frame Grid
   ↓
2. Get pointer position (window coordinates)
   ↓
3. PerformFrameHitTest()
   - Check if point is in frame border region
   - Determine resize mode based on proximity to edges/corners
   ↓
4. Return FrameHitTestResult
   - IsOpaque: true/false
   - ResizeMode: None, Top, TopLeft, etc.
   - CursorType: Appropriate cursor for region
   ↓
5. Handle based on result
   - Non-opaque: Pass event through (e.Handled = false)
   - Opaque: Capture event for window interaction (e.Handled = true)
```

### Resize/Drag Operations

**Resize Flow:**
```
1. PointerPressed on opaque frame region near edge/corner
   ↓
2. Set _isResizing = true, capture resize mode
   ↓
3. PointerMoved events
   - Calculate delta from start position
   - Apply resize based on mode (Top, Bottom, Left, Right, or corners)
   - Respect minimum window size
   ↓
4. PointerReleased
   - Set _isResizing = false
```

**Drag Flow:**
```
1. PointerPressed on opaque frame region (not near edge/corner)
   ↓
2. Set _isDragging = true
   ↓
3. PointerMoved events
   - Calculate delta from start position
   - Update window position
   ↓
4. PointerReleased
   - Set _isDragging = false
```

## Minimum Window Size

The window respects minimum size constraints during resize:

- **Content area**: `MinContentWidth` and `MinContentHeight` from `Startup` config
- **Frame dimensions**: Calculated from frame slice definition and scale
- **Total minimum**: Content + Frame + Padding

This ensures the window never becomes too small to display content properly.

## Cross-Platform Considerations

The frame interaction system is designed to work consistently across all supported platforms:

- **Windows**: Full support for resize and drag operations
- **macOS**: Full support for resize and drag operations
- **Linux**: Full support for resize and drag operations

The system uses Avalonia's cross-platform APIs for window manipulation, ensuring consistent behavior.

## Disabling Frame Interaction

To disable frame-based window interaction (reverting to pass-through-only behavior):

```json
{
  "Frame": {
    "EnableWindowedMode": false
  }
}
```

When disabled:
- All clicks pass through the frame to content below
- No window resizing or dragging via frame
- Standard window controls (if any) remain functional

## Future Enhancements

Potential future improvements to the frame interaction system:

1. **Actual pixel alpha sampling**: Currently uses a simplified heuristic approach. Could be enhanced to read actual pixel alpha values from bitmaps for more accurate hit testing.

2. **Configurable resize zones per frame region**: Allow defining which frame regions support resize operations.

3. **Double-click behaviors**: Maximize/restore on double-click of frame regions.

4. **Snap to screen edges**: Automatic alignment when dragging near screen boundaries.

5. **Multi-monitor improvements**: Enhanced behavior when dragging across monitors with different DPI settings.

## See Also

- [Architecture Guide](ARCHITECTURE.md) - Overall application architecture
- [Splash Screen Startup](splash-screen-startup.md) - Splash screen and shell window initialization
- Configuration: `WatchTower/appsettings.json`
