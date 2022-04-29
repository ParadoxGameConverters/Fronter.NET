using Avalonia.Data;
using commonItems;
using Fronter.Extensions;
using System.IO;

namespace Fronter.Models.Configuration;

public class RequiredFolder : RequiredPath {
	public RequiredFolder(BufferedReader reader) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("name", reader => Name = reader.GetString());
		parser.RegisterKeyword("tooltip", reader => Tooltip = reader.GetString());
		parser.RegisterKeyword("displayName", reader => DisplayName = reader.GetString());
		parser.RegisterKeyword("mandatory", reader => Mandatory = reader.GetString() == "true");
		parser.RegisterKeyword("outputtable", reader => Outputtable = reader.GetString() == "true");

		parser.RegisterKeyword("searchPathType", reader => SearchPathType = reader.GetString());
		parser.RegisterKeyword("searchPathID", reader => SearchPathId = reader.GetString());
		parser.RegisterKeyword("searchPath", reader => SearchPath = reader.GetString());
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}

	// If we have folders listed, they are generally required. Override with false in conf file.
	public override bool Outputtable { get; protected set; } = true;

	public string SearchPathId { get; private set; } = string.Empty;

	public override string Value {
		get => base.Value;
		set {
			if (!string.IsNullOrEmpty(value) && !Directory.Exists(value)) {
				throw new DataValidationException("Directory does not exist!");
			}

			base.Value = value;
			Logger.Info($"{TranslationSource.Instance[DisplayName]} set to: {value}");
		}
	}
}