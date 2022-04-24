using Avalonia;
using Avalonia.Data.Converters;
using Fronter.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Fronter.ValueConverters; 

public class LocKeyToValueConverter : IMultiValueConverter {
	public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) {
		return values[0] is not string locKey ? AvaloniaProperty.UnsetValue : TranslationSource.Instance[locKey];
	}
}