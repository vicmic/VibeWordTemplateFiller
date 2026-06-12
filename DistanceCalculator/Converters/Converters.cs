using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using DistanceCalculator.Models;

namespace DistanceCalculator.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool b = value is bool bval && bval;
        bool invert = parameter is string s && s == "invert";
        if (invert) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ProcessingStatus status)
        {
            return status switch
            {
                ProcessingStatus.Success => new SolidColorBrush(Color.FromRgb(0x43, 0xA0, 0x47)),
                ProcessingStatus.Error => new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35)),
                ProcessingStatus.Processing => new SolidColorBrush(Color.FromRgb(0x19, 0x76, 0xD2)),
                ProcessingStatus.Skipped => new SolidColorBrush(Color.FromRgb(0x75, 0x75, 0x75)),
                _ => new SolidColorBrush(Color.FromRgb(0xBD, 0xBD, 0xBD))
            };
        }
        return Brushes.Transparent;
    }
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class StatusToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ProcessingStatus status)
        {
            return status switch
            {
                ProcessingStatus.Success => MaterialDesignThemes.Wpf.PackIconKind.CheckCircle,
                ProcessingStatus.Error => MaterialDesignThemes.Wpf.PackIconKind.AlertCircle,
                ProcessingStatus.Processing => MaterialDesignThemes.Wpf.PackIconKind.ProgressClock,
                ProcessingStatus.Skipped => MaterialDesignThemes.Wpf.PackIconKind.MinusCircle,
                _ => MaterialDesignThemes.Wpf.PackIconKind.CircleOutline
            };
        }
        return MaterialDesignThemes.Wpf.PackIconKind.CircleOutline;
    }
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool hasValue = value is string s ? !string.IsNullOrWhiteSpace(s) : value != null;
        bool invert = parameter is string p && p == "invert";
        if (invert) hasValue = !hasValue;
        return hasValue ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class ProgressToWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 && values[0] is double progress && values[1] is double totalWidth)
            return totalWidth * progress / 100.0;
        return 0.0;
    }
    public object[] ConvertBack(object value, Type[] t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class ApiKeyValidToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && b
            ? new SolidColorBrush(Color.FromRgb(0x43, 0xA0, 0x47))
            : new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35));
    }
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

public class DoubleFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
            return d.ToString("N2", new CultureInfo("it-IT"));
        return "-";
    }
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}
