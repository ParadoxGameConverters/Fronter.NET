using Avalonia.Data.Converters;
using Avalonia.Media;
using log4net.Core;
using System;

namespace Fronter.ValueConverters;

// based on https://stackoverflow.com/a/5551986/10249243
internal sealed class LogLevelToColorNameConverter : IValueConverter {
	public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) {
		if (value is not Level input) {
			return Brushes.Transparent;
		}
		// Tried to order this by expected frequency for better performance.
		return input.Name switch {
			"DEBUG" => Brushes.SlateGray,
			"INFO" => Brushes.Transparent,
			"PROGRESS" => Brushes.ForestGreen,
			"WARN" => Brushes.Orange,
			"NOTICE" => Brushes.CornflowerBlue,
			"ERROR" => Brushes.IndianRed,
			"FATAL" => Brushes.DarkRed,
			"WARNING" => Brushes.Orange,
			"CRITICAL" => Brushes.Red,
			_ => Brushes.Transparent,
		};
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) {
		throw new NotSupportedException();
	}
}