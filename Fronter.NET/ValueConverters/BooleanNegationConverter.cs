using Avalonia;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Fronter.ValueConverters;

internal sealed class BooleanNegationConverter : IValueConverter {
    public static readonly BooleanNegationConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is bool b) {
            return !b;
        }
        return AvaloniaProperty.UnsetValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is bool b) {
            return !b;
        }
        return AvaloniaProperty.UnsetValue;
    }
}