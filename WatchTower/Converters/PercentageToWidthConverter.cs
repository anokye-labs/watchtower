using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace WatchTower.Converters;

/// <summary>
/// Converts a percentage (0-100) to a width value by multiplying with a fixed width (300).
/// </summary>
public class PercentageToWidthConverter : IValueConverter
{
    public static readonly PercentageToWidthConverter Instance = new();

    private const double MaxWidth = 300.0;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue)
        {
            return (intValue / 100.0) * MaxWidth;
        }

        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
