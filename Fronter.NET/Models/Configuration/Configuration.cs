using commonItems;
using Fronter.Models.Configuration.Options;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Fronter.Models.Configuration;

public class Configuration {
	public string Name { get; private set; }
	public string ConverterFolder { get; private set; }
	public string BackendExePath { get; private set; } // relative to ConverterFolder
	public string DisplayName { get; private set; }
	public string SourceGame { get; private set; }
	public string TargetGame { get; private set; }
	public string AutoGenerateModsFrom { get; private set; }
	public bool UpdateCheckerEnabled { get; private set; } = false;
	public bool CheckForUpdatesOnStartup { get; private set; } = false;
	public string ConverterReleaseForumThread { get; private set; }
	public string LatestGitHubConverterReleaseUrl { get; private set; }
	public string PagesCommitIdUrl { get; private set; }
	public Dictionary<string, RequiredFile> RequiredFiles { get; } = new();
	public Dictionary<string, RequiredFolder> RequiredFolders { get; } = new();
	public List<Option> Options { get; } = new();
	public List<Mod> AutoLocatedMods { get; } = new();
	public HashSet<string> PreloadedModFileNames { get; } = new();
	private int optionCounter;

	public Configuration() {
		var parser = new Parser();
		RegisterKeys(parser);
		if (File.Exists("Configuration/fronter-configuration.txt")) {
			parser.ParseFile("Configuration/fronter-configuration.txt");
			Logger.Info("Frontend configuration loaded.");
		} else {
			Logger.Warn("Configuration/fronter-configuration.txt not found!");
		}
		if (File.Exists("Configuration/fronter-options.txt")) {
			parser.ParseFile("Configuration/fronter-options.txt");
			Logger.Info("Frontend options loaded.");
		} else {
			Logger.Warn("Configuration/fronter-options.txt not found!");
		}
		parser.ClearRegisteredRules();

		RegisterPreloadKeys(parser);
		if (!string.IsNullOrEmpty(ConverterFolder) && File.Exists(ConverterFolder + "/configuration.txt")) {
			Logger.Info("Previous configuration located, preloading selections.");
			parser.ParseFile(ConverterFolder + "/configuration.txt");
		}
	}

	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("name", reader => {
			Name = reader.GetString();
		});
		parser.RegisterKeyword("converterFolder", reader => {
			ConverterFolder = reader.GetString();
		});
		parser.RegisterKeyword("backendExePath", reader => {
			BackendExePath = reader.GetString();
		});
		parser.RegisterKeyword("requiredFolder", reader => {
			var newFolder = new RequiredFolder(reader);
			if (!string.IsNullOrEmpty(newFolder.Name))
				RequiredFolders.Add(newFolder.Name, newFolder);
			else
				Logger.Error("Required Folder has no mandatory field: name!");
		});
		parser.RegisterKeyword("requiredFile", reader => {
			var newFile = new RequiredFile(reader);
			if (!string.IsNullOrEmpty(newFile.Name))
				RequiredFiles.Add(newFile.Name, newFile);
			else
				Logger.Error("Required File has no mandatory field: name!");
		});
		parser.RegisterKeyword("option", reader => {
			++optionCounter;
			var newOption = new Option(reader, optionCounter);
			Options.Add(newOption);
		});
		parser.RegisterKeyword("displayName", reader => {
			DisplayName = reader.GetString();
		});
		parser.RegisterKeyword("sourceGame", reader => {
			SourceGame = reader.GetString();
		});
		parser.RegisterKeyword("targetGame", reader => {
			TargetGame = reader.GetString();
		});
		parser.RegisterKeyword("autoGenerateModsFrom", reader => {
			AutoGenerateModsFrom = reader.GetString();
		});
		parser.RegisterKeyword("enableUpdateChecker", reader => {
			UpdateCheckerEnabled = reader.GetString() == "true";
		});
		parser.RegisterKeyword("checkForUpdatesOnStartup", reader => {
			CheckForUpdatesOnStartup = reader.GetString() == "true";
		});
		parser.RegisterKeyword("converterReleaseForumThread", reader => {
			ConverterReleaseForumThread = reader.GetString();
		});
		parser.RegisterKeyword("latestGitHubConverterReleaseUrl", reader => {
			LatestGitHubConverterReleaseUrl = reader.GetString();
		});
		parser.RegisterKeyword("pagesCommitIdUrl", reader => {
			PagesCommitIdUrl = reader.GetString();
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}

	private void RegisterPreloadKeys(Parser parser) {
		parser.RegisterRegex("[a-zA-Z0-9_-]+", (reader, incomingKey) => {
			var valueStr = reader.GetStringOfItem();
			var valueReader = new BufferedReader(valueStr.ToString());
			var theString = valueReader.GetString();

			if (incomingKey == "configuration") {
				Logger.Warn("You have an old configuration file. Preload will not be possible.");
				return;
			}

			foreach (var (requiredFolderName, folder) in RequiredFolders) {
				if (requiredFolderName == incomingKey)
					folder.Value = theString;
			}

			foreach (var (requiredFileName, file) in RequiredFiles) {
				if (requiredFileName == incomingKey)
					file.Value = theString;
			}
			foreach (var option in Options) {
				if (option.Name == incomingKey && option.CheckBoxSelector is null) {
					option.SetValue(theString);
				} else if (option.Name == incomingKey && option.CheckBoxSelector is not null) {
					var selections = valueReader.GetStrings();
					var values = selections.ToHashSet();
					option.SetValue(values);
					option.SetCheckBoxSelectorPreloaded();
				}
			}
			if (incomingKey == "selectedMods") {
				var selections = valueReader.GetStrings();
				PreloadedModFileNames.UnionWith(selections);
			}
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}
}