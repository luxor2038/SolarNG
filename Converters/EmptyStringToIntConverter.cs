using System;
using System.Globalization;
using System.Windows.Data;

namespace SolarNG.Converters;

public class EmptyStringToIntConverter : IValueConverter
{
    public int EmptyStringValue { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return null;
        }
        if (value is string)
        {
            return value;
        }
        if (value is int v && v == EmptyStringValue)
        {
            return string.Empty;
        }
        return value.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string)
        {
            return value;
        }
        if (int.TryParse((string)value, out var result))
        {
            return result;
        }
        return EmptyStringValue;
    }
}
