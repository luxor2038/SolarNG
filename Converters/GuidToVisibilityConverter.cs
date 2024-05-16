using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SolarNG.Converters;

public class GuidToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return true;
        }
        if ((Guid)value == Guid.Empty)
        {
            return Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
