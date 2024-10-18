using commonItems;
using ReactiveUI;
using System;

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
	private DateTimeOffset? dateTimeOffsetValue;
	public DateTimeOffset? DateTimeOffsetValue {
		get => dateTimeOffsetValue;
		set {
			this.RaiseAndSetIfChanged(ref dateTimeOffsetValue, value);
		}
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

	public string? Tooltip { get; private set; }

	public void ClearValue() {
		DateTimeOffsetValue = null;
	}
}