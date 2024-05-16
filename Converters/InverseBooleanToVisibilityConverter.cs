using System;
using System.Globalization;
using System.Windows.Data;

namespace SolarNG.Converters;

public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return new BooleanToVisibilityConverter().Convert((value != null && !(bool)value), targetType, parameter, culture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
