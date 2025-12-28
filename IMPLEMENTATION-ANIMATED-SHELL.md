# Animated Splash Frame Expansion Implementation

## Overview

This implementation replaces the separate SplashWindow and MainWindow with a unified ShellWindow that:
1. Opens at frame-based splash size (calculated from frame static components + minimum content area)
2. Displays splash content during initialization
3. Animates smoothly to full-screen over 500ms using Avalonia's Animation system
4. Transitions to main application content

## Key Components

### ShellWindow (`Views/ShellWindow.axaml`)
- Root window with decorative frame background (`main-frame.png`)
- ContentControl that switches between splash and main content
- Frame uses 5x5 grid slicing for resolution-independent scaling
- Configurable padding for content area inside frame

### ShellWindowViewModel (`ViewModels/ShellWindowViewModel.cs`)
- Manages state transitions between splash and main modes
- Forwards IStartupLogger calls to SplashWindowViewModel
- Handles content switching and animation coordination

### Animation Logic (`Views/ShellWindow.axaml.cs`)
- **SetSplashSize()**: Calculates initial window size based on frame static components + minimum content area
- **AnimateExpansionAsync()**: Smoothly animates width, height, and position over 500ms using Avalonia's Animation system
- Uses cubic ease-out easing for natural motion
- Properly handles DPI scaling and multi-monitor setups

### Startup Flow (`App.axaml.cs`)
1. Create ShellWindow with SplashWindowViewModel content
2. Execute startup orchestration (service initialization)
3. Mark startup complete
4. **AnimateExpansionAsync()** - window expands to full-screen
5. **TransitionToMainContent()** - switch to MainWindowViewModel

## Frame Asset

### Current Implementation
The implementation uses a PNG frame (`Assets/main-frame.png`) that provides:
- Golden/bronze ornate corners with Art Deco patterns
- Configurable border thickness via slice coordinates
- Transparent center for content
- High-resolution source image for quality scaling

### 5x5 Grid Slicing Implementation
To prevent corner distortion during the window expansion animation, the frame is implemented using a 5x5 grid (25-slice) layout configured in `appsettings.json`:
- **Slice coordinates** define 8 boundary points (Left, LeftInner, RightInner, Right, Top, TopInner, BottomInner, Bottom)
- **FrameSliceService** extracts 16 border pieces from the source image
- **LRU-5 cache** stores sliced frames for different display resolutions
- **Content padding** configurable via Frame.Padding settings

This ensures the decorative corners maintain their detail while the edges stretch smoothly as the window animates from splash size to full-screen.

## Testing

### Expected Behavior
1. **Launch**: Window appears centered at frame-based splash size with frame visible
2. **Splash**: Shows WatchTower logo, spinner, and status messages
3. **Animation**: After ~3-5 seconds, window smoothly expands to full-screen over 500ms
4. **Main Content**: Adaptive card interface appears inside the frame

### Keyboard Shortcuts (Splash Mode)
- `D`: Toggle diagnostics panel
- `ESC` or `✕`: Exit application

### Keyboard Shortcuts (Main Mode)
- `Ctrl+R`: Toggle rich text input overlay
- `Ctrl+M`: Toggle voice input overlay  
- `Ctrl+L`: Toggle event log overlay
- `ESC`: Close any open overlay

## Frame Layout

The frame uses a 9-slice Grid layout for perfect scaling:
```
┌────────────────────────────────────┐
│ ╔══════════════════════════════╗   │
│ ║ Corner │  Top Edge  │ Corner ║   │ ← Fixed 28px
│ ║────────┼────────────┼────────║   │
│ ║        │            │        ║   │
│ ║  Left  │   Content  │ Right  ║   │ ← Stretches
│ ║  Edge  │    Area    │  Edge  ║   │
│ ║        │            │        ║   │
│ ║────────┼────────────┼────────║   │
│ ║ Corner │ Bottom Edge│ Corner ║   │ ← Fixed 28px
│ ╚══════════════════════════════╝   │
└────────────────────────────────────┘
     ↑          ↑          ↑
   28px     Stretches    28px
```

- **Corners** (28x28px): Fixed size, never distort
- **Edges** (1px strips): Tile seamlessly as window resizes
- **Center**: Transparent, holds application content
- **Content margin**: 28px prevents overlap with frame border

## Known Limitations

1. **Cross-Platform**: Animation timing may vary slightly on different platforms due to rendering performance.

2. **Screen Resolution**: The frame-based splash size is clamped to 90% of screen size to prevent oversized windows on small displays.

3. **Frame Asset**: Frame uses 5x5 grid slicing for resolution-independent scaling. The implementation properly handles different DPI settings across monitors.

## Architecture Compliance

✅ **MVVM**: Logic in ViewModels, Views only handle presentation
✅ **DI**: Services injected via ServiceProvider
✅ **Cross-Platform**: Pure Avalonia, no platform-specific code
✅ **Testability**: ViewModels testable without UI dependencies
✅ **Open Source**: Only MIT/Apache 2.0 licensed dependencies

## Future Enhancements

1. **Cross-Fade**: Add opacity animation when transitioning from splash to main content
2. **Frame Variations**: Support different frame styles for themes
3. **9-Slice Implementation**: Create proper 9-slice frame layout if corner distortion is problematic
4. **Responsive Sizing**: Adjust splash size based on screen DPI/resolution
