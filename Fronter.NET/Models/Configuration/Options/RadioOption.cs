using commonItems;

namespace Fronter.Models.Configuration.Options;

public class RadioOption {
	public RadioOption(BufferedReader reader, int id) {
		Id = id;

		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("name", reader => Name = reader.GetString());
		parser.RegisterKeyword("tooltip", reader => Tooltip = reader.GetString());
		parser.RegisterKeyword("displayName", reader => DisplayName = reader.GetString());
		parser.RegisterKeyword("default", reader => {
			Defaulted = reader.GetString() == "true";
			if (Defaulted) {
				Value = true;
			}
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}

	public void SetValue() {
		Value = true;
	}
	public void UnsetValue() {
		Value = false;
	}

	public string Name { get; private set; } = string.Empty;
	public string DisplayName { get; private set; } = string.Empty;
	public string Tooltip { get; private set; } = string.Empty;
	public bool Value { get; private set; } = false;
	public bool Defaulted { get; private set; } = false;
	public int Id { get; private set; } = 0;
}