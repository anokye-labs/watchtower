# Animated Splash Frame Expansion Implementation

## Overview

This implementation replaces the separate SplashWindow and MainWindow with a unified ShellWindow that:
1. Opens at 70% of screen size with a decorative frame
2. Displays splash content during initialization
3. Animates smoothly to full-screen over 800ms
4. Transitions to main application content

## Key Components

### ShellWindow (`Views/ShellWindow.axaml`)
- Root window with decorative frame background (`main-frame-complete-2.svg`)
- ContentControl that switches between splash and main content
- Frame stretches with window during animation
- 100px margin for content area inside frame

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
The implementation uses a placeholder SVG frame (`Assets/main-frame-complete-2.svg`) that provides:
- Golden/bronze ornate corners
- 60px border thickness
- 100px inner margin for content
- Decorative pattern overlay

### Replacing with Actual Asset
To use the actual `main-frame-complete-2.jpg` from the issue:
1. Download the image from the issue attachments
2. Replace `WatchTower/Assets/main-frame-complete-2.svg` with the .jpg file
3. Update `ShellWindow.axaml` line 20 to use `.jpg` instead of `.svg`:
   ```xml
   <Image Source="avares://WatchTower/Assets/main-frame-complete-2.jpg"
   ```
4. Adjust the `Margin="100"` on line 24 to match the actual frame's inner border size

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

The frame layout uses a simple Grid structure:
```
┌─────────────────────────────────┐
│ Frame Background (Image)        │ ← Stretches with window
│  ┌───────────────────────────┐  │
│  │ Content Area (100px margin)│  │
│  │                           │  │
│  │  Splash or Main Content   │  │
│  │                           │  │
│  └───────────────────────────┘  │
└─────────────────────────────────┘
```

The frame image stretches uniformly, so corners remain proportional during animation.

## Known Limitations

1. **9-Slice Stretching**: Avalonia 11.3.9 doesn't have built-in 9-slice (NineGrid) support for images. The current implementation uses `Stretch="Fill"` which may distort corners slightly. If corner distortion is unacceptable, the frame could be split into 9 separate images arranged in a Grid.

2. **Cross-Platform**: Animation timing may vary slightly on different platforms due to rendering performance. The implementation targets 60 FPS but will adapt based on system capabilities.

3. **Screen Resolution**: The 70% splash size works well on most displays. On very small screens (<1024px), the splash may appear cramped. Consider adding minimum size constraints if needed.

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
