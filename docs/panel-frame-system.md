# Panel Frame System

## Overview

The Panel Frame System extends the decorative 5x5 grid framing used in the main window to slideout panels (Event Log, Voice Input, Rich Text Input), creating visual consistency throughout the application.

## Architecture

### Components

1. **PanelFrameViewModel** - Manages frame rendering logic, grid dimensions, and edge visibility
2. **PanelFrame UserControl** - Renders the 5x5 grid frame with configurable slide direction
3. **PanelSlideDirection Enum** - Defines slide directions (Left, Bottom, Right, Top)

### How It Works

The panel frame system uses the same 5x5 grid slicing approach as the main window:
- Slices a source image into 16 pieces (corners, edges, and stretch sections)
- Fixed rows/columns: 0, 2, 4 (corners and centers)
- Stretchable rows/columns: 1, 3 (handled by Star sizing)
- Edge visibility controlled by slide direction

### Slide Direction Behavior

- **Left-slide panels**: Hide left edge, show right/top/bottom edges
- **Bottom-slide panels**: Hide bottom edge, show left/right/top edges
- **Right-slide panels**: Hide right edge, show left/top/bottom edges
- **Top-slide panels**: Hide top edge, show left/right/bottom edges

## Configuration

Panel frames are configured in `appsettings.json`:

```json
{
  "PanelFrame": {
    "SourceUri": "avares://WatchTower/Assets/main-frame.png",
    "Scale": 0.15,
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

If `PanelFrame` configuration is not specified, it falls back to the main `Frame` configuration.

## Usage

### In XAML

```xml
<controls:PanelFrame SlideDirection="Left"
                     HorizontalAlignment="Left"
                     VerticalAlignment="Stretch">
    <!-- Panel Content -->
    <Border Background="#EE1A1A1A" Padding="24">
        <!-- Your content here -->
    </Border>
</controls:PanelFrame>
```

### Configuration in Code-Behind

```csharp
// Pass configuration to PanelFrame
panelFrame.SetConfiguration(configuration);

// Update render scale for DPI changes
panelFrame.UpdateRenderScale(renderScale);
```

## Applied Panels

The panel frame system is currently applied to:

1. **Event Log Panel** - Left-slide direction
2. **Rich Text Input Panel** - Bottom-slide direction
3. **Voice Input Panel** - Bottom-slide direction

## Implementation Notes

- PanelFrame inherits from UserControl and uses ContentPresenter for inner content
- Frame loading happens in SetConfiguration() method
- Edge visibility updates automatically when SlideDirection changes
- Grid dimensions recalculate on RenderScale or FrameDisplayScale changes
- MVVM pattern maintained - all logic in ViewModel, View is presentation-only

## Cross-Platform Compatibility

The panel frame system works identically on Windows, macOS, and Linux:
- Uses Avalonia's rendering system for frame display
- DPI-aware scaling via RenderScale property
- No platform-specific code or dependencies

## Performance Considerations

- Frame slices are loaded once and cached
- Edge visibility toggles use bindings (no manual updates needed)
- Transitions handled by Avalonia's animation system
- Hit testing disabled on frame grid so clicks pass through to content

## Future Enhancements

Possible future improvements:
- Support for custom frame images per panel type
- Animation effects for frame appearance
- Different frame styles (rounded, sharp, ornate, etc.)
- Frame opacity controls
- Panel-specific frame scales
