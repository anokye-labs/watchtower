# Animated Splash Frame Expansion - Quick Start

## What Was Implemented

A unified shell window that replaces the separate splash and main windows with a smooth animated transition:

1. **Single Window Lifecycle**: One `ShellWindow` from launch to runtime
2. **Decorative Frame**: Ornate border that stretches with the window
3. **Smooth Animation**: 800ms cubic ease-out expansion from 70% to full-screen
4. **Seamless Transition**: Splash content → animation → main content

## Files Changed

### New Files
- `WatchTower/Views/ShellWindow.axaml` - Unified window layout
- `WatchTower/Views/ShellWindow.axaml.cs` - Animation and event handling
- `WatchTower/ViewModels/ShellWindowViewModel.cs` - State management
- `WatchTower/Assets/main-frame.png` - Decorative frame image

### Modified Files
- `WatchTower/App.axaml.cs` - Updated startup flow
- `WatchTower/WatchTower.csproj` - Added Assets as resources

### Documentation
- `IMPLEMENTATION-ANIMATED-SHELL.md` - Technical details
- `ANIMATION-FLOW.md` - Flow diagrams
- `VISUAL-MOCKUP.md` - Visual sequence mockup

## Quick Test

```bash
# Build and run
cd /path/to/watchtower
dotnet run --project WatchTower/WatchTower.csproj
```

**Expected behavior:**
1. Window opens centered (70% screen size)
2. Splash shows loading (~3-5 seconds)
3. Window smoothly expands to full-screen
4. Main content appears inside frame

## Customization

All in `ShellWindow.axaml.cs`:

```csharp
private const int AnimationDurationMs = 800;        // Animation speed
private const double SplashSizeRatio = 0.7;         // Initial size (70%)
```

## Frame Configuration

The frame image (`WatchTower/Assets/main-frame.png`) is configured via `appsettings.json`:

```json
{
  "Frame": {
    "SourceUri": "avares://WatchTower/Assets/main-frame.png",
    "Scale": 0.25,
    "BackgroundColor": "#261208",
    "Padding": { "Left": 80, "Top": 60, "Right": 80, "Bottom": 60 },
    "Slice": { ... }
  }
}
```

The frame uses a 5x5 grid slicing system for resolution-independent scaling.

## Architecture

✅ **MVVM**: Logic in ViewModels only
✅ **DI**: Services injected via ServiceProvider  
✅ **Cross-Platform**: Pure Avalonia
✅ **No Breaking Changes**: Original windows kept for reference

## Next Steps

1. **User Testing**: Run and validate animation behavior
2. **Frame Asset**: Replace placeholder with actual image
3. **Tuning**: Adjust animation parameters if desired
4. **Cleanup**: Remove old `SplashWindow.axaml*` and `MainWindow.axaml*` if satisfied

## Support

See detailed documentation:
- **IMPLEMENTATION-ANIMATED-SHELL.md** for implementation details
- **ANIMATION-FLOW.md** for flow diagrams
- **VISUAL-MOCKUP.md** for visual mockups

## Status

✅ Builds without errors/warnings  
✅ Code review completed  
✅ Ready for user testing  
⏳ Awaiting validation on actual hardware
