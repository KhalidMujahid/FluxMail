using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace FluxMail.Desktop.Converters;

public class BoolToBrushConverter : IValueConverter
{
    public static readonly BoolToBrushConverter Instance = new();

    public IBrush TrueBrush { get; set; } = new SolidColorBrush(Color.Parse("#A6E3A1"));
    public IBrush FalseBrush { get; set; } = new SolidColorBrush(Color.Parse("#F38BA8"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? TrueBrush : FalseBrush;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
