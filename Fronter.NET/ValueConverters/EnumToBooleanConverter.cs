using Avalonia;
using Avalonia.Data.Converters;
using commonItems;
using System;
using System.Globalization;

namespace Fronter.ValueConverters;

public class EnumToBooleanConverter : IValueConverter
{
	public static EnumToBooleanConverter Instance = new EnumToBooleanConverter();

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
		return value?.Equals(parameter) ?? AvaloniaProperty.UnsetValue;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
		if (value != null) {
			return (bool)value ? parameter : AvaloniaProperty.UnsetValue;
		}
		return AvaloniaProperty.UnsetValue;
	}
}