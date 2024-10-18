using commonItems;

namespace Fronter.Models.Configuration.Options;

public sealed class TextSelector {
	public TextSelector(BufferedReader reader) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("editable", reader => Editable = string.Equals(reader.GetString(), "true", System.StringComparison.OrdinalIgnoreCase));
		parser.RegisterKeyword("value", reader => Value = reader.GetString());
		parser.RegisterKeyword("tooltip", reader => Tooltip = reader.GetString());
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}

	public bool Editable { get; private set; } = true; // editable unless disabled
	public string Value { get; set; } = string.Empty;
	public string? Tooltip { get; private set; }
}