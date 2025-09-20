using Avalonia.Controls.ApplicationLifetimes;
using commonItems;
using Fronter.Models.Configuration.Options;
using Fronter.Models.Database;
using Fronter.Services;
using Fronter.ViewModels;
using log4net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Fronter.Models.Configuration;

internal sealed class Config {
	public string Name { get; private set; } = string.Empty;
	public string ConverterFolder { get; private set; } = string.Empty;
	public string BackendExePath { get; private set; } = string.Empty; // relative to ConverterFolder
	public string DisplayName { get; private set; } = string.Empty;
	public string SourceGame { get; private set; } = string.Empty;
	public string TargetGame { get; private set; } = string.Empty;
	public string? SentryDsn { get; private set; }
	public bool TargetPlaysetSelectionEnabled { get; private set; } = false;
	public ObservableCollection<Playset> AutoLocatedPlaysets { get; } = [];
	public Playset? SelectedPlayset { get; set; }
	public bool CopyToTargetGameModDirectory { get; set; } = true;
	public ushort ProgressOnCopyingComplete { get; set; } = 109;
	public bool UpdateCheckerEnabled { get; private set; } = false;
	public bool CheckForUpdatesOnStartup { get; private set; } = false;
	public string ConverterReleaseForumThread { get; private set; } = string.Empty;
	public string LatestGitHubConverterReleaseUrl { get; private set; } = string.Empty;
	public string PagesCommitIdUrl { get; private set; } = string.Empty;
	public List<RequiredFile> RequiredFiles { get; } = [];
	public List<RequiredFolder> RequiredFolders { get; } = [];
	public List<Option> Options { get; } = [];
	private int optionCounter;

	private static readonly ILog logger = LogManager.GetLogger("Configuration");

	public Config() {
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
		parser.RegisterKeyword("name", reader => Name = reader.GetString());
		parser.RegisterKeyword("sentryDsn", reader => SentryDsn = reader.GetString());
		parser.RegisterKeyword("converterFolder", reader => ConverterFolder = reader.GetString());
		parser.RegisterKeyword("backendExePath", reader => BackendExePath = reader.GetString());
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
		parser.RegisterKeyword("displayName", reader => DisplayName = reader.GetString());
		parser.RegisterKeyword("sourceGame", reader => SourceGame = reader.GetString());
		parser.RegisterKeyword("targetGame", reader => TargetGame = reader.GetString());
		parser.RegisterKeyword("targetPlaysetSelectionEnabled", reader => {
			TargetPlaysetSelectionEnabled = reader.GetBool();
		});
		parser.RegisterKeyword("copyToTargetGameModDirectory", reader => {
			CopyToTargetGameModDirectory = reader.GetString().Equals("true");
		});
		parser.RegisterKeyword("progressOnCopyingComplete", reader => {
			ProgressOnCopyingComplete = (ushort)reader.GetInt();
		});
		parser.RegisterKeyword("enableUpdateChecker", reader => {
			UpdateCheckerEnabled = reader.GetString().Equals("true");
		});
		parser.RegisterKeyword("checkForUpdatesOnStartup", reader => {
			CheckForUpdatesOnStartup = reader.GetString().Equals("true");
		});
		parser.RegisterKeyword("converterReleaseForumThread", reader => {
			ConverterReleaseForumThread = reader.GetString();
		});
		parser.RegisterKeyword("latestGitHubConverterReleaseUrl", reader => {
			LatestGitHubConverterReleaseUrl = reader.GetString();
		});
		parser.RegisterKeyword("pagesCommitIdUrl", reader => PagesCommitIdUrl = reader.GetString());
		parser.IgnoreAndLogUnregisteredItems();
	}

	private void RegisterPreloadKeys(Parser parser) {
		parser.RegisterRegex(CommonRegexes.String, (reader, incomingKey) => {
			StringOfItem valueStringOfItem = reader.GetStringOfItem();
			string valueStr = valueStringOfItem.ToString().RemQuotes();
			var valueReader = new BufferedReader(valueStr);

			foreach (var folder in RequiredFolders) {
				if (folder.Name.Equals(incomingKey) && Directory.Exists(valueStr)) {
					folder.Value = valueStr;
				}
			}

			foreach (var file in RequiredFiles) {
				if (file.Name.Equals(incomingKey) && File.Exists(valueStr)) {
					file.Value = valueStr;
				}
			}
			foreach (var option in Options) {
				if (option.Name.Equals(incomingKey) && option.CheckBoxSelector is null) {
					option.SetValue(valueStr);
				} else if (option.Name.Equals(incomingKey) && option.CheckBoxSelector is not null) {
					var selections = valueReader.GetStrings();
					var values = selections.ToHashSet(StringComparer.Ordinal);
					option.SetValue(values);
					option.SetCheckBoxSelectorPreloaded();
				}
			}
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}

	private void InitializePaths() {
		if (!OperatingSystem.IsWindows()) {
			return;
		}

		string documentsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
		InitializeFolders(documentsDir);
		InitializeFiles(documentsDir);
	}

	private void InitializeFolders(string documentsDir) {
		foreach (var folder in RequiredFolders) {
			string? initialValue = null;

			if (!string.IsNullOrEmpty(folder.Value)) {
				continue;
			}

			if (folder.SearchPathType.Equals("windowsUsersFolder")) {
				initialValue = Path.Combine(documentsDir, folder.SearchPath);
			} else if (folder.SearchPathType.Equals("storeFolder")) {
				string? possiblePath = null;
				if (uint.TryParse(folder.SteamGameId, out uint steamId)) {
					possiblePath = CommonFunctions.GetSteamInstallPath(steamId);
				}
				if (possiblePath is null && long.TryParse(folder.GOGGameId, CultureInfo.InvariantCulture, out long gogId)) {
					possiblePath = CommonFunctions.GetGOGInstallPath(gogId);
				}

				if (possiblePath is null) {
					continue;
				}

				initialValue = possiblePath;
				if (!string.IsNullOrEmpty(folder.SearchPath)) {
					initialValue = Path.Combine(initialValue, folder.SearchPath);
				}
			} else if (folder.SearchPathType.Equals("direct")) {
				initialValue = folder.SearchPath;
			}

			if (Directory.Exists(initialValue)) {
				folder.Value = initialValue;
			}
		}
	}

	private void InitializeFiles(string documentsDir) {
		foreach (var file in RequiredFiles) {
			string? initialDirectory = null;
			string? initialValue = null;

			if (!string.IsNullOrEmpty(file.Value)) {
				initialDirectory = CommonFunctions.GetPath(file.Value);
			} else if (file.SearchPathType.Equals("windowsUsersFolder")) {
				initialDirectory = Path.Combine(documentsDir, file.SearchPath);
				if (!string.IsNullOrEmpty(file.FileName)) {
					initialValue = Path.Combine(initialDirectory, file.FileName);
				}
			} else if (file.SearchPathType.Equals("converterFolder")) {
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

			WriteRequiredFolders(writer);
			WriteRequiredFiles(writer);
			if (SelectedPlayset is not null) {
				writer.WriteLine($"selectedPlayset = {SelectedPlayset.Id}");
			}

			WriteOptions(writer);

			SetSavingStatus("CONVERTSTATUSPOSTSUCCESS");
			return true;
		} catch (Exception ex) {
			logger.Error($"Could not open configuration.txt! Error: {ex}");
			SetSavingStatus("CONVERTSTATUSPOSTFAIL");
			return false;
		}
	}

	private void WriteOptions(StreamWriter writer) {
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
	}

	private void WriteRequiredFiles(StreamWriter writer) {
		foreach (var file in RequiredFiles) {
			if (!file.Outputtable) {
				continue;
			}

			// In the file path, replace backslashes with forward slashes.
			string pathToWrite = file.Value.Replace('\\', '/');
			writer.WriteLine($"{file.Name} = \"{pathToWrite}\"");
		}
	}

	private void WriteRequiredFolders(StreamWriter writer) {
		foreach (var folder in RequiredFolders) {
			// In the folder path, replace backslashes with forward slashes.
			string pathToWrite = folder.Value.Replace('\\', '/');
			writer.WriteLine($"{folder.Name} = \"{pathToWrite}\"");
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

	public string? TargetGameModsPath {
		get {
			var targetGameModPath = RequiredFolders
				.FirstOrDefault(f => f?.Name == "targetGameModPath", defaultValue: null);
			return targetGameModPath?.Value;
		}
	}

	public void AutoLocatePlaysets() {
		logger.Debug("Clearing previously located playsets...");
		AutoLocatedPlaysets.Clear();
		logger.Debug("Autolocating playsets...");

		var destModsFolder = TargetGameModsPath;
		var locatedPlaysetsCount = 0;
		if (destModsFolder is not null) {
			var dbContext = TargetDbManager.GetLauncherDbContext(this);
			if (dbContext is not null) {
				foreach (var playset in dbContext.Playsets.Where(p => p.IsRemoved == null || p.IsRemoved == false )) {
					AutoLocatedPlaysets.Add(playset);
				}
			}
			
			locatedPlaysetsCount = AutoLocatedPlaysets.Count;
		}
		
		logger.Debug($"Autolocated {locatedPlaysetsCount} playsets.");
	}

	private static List<string> GetValidModFiles(string modPath) {
		var validModFiles = new List<string>();
		foreach (var file in SystemUtils.GetAllFilesInFolder(modPath)) {
			var lastDot = file.LastIndexOf('.');
			if (lastDot == -1) {
				continue;
			}

			var extension = CommonFunctions.GetExtension(file);
			if (!extension.Equals("mod")) {
				continue;
			}

			validModFiles.Add(file);
		}

		return validModFiles;
	}
}