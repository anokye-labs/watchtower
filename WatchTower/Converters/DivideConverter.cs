using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace WatchTower.Converters;

/// <summary>
/// Converts a value by dividing it by a parameter (for scaling calculations)
/// </summary>
public class DivideConverter : IValueConverter
{
    public static readonly DivideConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double doubleValue && parameter is string paramStr && double.TryParse(paramStr, out var divisor) && Math.Abs(divisor) > double.Epsilon)
        {
            return doubleValue / divisor;
        }

        return 1.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
