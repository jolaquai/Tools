using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace DbdOverlay.Converters;

public class HasContentToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        switch (value)
        {
            case Panel fe when fe.Children is { Count: > 0 }:
            case string s when !string.IsNullOrWhiteSpace(s):
            case not null:
                return Visibility.Visible;
            default:
                return Visibility.Collapsed;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
