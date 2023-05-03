using commonItems;
using System;

namespace Fronter.Models.Configuration.Options; 

public class DateSelector : Selector {
	public DateSelector(BufferedReader reader) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("editable", reader => Editable = reader.GetString() == "true");
		parser.RegisterKeyword("value", reader => {
			var valueStr = reader.GetString();
			Value = string.IsNullOrWhiteSpace(valueStr) ? null : new Date(valueStr);
		});
		parser.RegisterKeyword("tooltip", reader => Tooltip = reader.GetString());
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}

	public bool Editable { get; private set; } = true; // editable unless disabled
	public DateTimeOffset? MinDate => DateTimeOffset.MinValue;
	public DateTimeOffset? DateTimeOffsetValue { get; set; }

	public Date? Value {
		get {
			if (DateTimeOffsetValue is null) {
				return null;
			}

			var offsetValue = DateTimeOffsetValue.Value;
			return new Date(offsetValue.Year, offsetValue.Month, offsetValue.Day);
		}
		set {
			if (value is null) {
				DateTimeOffsetValue = null;
			} else {
				var dt = new DateTime(value.Year, value.Month, value.Day);
				DateTimeOffsetValue = new DateTimeOffset(dt);
			}
		}
	}

	public string? Tooltip { get; private set; }
}