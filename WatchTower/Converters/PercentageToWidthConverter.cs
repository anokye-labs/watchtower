using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace WatchTower.Converters;

/// <summary>
/// Converts a percentage (0-100) to a width value by multiplying with a configurable max width.
/// If no parameter is provided, defaults to 300.
/// </summary>
public class PercentageToWidthConverter : IValueConverter
{
    public static readonly PercentageToWidthConverter Instance = new();

    private const double DefaultMaxWidth = 300.0;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue)
        {
            // Use parameter if provided, otherwise use default
            double maxWidth = DefaultMaxWidth;
            if (parameter is string paramStr && double.TryParse(paramStr, out var customWidth))
            {
                maxWidth = customWidth;
            }
            
            // Clamp the percentage to the valid range [0, 100] to avoid negative or oversized widths
            int clampedPercentage = Math.Clamp(intValue, 0, 100);
            
            return (clampedPercentage / 100.0) * maxWidth;
        }

        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
