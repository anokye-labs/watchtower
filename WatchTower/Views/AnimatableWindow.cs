using Avalonia;
using Avalonia.Controls;

namespace WatchTower.Views;

/// <summary>
/// Custom Window base class with animatable proxy properties for window position and size.
/// Provides StyledProperty definitions that integrate with Avalonia's animation system,
/// syncing changes to the platform Window properties.
/// 
/// All animated properties use logical coordinates/pixels consistently:
/// - AnimatedX/Y: logical coordinates, converted to physical pixels at the boundary
/// - AnimatedWidth/Height: logical pixels, synced directly to Window.Width/Height
/// </summary>
public class AnimatableWindow : Window
{
    /// <summary>
    /// Defines the AnimatedX property (window X position in logical coordinates).
    /// Converted to physical pixels when synced to Window.Position.
    /// </summary>
    public static readonly StyledProperty<double> AnimatedXProperty =
        AvaloniaProperty.Register<AnimatableWindow, double>(
            nameof(AnimatedX),
            defaultValue: 0);

    /// <summary>
    /// Gets or sets the animated X position in logical coordinates.
    /// </summary>
    public double AnimatedX
    {
        get => GetValue(AnimatedXProperty);
        set => SetValue(AnimatedXProperty, value);
    }

    /// <summary>
    /// Defines the AnimatedY property (window Y position in logical coordinates).
    /// Converted to physical pixels when synced to Window.Position.
    /// </summary>
    public static readonly StyledProperty<double> AnimatedYProperty =
        AvaloniaProperty.Register<AnimatableWindow, double>(
            nameof(AnimatedY),
            defaultValue: 0);

    /// <summary>
    /// Gets or sets the animated Y position in logical coordinates.
    /// </summary>
    public double AnimatedY
    {
        get => GetValue(AnimatedYProperty);
        set => SetValue(AnimatedYProperty, value);
    }

    /// <summary>
    /// Defines the AnimatedWidth property (window width in logical pixels).
    /// </summary>
    public static readonly StyledProperty<double> AnimatedWidthProperty =
        AvaloniaProperty.Register<AnimatableWindow, double>(
            nameof(AnimatedWidth),
            defaultValue: 800);

    /// <summary>
    /// Gets or sets the animated width in logical pixels.
    /// </summary>
    public double AnimatedWidth
    {
        get => GetValue(AnimatedWidthProperty);
        set => SetValue(AnimatedWidthProperty, value);
    }

    /// <summary>
    /// Defines the AnimatedHeight property (window height in logical pixels).
    /// </summary>
    public static readonly StyledProperty<double> AnimatedHeightProperty =
        AvaloniaProperty.Register<AnimatableWindow, double>(
            nameof(AnimatedHeight),
            defaultValue: 600);

    /// <summary>
    /// Gets or sets the animated height in logical pixels.
    /// </summary>
    public double AnimatedHeight
    {
        get => GetValue(AnimatedHeightProperty);
        set => SetValue(AnimatedHeightProperty, value);
    }

    /// <summary>
    /// Syncs animated property values to platform window properties.
    /// Called automatically when animated properties change.
    /// Converts logical coordinates to physical pixels for Window.Position.
    /// </summary>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == AnimatedXProperty || change.Property == AnimatedYProperty)
        {
            // Convert logical coordinates to physical pixels for Window.Position
            var scaling = GetCurrentScaling();
            var physicalX = (int)(AnimatedX * scaling);
            var physicalY = (int)(AnimatedY * scaling);
            Position = new PixelPoint(physicalX, physicalY);
        }
        else if (change.Property == AnimatedWidthProperty)
        {
            Width = AnimatedWidth;
        }
        else if (change.Property == AnimatedHeightProperty)
        {
            Height = AnimatedHeight;
        }
    }

    /// <summary>
    /// Initializes animated properties from current window state.
    /// Call this before starting animations to ensure correct starting values.
    /// Converts physical pixels to logical coordinates for AnimatedX/Y.
    /// </summary>
    public void SyncFromWindowState()
    {
        var scaling = GetCurrentScaling();
        AnimatedX = Position.X / scaling;
        AnimatedY = Position.Y / scaling;
        AnimatedWidth = Width;
        AnimatedHeight = Height;
    }

    /// <summary>
    /// Gets the current DPI scaling factor for this window.
    /// </summary>
    private double GetCurrentScaling()
    {
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        return screen?.Scaling ?? 1.0;
    }
}
