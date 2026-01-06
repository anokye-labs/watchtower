# WatchTower Assets Documentation

This document describes the visual assets used in the WatchTower application and their usage.

## Asset Inventory

### Logo and Branding

#### `logo.png`
- **Source**: `concept-art/V1/Watch-Tower_Logo__Iconic_videogame_title_logo_with_eye-tower_symbol_and_Ashanti-inspired_geometry.png`
- **Usage**: Displayed in splash screen during application startup
- **Dimensions**: 200x80px (display size), original is ~6.1MB high-resolution
- **Location**: `WatchTower/Assets/logo.png`
- **XAML Reference**: `avares://WatchTower/Assets/logo.png`

#### `logo.svg`
- **Source**: `concept-art/Anokye.svg`
- **Usage**: Vector version of application logo (for future scalable use)
- **Location**: `WatchTower/Assets/logo.svg`
- **XAML Reference**: `avares://WatchTower/Assets/logo.svg`

### Adinkra Symbols

Adinkra symbols are West African visual symbols that represent concepts, proverbs, and values. WatchTower uses these symbols to reinforce the "Ancestral Futurism" design language.

#### `gye-nyame.png`
- **Symbol Name**: Gye Nyame (Except God)
- **Meaning**: "Except God" - Supremacy and immortality of God, symbol of divine protection
- **Usage**: Animated symbol on splash screen during startup, represents security/system health
- **Source**: `concept-art/Adinkra Symbols/Adinkra Symbols/Gye-Nyame.png`
- **Dimensions**: 50x50px (display size)
- **Animation**: Pulsing opacity (0.6 to 1.0) over 2 seconds with cubic ease-in-out
- **Location**: `WatchTower/Assets/gye-nyame.png`
- **XAML Reference**: `avares://WatchTower/Assets/gye-nyame.png`

#### `sankofa.png`
- **Symbol Name**: Sankofa (Go back and get it)
- **Meaning**: "It is not wrong to go back for that which you have forgotten" - Learning from the past
- **Usage**: Reserved for history/logs/undo features (future implementation)
- **Source**: `concept-art/Adinkra Symbols/Adinkra Symbols/Sankofa.png`
- **Location**: `WatchTower/Assets/sankofa.png`
- **XAML Reference**: `avares://WatchTower/Assets/sankofa.png`

### Frame Assets

#### `main-frame.png`
- **Usage**: 5x5 grid-sliced decorative frame for shell window
- **Source**: Original WatchTower frame design
- **Size**: ~5.7MB
- **Location**: `WatchTower/Assets/main-frame.png`
- **XAML Reference**: `avares://WatchTower/Assets/main-frame.png`

## Asset Usage Guidelines

### Adding New Assets

1. Place assets in `WatchTower/Assets/` directory
2. Assets are automatically included via `<AvaloniaResource Include="Assets\**" />` in the .csproj
3. Reference using the `avares://WatchTower/Assets/[filename]` URI scheme

### Naming Conventions

- Use lowercase with hyphens for multi-word names: `gye-nyame.png`, not `GyeNyame.png`
- Use descriptive names that indicate purpose: `logo.png`, not `image1.png`
- For Adinkra symbols, use the traditional name

### File Formats

- **PNG**: Preferred for raster graphics (logos, symbols, icons)
- **SVG**: Supported for scalable vector graphics (future use)
- **Avoid**: JPEG (lossy compression not suitable for UI elements)

### Cross-Platform Considerations

- Use forward slashes `/` in asset paths, not backslashes
- Test asset rendering on all platforms (Windows, macOS, Linux)
- Ensure assets scale correctly with different DPI settings
- Avoid platform-specific image formats

### Design System Alignment

All visual assets should align with the "Ancestral Futurism" design language:

- **Colors**: Ashanti Gold (#FFD700), Holographic Cyan (#00F0FF), Deep Mahogany (#4A1812), Void Black (#050508)
- **Style**: Blend of modern/futuristic aesthetics with West African cultural elements
- **Symbolism**: Use Adinkra symbols to convey meaning and reinforce cultural identity

## Future Asset Additions

Planned assets for future implementation:

- Additional Adinkra symbols:
  - **Dwennimmen** (Ram's Horns) - Strength/Conflict, for warrior deployment views
  - **Bi Nka Bi** (No one bites another) - Harmony/Consensus, for agent agreement indicators
  - **Dame-Dame** (Checkers) - Strategy/Intelligence, for map/grid views
- Application icon (`.ico`, `.icns` for platform-specific app icons)
- Splash screen background variations
- Loading animation sequences
- Custom cursor assets (for themed experience)

## License and Attribution

All Adinkra symbols are traditional West African (specifically Akan/Ashanti) cultural symbols that are in the public domain. The specific renderings used in this project should be attributed appropriately if derived from external sources.

Custom artwork and logos created specifically for WatchTower are part of the project's assets.

---

Last Updated: 2026-01-01
