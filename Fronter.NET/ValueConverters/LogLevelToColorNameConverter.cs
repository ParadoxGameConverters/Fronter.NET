using Avalonia.Data.Converters;
using Avalonia.Media;
using log4net.Core;
using System;

namespace Fronter.ValueConverters;

// based on https://stackoverflow.com/a/5551986/10249243
public class LogLevelToColorNameConverter : IValueConverter {
	public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) {
		var input = value as Level;
		return input?.Name switch {
			"PROGRESS" => Brushes.ForestGreen,
			"NOTICE" => Brushes.CornflowerBlue,
			"FATAL" => Brushes.DarkRed,
			"CRITICAL" => Brushes.Red,
			"ERROR" => Brushes.IndianRed,
			"WARN" => Brushes.Orange,
			"WARNING" => Brushes.Orange,
			"INFO" => Brushes.Transparent,
			"DEBUG" => Brushes.SlateGray,
			_ => Brushes.Transparent
		};
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) {
		throw new NotSupportedException();
	}
}