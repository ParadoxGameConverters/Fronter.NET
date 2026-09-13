using Avalonia;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Fronter.ValueConverters;

internal sealed class EnumToBooleanConverter : IValueConverter {
	public static readonly EnumToBooleanConverter Instance = new();

	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
		return value?.Equals(parameter) ?? AvaloniaProperty.UnsetValue;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
		if (value is not null) {
			return (bool)value ? parameter : AvaloniaProperty.UnsetValue;
		}
		return AvaloniaProperty.UnsetValue;
	}
}