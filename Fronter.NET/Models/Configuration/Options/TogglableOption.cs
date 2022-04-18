using commonItems;
using ReactiveUI;

namespace Fronter.Models.Configuration.Options;

public class TogglableOption : ReactiveObject {
	public TogglableOption(BufferedReader reader, int id) {
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
			var value = reader.GetString() == "true";
			PendingInitialValue = value;
			if (value) {
				Value = true;
			}
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}

	public string Name { get; private set; } = string.Empty;
	public string DisplayName { get; private set; } = string.Empty;
	public string Tooltip { get; private set; } = string.Empty;
	public bool? PendingInitialValue { get; set; }
	private bool boolValue = false;
	public bool Value { get => boolValue; set => this.RaiseAndSetIfChanged(ref boolValue, value); }
	public int Id { get; private set; } = 0;
}