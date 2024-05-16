using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SolarNG.Converters;

internal class InverseGuidToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (value is Guid guid && object.Equals(guid, Guid.Empty)) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
