# Window Position Persistence - Manual Testing Guide

## Overview
The window position persistence feature remembers the display/monitor where WatchTower was last shown and automatically reopens on that same display when launched again.

## What Gets Saved
- Window X and Y position (in logical coordinates)
- Window width and height (in logical pixels)
- Display bounds (X, Y, Width, Height in physical pixels) for display identification

## Where It's Saved
The preferences are stored in:
- **Windows**: `%APPDATA%\WatchTower\user-preferences.json`

## Manual Testing Scenarios

### Scenario 1: Basic Position Persistence (Single Monitor)
1. Launch WatchTower
2. After the window expands to fill the screen, resize it so it does not occupy the entire working area
3. Move the resized window to a different position on the screen
4. Close WatchTower
5. Relaunch WatchTower
6. **Expected**: The restored window (before any expansion animation to full screen) should appear at the position and size where it was closed

> Note: By default the window animates to fill the working area after launch. This scenario verifies the saved position of the initial (non-maximized) window, not the final full-screen state.

### Scenario 2: Multi-Monitor Position Persistence
1. Ensure you have multiple monitors connected
2. Launch WatchTower (should open on primary monitor)
3. Drag the window to a secondary monitor
4. Close WatchTower
5. Relaunch WatchTower
6. **Expected**: Window should appear on the secondary monitor at the saved position

### Scenario 3: Monitor Switch During Session
1. Launch WatchTower on primary monitor
2. Drag the window to a secondary monitor
3. Wait for the monitor switch animation to complete (window expands to fill new screen)
4. Close WatchTower
5. Relaunch WatchTower
6. **Expected**: Window should appear on the secondary monitor

### Scenario 4: Display No Longer Available (Graceful Fallback)
1. With multiple monitors, launch WatchTower and move it to a secondary monitor
2. Close WatchTower
3. Disconnect/disable the secondary monitor
4. Relaunch WatchTower
5. **Expected**: Window should fall back to primary monitor with default centering behavior

### Scenario 5: First Launch (No Saved Preferences)
1. Delete the user preferences file (see "Where It's Saved" section above)
2. Launch WatchTower
3. **Expected**: Window should appear centered on the primary monitor

### Scenario 6: Display Configuration Changes
1. Launch WatchTower with monitors in one arrangement
2. Close WatchTower
3. Change monitor arrangement (e.g., swap primary/secondary)
4. Relaunch WatchTower
5. **Expected**: If saved display exists at new coordinates, window appears there; otherwise falls back to primary

## Verification Checklist

- [ ] Window position persists across app restarts (single monitor)
- [ ] Window opens on the correct monitor in multi-monitor setup
- [ ] Position is saved when closing the application
- [ ] Position is saved when moving to a different monitor
- [ ] Graceful fallback when saved monitor is unavailable
- [ ] Works correctly on Windows
- [ ] No exceptions or errors in console logs
- [ ] Preferences file is created and updated correctly

## Inspecting Saved Preferences

You can examine the saved preferences by opening `user-preferences.json` in a text editor. Look for the `windowPosition` section:

```json
{
  "themeMode": "System",
  "windowPosition": {
    "x": 100.0,
    "y": 200.0,
    "width": 800.0,
    "height": 600.0,
    "displayBounds": {
      "x": 0,
      "y": 0,
      "width": 1920,
      "height": 1080
    }
  }
}
```

## Known Behaviors

1. **Window starts at splash size**: On first load (with no saved preferences), the window appears at a calculated splash size, then animates to full screen
2. **Saved position applies to expanded window**: After preferences exist, the saved position and size are applied to the fully expanded window on startup. If the saved size matches the working area, the expansion animation is skipped automatically
3. **Monitor switch animation**: When dragging to a new monitor, the window smoothly animates to fill the new screen's working area
4. **Display identification**: Displays are identified by their bounds (position and size), which should be unique in most configurations. For mirrored displays with identical bounds, the first matching display is used
5. **Position validation**: The system validates that saved positions are reasonable (positive dimensions, mostly visible on screen) before restoring them
