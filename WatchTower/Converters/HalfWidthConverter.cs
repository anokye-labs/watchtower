using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace WatchTower.Converters;

/// <summary>
/// Converts a width value to half of its value.
/// Used to make the Event Log panel half the window width.
/// </summary>
public class HalfWidthConverter : IValueConverter
{
    public static readonly HalfWidthConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double width)
        {
            return width / 2.0;
        }
        return 400.0; // Fallback default
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
