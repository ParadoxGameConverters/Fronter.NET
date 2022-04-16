using Avalonia;
using Avalonia.Data.Converters;
using Fronter.Extensions;
using System;
using System.Globalization;

namespace Fronter.ValueConverters; 

public class LocKeyToValueConverter : IValueConverter {
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
		var locKey = (string?)value;
		return locKey is null ? AvaloniaProperty.UnsetValue : TranslationSource.Instance[locKey];
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
		throw new NotImplementedException();
	}
}