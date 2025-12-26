using Avalonia;
using Avalonia.Controls;

namespace WatchTower.Views;

/// <summary>
/// Custom Window base class with animatable proxy properties for window position and size.
/// Provides StyledProperty definitions that integrate with Avalonia's animation system,
/// syncing changes to the platform Window properties.
/// </summary>
public class AnimatableWindow : Window
{
    /// <summary>
    /// Defines the AnimatedX property (window X position in physical pixels).
    /// </summary>
    public static readonly StyledProperty<double> AnimatedXProperty =
        AvaloniaProperty.Register<AnimatableWindow, double>(
            nameof(AnimatedX),
            defaultValue: 0);

    /// <summary>
    /// Gets or sets the animated X position. Changes are synced to Window.Position.
    /// </summary>
    public double AnimatedX
    {
        get => GetValue(AnimatedXProperty);
        set => SetValue(AnimatedXProperty, value);
    }

    /// <summary>
    /// Defines the AnimatedY property (window Y position in physical pixels).
    /// </summary>
    public static readonly StyledProperty<double> AnimatedYProperty =
        AvaloniaProperty.Register<AnimatableWindow, double>(
            nameof(AnimatedY),
            defaultValue: 0);

    /// <summary>
    /// Gets or sets the animated Y position. Changes are synced to Window.Position.
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
    /// Gets or sets the animated width. Changes are synced to Window.Width.
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
    /// Gets or sets the animated height. Changes are synced to Window.Height.
    /// </summary>
    public double AnimatedHeight
    {
        get => GetValue(AnimatedHeightProperty);
        set => SetValue(AnimatedHeightProperty, value);
    }

    /// <summary>
    /// Syncs animated property values to platform window properties.
    /// Called automatically when animated properties change.
    /// </summary>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == AnimatedXProperty || change.Property == AnimatedYProperty)
        {
            // Sync AnimatedX/AnimatedY to Window.Position
            var x = (int)AnimatedX;
            var y = (int)AnimatedY;
            System.Diagnostics.Debug.WriteLine($"AnimatableWindow: AnimatedX/Y changed to {x},{y} (from {change.OldValue})");
            Position = new PixelPoint(x, y);
        }
        else if (change.Property == AnimatedWidthProperty)
        {
            // Sync AnimatedWidth to Window.Width
            System.Diagnostics.Debug.WriteLine($"AnimatableWindow: AnimatedWidth changed to {AnimatedWidth:F0} (from {change.OldValue})");
            Width = AnimatedWidth;
        }
        else if (change.Property == AnimatedHeightProperty)
        {
            // Sync AnimatedHeight to Window.Height
            System.Diagnostics.Debug.WriteLine($"AnimatableWindow: AnimatedHeight changed to {AnimatedHeight:F0} (from {change.OldValue})");
            Height = AnimatedHeight;
        }
    }

    /// <summary>
    /// Initializes animated properties from current window state.
    /// Call this before starting animations to ensure correct starting values.
    /// </summary>
    public void SyncFromWindowState()
    {
        AnimatedX = Position.X;
        AnimatedY = Position.Y;
        AnimatedWidth = Width;
        AnimatedHeight = Height;
    }
}
