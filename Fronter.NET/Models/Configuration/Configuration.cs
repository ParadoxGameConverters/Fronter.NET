using Avalonia.Controls.ApplicationLifetimes;
using commonItems;
using Fronter.Models.Configuration.Options;
using Fronter.ViewModels;
using log4net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Fronter.Models.Configuration;

public class Configuration {
	public string Name { get; private set; } = string.Empty;
	public string ConverterFolder { get; private set; } = string.Empty;
	public string BackendExePath { get; private set; } = string.Empty; // relative to ConverterFolder
	public string DisplayName { get; private set; } = string.Empty;
	public string SourceGame { get; private set; } = string.Empty;
	public string TargetGame { get; private set; } = string.Empty;
	public string? TargetPlaysetsSource { get; private set; } = null;
	public ObservableCollection<TargetPlayset> AutoLocatedMods { get; } = new();
	public bool CopyToTargetGameModDirectory { get; set; } = true;
	public ushort ProgressOnCopyingComplete { get; set; } = 109;
	public bool OverwritePlayset { get; set; } = false;
	public bool UpdateCheckerEnabled { get; private set; } = false;
	public bool CheckForUpdatesOnStartup { get; private set; } = false;
	public string ConverterReleaseForumThread { get; private set; } = string.Empty;
	public string LatestGitHubConverterReleaseUrl { get; private set; } = string.Empty;
	public string PagesCommitIdUrl { get; private set; } = string.Empty;
	public List<RequiredFile> RequiredFiles { get; } = new();
	public List<RequiredFolder> RequiredFolders { get; } = new();
	public List<Option> Options { get; } = new();
	private int optionCounter;

	private static readonly ILog logger = LogManager.GetLogger("Configuration");

	public Configuration() {
		var parser = new Parser();
		RegisterKeys(parser);
		var fronterConfigurationPath = Path.Combine("Configuration", "fronter-configuration.txt");
		if (File.Exists(fronterConfigurationPath)) {
			parser.ParseFile(fronterConfigurationPath);
			logger.Info("Frontend configuration loaded.");
		} else {
			logger.Warn($"{fronterConfigurationPath} not found!");
		}

		var fronterOptionsPath = Path.Combine("Configuration", "fronter-options.txt");
		if (File.Exists(fronterOptionsPath)) {
			parser.ParseFile(fronterOptionsPath);
			logger.Info("Frontend options loaded.");
		} else {
			logger.Warn($"{fronterOptionsPath} not found!");
		}

		InitializePaths();

		LoadExistingConfiguration();
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
			var newFolder = new RequiredFolder(reader, this);
			if (!string.IsNullOrEmpty(newFolder.Name)) {
				RequiredFolders.Add(newFolder);
			} else {
				logger.Error("Required Folder has no mandatory field: name!");
			}
		});
		parser.RegisterKeyword("requiredFile", reader => {
			var newFile = new RequiredFile(reader);
			if (!string.IsNullOrEmpty(newFile.Name)) {
				RequiredFiles.Add(newFile);
			} else {
				logger.Error("Required File has no mandatory field: name!");
			}
		});
		parser.RegisterKeyword("option", reader => {
			var newOption = new Option(reader, ++optionCounter);
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
		parser.RegisterKeyword("targetPlaysetsSource", reader => {
			TargetPlaysetsSource = reader.GetString();
		});
		parser.RegisterKeyword("copyToTargetGameModDirectory", reader => {
			CopyToTargetGameModDirectory = reader.GetString() == "true";
		});
		parser.RegisterKeyword("progressOnCopyingComplete", reader => {
			ProgressOnCopyingComplete = (ushort)reader.GetInt();
		});
		parser.RegisterKeyword("overwritePlayset", reader => {
			OverwritePlayset = reader.GetString() == "true";
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
		parser.IgnoreAndLogUnregisteredItems();
	}

	private void RegisterPreloadKeys(Parser parser) {
		parser.RegisterRegex(CommonRegexes.String, (reader, incomingKey) => {
			var valueStringOfItem = reader.GetStringOfItem();
			var valueStr = valueStringOfItem.ToString().RemQuotes();
			var valueReader = new BufferedReader(valueStr);

			foreach (var folder in RequiredFolders) {
				if (folder.Name == incomingKey && Directory.Exists(valueStr)) {
					folder.Value = valueStr;
				}
			}

			foreach (var file in RequiredFiles) {
				if (file.Name == incomingKey && File.Exists(valueStr)) {
					file.Value = valueStr;
				}
			}
			foreach (var option in Options) {
				if (option.Name == incomingKey && option.CheckBoxSelector is null) {
					option.SetValue(valueStr);
				} else if (option.Name == incomingKey && option.CheckBoxSelector is not null) {
					var selections = valueReader.GetStrings();
					var values = selections.ToHashSet();
					option.SetValue(values);
					option.SetCheckBoxSelectorPreloaded();
				}
			}
			if (incomingKey == "selectedMods") {
				var theList = valueReader.GetStrings();
				var matchingMods = AutoLocatedMods.Where(m => theList.Contains(m.FileName));
				foreach (var mod in matchingMods) {
					mod.Enabled = true;
				}
			}
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}

	public void InitializePaths() {
		if (!OperatingSystem.IsWindows()) {
			return;
		}

		string documentsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

		foreach (var folder in RequiredFolders) {
			string? initialValue = null;

			if (!string.IsNullOrEmpty(folder.Value)) {
				continue;
			}

			if (folder.SearchPathType == "windowsUsersFolder") {
				initialValue = Path.Combine(documentsDir, folder.SearchPath);
			} else if (folder.SearchPathType == "storeFolder") {
				string? possiblePath = null;
				if (int.TryParse(folder.SteamGameId, out int steamId)) {
					possiblePath = CommonFunctions.GetSteamInstallPath(steamId);
				}
				if (possiblePath is null && long.TryParse(folder.GOGGameId, out long gogId)) {
					possiblePath = CommonFunctions.GetGOGInstallPath(gogId);
				}

				if (possiblePath is null) {
					continue;
				}

				initialValue = possiblePath;
				if (!string.IsNullOrEmpty(folder.SearchPath)) {
					initialValue = Path.Combine(initialValue, folder.SearchPath);
				}
			} else if (folder.SearchPathType == "direct") {
				initialValue = folder.SearchPath;
			}

			if (Directory.Exists(initialValue)) {
				folder.Value = initialValue;
			}
		}

		foreach (var file in RequiredFiles) {
			string? initialDirectory = null;
			string? initialValue = null;

			if (!string.IsNullOrEmpty(file.Value)) {
				initialDirectory = CommonFunctions.GetPath(file.Value);
			} else if (file.SearchPathType == "windowsUsersFolder") {
				initialDirectory = Path.Combine(documentsDir, file.SearchPath);
				if (!string.IsNullOrEmpty(file.FileName)) {
					initialValue = Path.Combine(initialDirectory, file.FileName);
				}
			} else if (file.SearchPathType == "converterFolder") {
				var currentDir = Directory.GetCurrentDirectory();
				initialDirectory = Path.Combine(currentDir, file.SearchPath);
				if (!string.IsNullOrEmpty(file.FileName)) {
					initialValue = Path.Combine(initialDirectory, file.FileName);
				}
			}

			if (string.IsNullOrEmpty(file.Value) && File.Exists(initialValue)) {
				file.Value = initialValue;
			}

			if (Directory.Exists(initialDirectory)) {
				file.InitialDirectory = initialDirectory;
			}
		}
	}

	public void LoadExistingConfiguration() {
		var parser = new Parser();
		RegisterPreloadKeys(parser);
		var converterConfigurationPath = Path.Combine(ConverterFolder, "configuration.txt");
		if (string.IsNullOrEmpty(ConverterFolder) || !File.Exists(converterConfigurationPath)) {
			return;
		}

		logger.Info("Previous configuration located, preloading selections...");
		parser.ParseFile(converterConfigurationPath);
	}

	public bool ExportConfiguration() {
		SetSavingStatus("CONVERTSTATUSIN");

		if (string.IsNullOrEmpty(ConverterFolder)) {
			logger.Error("Converter folder is not set!");
			SetSavingStatus("CONVERTSTATUSPOSTFAIL");
			return false;
		}
		if (!Directory.Exists(ConverterFolder)) {
			logger.Error("Could not find converter folder!");
			SetSavingStatus("CONVERTSTATUSPOSTFAIL");
			return false;
		}

		var outConfPath = Path.Combine(ConverterFolder, "configuration.txt");
		try {
			using var writer = new StreamWriter(outConfPath);
			foreach (var folder in RequiredFolders) {
				writer.WriteLine($"{folder.Name} = \"{folder.Value}\"");
			}

			foreach (var file in RequiredFiles) {
				if (!file.Outputtable) {
					continue;
				}
				writer.WriteLine($"{file.Name} = \"{file.Value}\"");
			}

			writer.WriteLine("selectedMods = {");
			foreach (var mod in AutoLocatedMods) {
				if (mod.Enabled) {
					writer.WriteLine($"\t\"{mod.FileName}\"");
				}
			}
			writer.WriteLine("}");

			foreach (var option in Options) {
				if (option.CheckBoxSelector is not null) {
					writer.Write($"{option.Name} = {{ ");
					foreach (var value in option.GetValues()) {
						writer.Write($"\"{value}\" ");
					}
					writer.WriteLine("}");
				} else {
					writer.WriteLine($"{option.Name} = \"{option.GetValue()}\"");
				}
			}

			SetSavingStatus("CONVERTSTATUSPOSTSUCCESS");
			return true;
		} catch (Exception ex) {
			logger.Error($"Could not open configuration.txt! Error: {ex}");
			SetSavingStatus("CONVERTSTATUSPOSTFAIL");
			return false;
		}
	}

	private static void SetSavingStatus(string locKey) {
		if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) {
			return;
		}

		if (desktop.MainWindow?.DataContext is MainWindowViewModel mainWindowDataContext) {
			mainWindowDataContext.SaveStatus = locKey;
		}
	}

	public void AutoLocatePlaylists() {
		logger.Debug("Clearing previously located playlists...");
		AutoLocatedPlaylists.Clear();
		logger.Debug("Autolocating mods...");

		// Do we have a mod path?
		string? modPath = null;
		foreach (var folder in RequiredFolders) {
			if (folder.Name == ModAutoGenerationSource) {
				modPath = folder.Value;
			}
		}
		if (modPath is null) {
			logger.Warn("No folder found as source for mods autolocation.");
			return;
		}

		// Does it exist?
		if (!Directory.Exists(modPath)) {
			logger.Warn($"Mod path \"{modPath}\" does not exist or can not be accessed!");
			return;
		}

		logger.Debug($"Mods autolocation path set to: \"{modPath}\".");

		// Are there mods inside?
		var validModFiles = new List<string>();
		foreach (var file in SystemUtils.GetAllFilesInFolder(modPath)) {
			var lastDot = file.LastIndexOf('.');
			if (lastDot == -1) {
				continue;
			}

			var extension = CommonFunctions.GetExtension(file);
			if (extension != "mod") {
				continue;
			}

			validModFiles.Add(file);
		}

		if (validModFiles.Count == 0) {
			logger.Debug($"No mod files could be found in \"{modPath}\"");
			return;
		}

		foreach (var modFile in validModFiles) {
			var path = Path.Combine(modPath, modFile);
			var theMod = new Mod(path);
			if (string.IsNullOrEmpty(theMod.Name)) {
				logger.Warn($"Mod at \"{path}\" has no defined name, skipping.");
				continue;
			}
			AutoLocatedMods.Add(theMod);
		}
		logger.Debug($"Autolocated {AutoLocatedMods.Count} mods");
	}
}