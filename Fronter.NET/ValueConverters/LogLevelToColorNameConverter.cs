using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using commonItems;
using System;

namespace Fronter.ValueConverters; 

// based on https://stackoverflow.com/a/5551986/10249243
public class LogLevelToColorNameConverter : IValueConverter {
	public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) {
		var input = (LogLevel?)value;
		return input switch {
			LogLevel.Progress => Brushes.ForestGreen,
			LogLevel.Notice => Brushes.CornflowerBlue,
			LogLevel.Error => Brushes.IndianRed,
			LogLevel.Warn => Brushes.Orange,
			LogLevel.Info => Brushes.Transparent,
			LogLevel.Debug => Brushes.SlateGray,
			_ => Brushes.Transparent
		};
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) {
		throw new NotSupportedException();
	}
}