# Fluent Icons Usage Guide

## Overview

WatchTower integrates the `FluentIcons.Avalonia.Fluent` package to provide access to Microsoft's Fluent UI System Icons with Segoe Fluent Icons metrics. This provides a modern, consistent, and scalable icon solution across all platforms (Windows, macOS, and Linux).

## Package Information

- **Package**: [FluentIcons.Avalonia.Fluent](https://www.nuget.org/packages/FluentIcons.Avalonia.Fluent/)
- **Version**: 2.0.316.1
- **License**: MIT

## Integration

The FluentIcons package has been integrated into WatchTower and is ready to use without additional configuration.

### Setup Steps Completed

1. ✅ NuGet package added to `WatchTower.csproj`
2. ✅ Namespace declarations added to `App.axaml` for global availability
3. ✅ Cross-platform support verified

## Usage in XAML

### Basic Usage

To use Fluent icons in your XAML views, first add the namespace declaration to your Window or UserControl:

```xml
<Window xmlns:fluent="using:FluentIcons.Avalonia.Fluent"
        ...>
```

Then use the `SymbolIcon` control:

```xml
<fluent:SymbolIcon Symbol="Settings" FontSize="24" />
```

### With Customization

You can customize the appearance of icons using standard Avalonia properties:

```xml
<!-- Custom size and color -->
<fluent:SymbolIcon Symbol="Calendar" 
                   FontSize="32" 
                   Foreground="Blue" />

<!-- With margins and alignment -->
<fluent:SymbolIcon Symbol="ChevronRight" 
                   FontSize="16" 
                   Margin="5,0"
                   VerticalAlignment="Center" />
```

### In Layouts

Icons work seamlessly in any Avalonia layout:

```xml
<StackPanel Orientation="Horizontal" Spacing="10">
    <fluent:SymbolIcon Symbol="Settings" FontSize="20" />
    <TextBlock Text="Settings" VerticalAlignment="Center" />
</StackPanel>
```

## Available Symbols

The FluentIcons package includes hundreds of symbols from the Fluent UI System Icons collection. Some commonly used symbols include:

### Navigation
- `ArrowLeft`, `ArrowRight`, `ArrowUp`, `ArrowDown`
- `ChevronLeft`, `ChevronRight`, `ChevronUp`, `ChevronDown`
- `Home`, `Back`, `Forward`

### Actions
- `Add`, `Delete`, `Edit`, `Save`
- `Copy`, `Cut`, `Paste`
- `Search`, `Filter`, `Sort`

### UI Elements
- `Settings`, `Options`, `MoreVertical`, `MoreHorizontal`
- `Close`, `Minimize`, `Maximize`
- `Calendar`, `Clock`, `Star`

### Media & Files
- `Play`, `Pause`, `Stop`, `Previous`, `Next`
- `Folder`, `Document`, `Image`, `Video`

### Communication
- `Mail`, `People`, `Phone`, `Chat`

### Complete Icon List

For a complete list of available icons and their names, refer to:
- [Fluent UI System Icons Repository](https://github.com/microsoft/fluentui-system-icons)
- [FluentIcons.Avalonia Documentation](https://github.com/davidxuang/FluentIcons)

## Example Implementation

See `WatchTower/Views/MainWindow.axaml` for a working example of FluentIcons usage in the application.

## Benefits

- ✅ **Consistency**: Microsoft's official Fluent design icons across all platforms
- ✅ **Scalability**: Vector-based icons scale perfectly at any resolution
- ✅ **Performance**: Lightweight and efficient rendering
- ✅ **Maintainability**: No need to manage custom SVG or image assets
- ✅ **Cross-platform**: Identical appearance on Windows, macOS, and Linux
- ✅ **Segoe Metrics**: Proper alignment with Segoe Fluent Icons specifications

## Best Practices

1. **Consistent Sizing**: Use standardized icon sizes (16, 20, 24, 32, 48) for UI consistency
2. **Accessibility**: Ensure adequate color contrast when customizing Foreground colors
3. **Spacing**: Use appropriate margins/padding around icons in layouts
4. **Semantic Naming**: Choose icon symbols that clearly represent their function
5. **Theme Support**: Icons automatically adapt to the application's theme (Dark/Light)

## Migration from Other Icon Solutions

If migrating from other icon solutions:

1. Replace custom SVG/PNG assets with SymbolIcon where applicable
2. Update icon references from image paths to symbol names
3. Remove embedded icon resources that are now covered by FluentIcons
4. Test visual appearance across all target platforms

## Troubleshooting

### Icon Not Displaying

- Verify the symbol name is correct (case-sensitive)
- Ensure the `fluent` namespace is declared in your XAML
- Check that FontSize is set to a visible value

### Build Errors

- Ensure `FluentIcons.Avalonia.Fluent` package is properly restored
- Clean and rebuild the solution if needed
- Verify namespace declarations match the package version

## Additional Resources

- [Avalonia UI Documentation](https://docs.avaloniaui.net/)
- [FluentAvalonia Controls](https://github.com/amwx/FluentAvalonia)
- [Microsoft Fluent Design System](https://www.microsoft.com/design/fluent/)
