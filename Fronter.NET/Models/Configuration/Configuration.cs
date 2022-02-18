using commonItems;
using Fronter.Models.Configuration.Options;
using System.Collections.Generic;
using System.IO;

namespace Fronter.Models.Configuration;

public class Configuration {
	public string Name { get; private set; }
	public string ConverterFolder { get; private set; }
	public string BackendExePath { get; private set; } // relative to ConverterFolder
	public string DisplayName { get; private set; }
	public string SourceGame { get; private set; }
	public string TargetGame { get; private set; }
	public string AutoGenerateModsFrom { get; private set; }
	public bool EnableUpdateChecker { get; private set; } = false;
	public bool CheckForUpdatesOnStartup { get; private set; } = false;
	public string ConverterReleaseForumThread { get; private set; }
	public string LatestGitHubConverterReleaseThread { get; private set; }
	public string PagesCommitIdUrl { get; private set; }
	public Dictionary<string, RequiredFile> RequiredFiles { get; } = new();
	public Dictionary<string, RequiredFolder> RequiredFolders { get; } = new();
	public List<Option> Options { get; } = new();
	public List<Mod> AutoLocatedMods { get; } = new();
	public HashSet<string> PreloadedModFileNames { get; } = new();

	public Configuration() {
		File.Delete("log.txt");

	}

	private void RegisterKeys(commonItems.Parser parser) {
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
			EnableUpdateChecker = reader.GetString() == "true";
		});
		parser.RegisterKeyword("checkForUpdatesOnStartup", reader => {
			CheckForUpdatesOnStartup = reader.GetString() == "true";
		});
		parser.RegisterKeyword("converterReleaseForumThread", reader => {
			ConverterReleaseForumThread = reader.GetString();
		});
		parser.RegisterKeyword("latestGitHubConverterReleaseUrl", reader => {
			LatestGitHubConverterReleaseThread = reader.GetString();
		});
		parser.RegisterKeyword("pagesCommitIdUrl", reader => {
			PagesCommitIdUrl = reader.GetString();
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}
}