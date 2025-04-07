using Avalonia.Data;
using commonItems;
using ReactiveUI;
using System;
using System.Linq;

namespace Fronter.Models.Configuration.Options;

public sealed class DateSelector : ReactiveObject {
	public DateSelector(BufferedReader reader) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("editable", reader => Editable = string.Equals(reader.GetString(), "true", StringComparison.OrdinalIgnoreCase));
		parser.RegisterKeyword("value", reader => {
			var valueStr = reader.GetString();
			Value = string.IsNullOrWhiteSpace(valueStr) ? null : new Date(valueStr);
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
				DateTimeOffsetValue = value.ToDateTimeOffset();
			}
		}
	}

	public string? TextValue {
		get => Value?.ToString();
		set {
			if (string.IsNullOrWhiteSpace(value)) {
				DateTimeOffsetValue = null;
			} else {
				var dateElements = value.Split('.').Where(x => !string.IsNullOrEmpty(x)).ToArray();
				if (dateElements.Length >= 3) {
					ValidateYearString(dateElements[0]);
					ValidateMonthString(dateElements[1]);
					ValidateDayString(dateElements[2]);
				} else if (dateElements.Length == 2) {
					ValidateYearString(dateElements[0]);
					ValidateMonthString(dateElements[1]);
				} else if (dateElements.Length == 1) {
					ValidateYearString(dateElements[0]);
				} else {
					throw new DataValidationException($"'{value}' is not a valid date, it should be in the format YYYY.MM.DD.");
				}

				DateTimeOffsetValue = new Date(value).ToDateTimeOffset();
			}
		}
	}

	private static void ValidateYearString(string value) {
		if (!int.TryParse(value, out var year)) {
			throw new DataValidationException($"'{value}' is not a valid integer.");
		}
	}
	private static void ValidateMonthString(string value) {
		if (!int.TryParse(value, out var month)) {
			throw new DataValidationException($"'{value}' is not a valid integer.");
		}
		if (month is < 1 or > 12) {
			throw new DataValidationException($"'{value}' is not a valid month, it should be between 1 and 12.");
		}
	}
	private static void ValidateDayString(string value) {
		if (!int.TryParse(value, out var day)) {
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
		DateTimeOffsetValue = null;
		TextValue = null;
	}
}