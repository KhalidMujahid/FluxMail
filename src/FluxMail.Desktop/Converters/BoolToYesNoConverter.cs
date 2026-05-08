using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FluxMail.Desktop.Converters;

public class BoolToYesNoConverter : IValueConverter
{
    public static readonly BoolToYesNoConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "Yes" : "No";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is "Yes";
}
