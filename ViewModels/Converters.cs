using Organizer.Models;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Organizer.Converters;

public class LogTypeToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is LogType type)
        {
            return type switch
            {
                LogType.Success => new SolidColorBrush(Color.FromRgb(74, 222, 128)),   // verde
                LogType.Warning => new SolidColorBrush(Color.FromRgb(251, 191, 36)),   // amarillo
                LogType.Error => new SolidColorBrush(Color.FromRgb(248, 113, 113)),  // rojo
                _ => new SolidColorBrush(Color.FromRgb(148, 163, 184)),  // gris
            };
        }
        return Brushes.White;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}