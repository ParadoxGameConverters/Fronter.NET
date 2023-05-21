using Avalonia.Data;
using commonItems;
using Fronter.Extensions;
using log4net;
using System.IO;

namespace Fronter.Models.Configuration;

public class RequiredFolder : RequiredPath {
	private static readonly ILog logger = LogManager.GetLogger("Required folder");
	public RequiredFolder(BufferedReader reader, Configuration configuration) {
		config = configuration;

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
		parser.RegisterKeyword("steamGameID", reader => SteamGameId = reader.GetString());
		parser.RegisterKeyword("gogGameID", reader => GOGGameId = reader.GetString());
		parser.RegisterKeyword("searchPath", reader => SearchPath = reader.GetString());
		parser.IgnoreAndLogUnregisteredItems();
	}

	// If we have folders listed, they are generally required. Override with false in conf file.
	public override bool Outputtable { get; protected set; } = true;

	public string? SteamGameId { get; private set; }
	public string? GOGGameId { get; private set; }

	public override string Value {
		get => base.Value;
		set {
			if (!string.IsNullOrEmpty(value) && !Directory.Exists(value)) {
				throw new DataValidationException("Directory does not exist!");
			}

			base.Value = value;
			logger.Info($"{TranslationSource.Instance[DisplayName]} set to: {value}");

			if (Name == config.ModAutoGenerationSource) {
				config.AutoLocateMods();
			}
		}
	}

	private readonly Configuration config;
}