# Animated Splash Frame Expansion Implementation

## Overview

This implementation replaces the separate SplashWindow and MainWindow with a unified ShellWindow that:
1. Opens at 70% of screen size with a decorative frame
2. Displays splash content during initialization
3. Animates smoothly to full-screen over 800ms
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
- **SetSplashSize()**: Calculates initial window size (70% of screen, centered)
- **AnimateExpansionAsync()**: Smoothly interpolates width, height, and position over 800ms
- Uses cubic ease-out easing for natural motion
- ~60 FPS animation via DispatcherTimer
- Maximizes window after animation completes

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

This ensures the decorative corners maintain their detail while the edges stretch smoothly as the window animates from 70% to full-screen size.

## Testing

### Expected Behavior
1. **Launch**: Window appears centered at ~70% screen size with frame visible
2. **Splash**: Shows WatchTower logo, spinner, and status messages
3. **Animation**: After ~3-5 seconds, window smoothly expands to full-screen over 0.8 seconds
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

1. **Cross-Platform**: Animation timing may vary slightly on different platforms due to rendering performance. The implementation targets 60 FPS but will adapt based on system capabilities.

2. **Screen Resolution**: The 70% splash size works well on most displays. On very small screens (<1024px), the splash may appear cramped. Consider adding minimum size constraints if needed.

3. **Frame Asset**: Frame created at 1920x1080 with 28px borders. The 9-slice implementation ensures it scales properly to any resolution without corner distortion.

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
