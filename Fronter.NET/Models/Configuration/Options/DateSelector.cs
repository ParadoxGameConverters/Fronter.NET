using Avalonia.Data;
using commonItems;
using ReactiveUI;
using System;
using System.Globalization;

namespace Fronter.Models.Configuration.Options;

internal sealed class DateSelector : ReactiveObject {
	public DateSelector(BufferedReader reader) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("editable", reader => Editable = string.Equals(reader.GetString(), "true", StringComparison.OrdinalIgnoreCase));
		parser.RegisterKeyword("value", reader => {
			var valueStr = reader.GetString();
			if (string.IsNullOrWhiteSpace(valueStr)) {
				Value = null;
			} else {
				Value = new Date(valueStr);
			}
		});
		parser.RegisterKeyword("minDate", reader => MinDate = new Date(reader.GetString()).ToDateTimeOffset());
		parser.RegisterKeyword("maxDate", reader => MaxDate = new Date(reader.GetString()).ToDateTimeOffset());
		parser.RegisterKeyword("tooltip", reader => Tooltip = reader.GetString());
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}

	public bool Editable { get; private set; } = true; // editable unless disabled
	public DateTimeOffset MinDate { get; set; } = DateTimeOffset.MinValue;
	public DateTimeOffset MaxDate { get; set; } = DateTimeOffset.MaxValue;

	public DateTimeOffset? DateTimeOffsetValue {
		get;
		set => this.RaiseAndSetIfChanged(ref field, value);
	}

	public Date? Value {
		get {
			if (DateTimeOffsetValue is null) {
				return null;
			}

			var offsetValue = DateTimeOffsetValue.Value;
			return new Date(offsetValue);
		}
		set {
			if (value is null) {
				DateTimeOffsetValue = null;
			} else {
				DateTimeOffsetValue = value.Value.ToDateTimeOffset();
			}
		}
	}

	public string? TextValue {
		get => Value?.ToString();
		set {
			if (string.IsNullOrWhiteSpace(value)) {
				DateTimeOffsetValue = null;
			} else {
				ValidateDateString(value);

				DateTimeOffsetValue = new Date(value).ToDateTimeOffset();
			}
			this.RaisePropertyChanged(nameof(TextValue));
		}
	}

	private static void ValidateDateString(string value) {
		int segmentCount = 0;
		ReadOnlySpan<char> span = value.AsSpan();

		int start = 0;
		for (int i = 0; i <= span.Length; i++) {
			bool atEnd = i == span.Length;
			if (!atEnd && span[i] != '.') {
				continue;
			}

			var segment = span.Slice(start, i - start).Trim();
			start = i + 1;

			if (segment.Length == 0) {
				continue;
			}

			segmentCount++;
			switch (segmentCount) {
				case 1:
					ValidateYearSpan(segment);
					break;
				case 2:
					ValidateMonthSpan(segment);
					break;
				case 3:
					ValidateDaySpan(segment);
					return; // matches previous behavior: validate first 3 non-empty segments only
			}
		}

		if (segmentCount == 0) {
			throw new DataValidationException($"'{value}' is not a valid date, it should be in the format YYYY.MM.DD.");
		}
	}

	private static void ValidateYearSpan(ReadOnlySpan<char> value) {
		if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)) {
			throw new DataValidationException($"'{value}' is not a valid integer.");
		}
	}

	private static void ValidateMonthSpan(ReadOnlySpan<char> value) {
		if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var month)) {
			throw new DataValidationException($"'{value}' is not a valid integer.");
		}
		if (month is < 1 or > 12) {
			throw new DataValidationException($"'{value}' is not a valid month, it should be between 1 and 12.");
		}
	}

	private static void ValidateDaySpan(ReadOnlySpan<char> value) {
		if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var day)) {
			throw new DataValidationException($"'{value}' is not a valid integer.");
		}
		if (day is < 1 or > 31) {
			throw new DataValidationException($"'{value}' is not a valid day, it should be between 1 and 31.");
		}
	}

	public string? Tooltip { get; private set; }

	public bool UseDatePicker {
		get;
		set => this.RaiseAndSetIfChanged(ref field, value);
	} = false;

	public void ToggleUseDatePicker() {
		UseDatePicker = !UseDatePicker;
	}
	public void ClearValue() {
		TextValue = null;
	}
}