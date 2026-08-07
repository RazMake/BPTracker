using System.Globalization;
using System.Windows.Data;
using BPTracker.Domain.Readings;
using BPTracker.Presentation.Readings;

namespace BPTracker.Desktop;

// The history grid binds straight to the reading, so the enum is turned into words here rather
// than by adding a display string to the domain.
public sealed class CategoryNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is BloodPressureCategory category
            ? BloodPressureCategoryName.For(category)
            : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException("The history grid is read only.");
}
