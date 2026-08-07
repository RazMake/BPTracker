using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BPTracker.Desktop;

// Turns the view model's "this number is a crisis" flag into the brush the entry boxes use.
public sealed class CriticalBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush Critical =
        new(Color.FromRgb(0xFF, 0x2D, 0x3E));

    private static readonly SolidColorBrush Normal =
        new(Color.FromRgb(0xE9, 0xEE, 0xF3));

    static CriticalBrushConverter()
    {
        Critical.Freeze();
        Normal.Freeze();
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Critical : Normal;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException("The entry boxes only read this flag.");
}
