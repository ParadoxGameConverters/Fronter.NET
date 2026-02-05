using commonItems;
using Fronter.Models.Configuration;
using Fronter.Models.Database;
using log4net;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Mod = Fronter.Models.Database.Mod;

namespace Fronter.Services;

internal sealed class ModCopier(Config config) {
	private readonly ILog logger = LogManager.GetLogger("Mod copier");

	public bool CopyMod() {
		logger.Notice("Mod Copying Started.");
		var converterFolder = config.ConverterFolder;
		if (!Directory.Exists(converterFolder)) {
			logger.Error("Copy failed - where is the converter?");
			return false;
		}

		var outputFolder = Path.Combine(converterFolder, "output");
		if (!Directory.Exists(outputFolder)) {
			logger.Error("Copy failed - where is the converter's output folder?");
			return false;
		}

		string? destModsFolder = config.TargetGameModsPath;
		if (destModsFolder is null) {
			logger.Error("Copy failed - Target Folder isn't loaded!");
			return false;
		}
		if (!Directory.Exists(destModsFolder)) {
			logger.Error("Copy failed - Target Folder does not exist!");
			return false;
		}
		var options = config.Options;
		string? targetName = null;
		foreach (var option in options) {
			var value = option.GetValue();
			if (option.Name.Equals("output_name") && !string.IsNullOrEmpty(value)) {
				targetName = value;
			}
		}
		var requiredFiles = config.RequiredFiles;
		if (string.IsNullOrEmpty(targetName)) {
			var saveGame = requiredFiles.FirstOrDefault(f => string.Equals(f?.Name, "SaveGame", StringComparison.Ordinal), defaultValue: null);
			if (saveGame is null) {
				logger.Error("Copy failed - SaveGame is does not exist!");
				return false;
			}
			var saveGamePath = saveGame.Value;
			if (string.IsNullOrEmpty(saveGamePath)) {
				logger.Error("Copy Failed - save game path is empty, did we even convert anything?");
				return false;
			}
			if (!File.Exists(saveGamePath)) {
				logger.Error("Copy Failed - save game does not exist, did we even convert anything?");
				return false;
			}
			if (Directory.Exists(saveGamePath)) {
				logger.Error("Copy Failed - Save game is a directory...");
				return false;
			}
			saveGamePath = CommonFunctions.TrimPath(saveGamePath);
			saveGamePath = CommonFunctions.NormalizeStringPath(saveGamePath);
			var pos = saveGamePath.LastIndexOf('.');
			if (pos != -1) {
				saveGamePath = saveGamePath[..pos];
			}
			targetName = saveGamePath;
		}

		targetName = CommonFunctions.ReplaceCharacter(targetName, '-');
		targetName = CommonFunctions.ReplaceCharacter(targetName, ' ');
		targetName = CommonFunctions.NormalizeUTF8Path(targetName);

		var modFolderPath = Path.Combine(outputFolder, targetName);
		if (!Directory.Exists(modFolderPath)) {
			logger.Error($"Copy Failed - Could not find mod folder: {modFolderPath}");
			return false;
		}

		// For games using mods with .metadata folders we need to skip .mod file requirement.
		bool skipModFile = false;
		var metadataPath = Path.Combine(outputFolder, $"{targetName}/.metadata");
		if (Directory.Exists(metadataPath)) {
			skipModFile = true;
		}

		var modFilePath = Path.Combine(outputFolder, $"{targetName}.mod");
		if (!skipModFile && !File.Exists(modFilePath)) {
			logger.Error($"Copy Failed - Could not find mod: {modFilePath}");
			return false;
		}

		var destModFilePath = Path.Combine(destModsFolder, $"{targetName}.mod");
		if (!skipModFile && File.Exists(destModFilePath)) {
			logger.Info("Previous mod file found, deleting...");
			File.Delete(destModFilePath);
		}

		var destModFolderPath = Path.Combine(destModsFolder, targetName);
		if (Directory.Exists(destModFolderPath)) {
			logger.Info("Previous mod directory found, deleting...");
			if (!SystemUtils.TryDeleteFolder(destModFolderPath)) {
				logger.Error($"Could not delete directory: {destModFolderPath}");
				return false;
			}
		}
		try {
			logger.Info("Copying mod to target location...");
			if (!skipModFile) {
				if (!SystemUtils.TryCopyFile(modFilePath, destModFilePath)) {
					logger.Error($"Could not copy file: {modFilePath}\nto {destModFilePath}");
				}
			}
			if (!SystemUtils.TryCopyFolder(modFolderPath, destModFolderPath)) {
				logger.Error($"Could not copy folder: {modFolderPath}\nto {destModFolderPath}");
			}
		} catch (Exception e) {
			logger.Error(e.ToString());
			return false;
		}
		logger.Notice($"Mod successfully copied to: {destModFolderPath}");

		CreatePlayset(destModsFolder, targetName, destModFolderPath);

		return true;
	}

	private void CreatePlayset(string targetModsDirectory, string modName, string destModFolder) {
		var gameDocsDirectory = Directory.GetParent(targetModsDirectory)?.FullName;
		if (gameDocsDirectory is null) {
			logger.Warn($"Couldn't get parent directory of \"{targetModsDirectory}\".");
			return;
		}
		try {
			var dbContext = TargetDbManager.GetLauncherDbContext(config);
			if (dbContext is null) {
				logger.Debug("Launcher's database not found.");
				return;
			}
			logger.Debug("Connecting to launcher's DB...");

			var playsetName = $"{config.Name}: {modName}";
			var dateTimeOffset = new DateTimeOffset(DateTime.UtcNow);
			string unixTimeMilliSeconds = dateTimeOffset.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);

			DeactivateCurrentPlayset(dbContext);

			// Check if a playset with the same name already exists.
			var playset = dbContext.Playsets.FirstOrDefault(p => p.Name == playsetName);
			if (playset is not null) {
				logger.Debug("Removing mods from existing playset...");
				dbContext.PlaysetsMods.RemoveRange(dbContext.PlaysetsMods.Where(pm => pm.PlaysetId == playset.Id));
				dbContext.SaveChanges();

				logger.Debug("Re-activating existing playset...");
				// Set isActive to true and updatedOn to current time.
				playset.IsActive = true;
				playset.UpdatedOn = unixTimeMilliSeconds;
				dbContext.SaveChanges();

				logger.Notice("Updated existing playset.");
			} else {
				logger.Debug("Creating new playset...");
				playset = new Playset {
					Id = Guid.NewGuid().ToString(),
					Name = playsetName,
					IsActive = true,
					IsRemoved = false,
					HasNotApprovedChanges = false,
					CreatedOn = unixTimeMilliSeconds
				};
				dbContext.Playsets.Add(playset);
				dbContext.SaveChanges();
			}

			Logger.Debug("Adding mods to playset...");
			var playsetInfo = LoadPlaysetInfo();
			if (playsetInfo.Count == 0) {
				var gameRegistryId = $"mod/{modName}.mod";
				var mod = AddModToDb(dbContext, modName, gameRegistryId, destModFolder);
				AddModToPlayset(dbContext, mod, playset);
			}
			foreach (var (playsetModName, playsetModPath) in playsetInfo) {
				string playsetModPathWithBackSlashes = playsetModPath.Replace('/', '\\');

				// Try to get an ID of existing matching mod.
				var mod = dbContext.Mods.FirstOrDefault(m => m.Name == playsetModName ||
															  m.DirPath == playsetModPath ||
															  m.DirPath == playsetModPathWithBackSlashes);
				if (mod is not null) {
					AddModToPlayset(dbContext, mod, playset);
				} else {
					var gameRegistryId = playsetModPath;
					if (!gameRegistryId.StartsWith("mod/", StringComparison.Ordinal)) {
						gameRegistryId = $"mod/{gameRegistryId}";
					}
					if (!gameRegistryId.EndsWith(".mod", StringComparison.Ordinal)) {
						gameRegistryId = $"{gameRegistryId}.mod";
					}

					string dirPath;
					if (Path.IsPathRooted(playsetModPath)) {
						dirPath = playsetModPath;
					} else {
						dirPath = Path.Combine(gameDocsDirectory, gameRegistryId);
					}

					mod = AddModToDb(dbContext, modName, gameRegistryId, dirPath);
					AddModToPlayset(dbContext, mod, playset);
				}
			}

			logger.Notice("Successfully set up playset.");
		} catch (Exception e) {
			logger.Error(e);
		}
	}

	// Returns saved mod.
	private Mod AddModToDb(LauncherDbContext dbContext, string modName, string gameRegistryId, string dirPath) {
		logger.Debug($"Saving mod \"{modName}\" to DB...");

		var mod = new Mod {
			Id = Guid.NewGuid().ToString(),
			Status = "ready_to_play",
			Source = "local",
			Version = "1",
			GameRegistryId = gameRegistryId,
			Name = modName,
			DirPath = dirPath,
		};
		dbContext.Mods.Add(mod);
		dbContext.SaveChanges();

		return mod;
	}

	private static void AddModToPlayset(LauncherDbContext dbContext, Mod mod, Playset playset) {
		var playsetMod = new PlaysetsMod {
			Playset = playset,
			Mod = mod,
		};
		dbContext.PlaysetsMods.Add(playsetMod);
		dbContext.SaveChanges();
	}

	// Loads playset info generated by converter backend.
	private Open.Collections.OrderedDictionary<string, string> LoadPlaysetInfo() {
		logger.Debug("Loading playset info from converter backend...");
		var toReturn = new Open.Collections.OrderedDictionary<string, string>();

		var filePath = Path.Combine(config.ConverterFolder, "playset_info.txt");
		if (!File.Exists(filePath)) {
			return toReturn;
		}

		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.QuotedString, (reader, modName) => {
			toReturn.Add(modName, reader.GetString().RemQuotes());
		});
		parser.ParseFile(filePath);
		return toReturn;
	}

	private void DeactivateCurrentPlayset(LauncherDbContext dbContext) {
		logger.Debug("Deactivating currently active playset...");
		dbContext.Playsets
			.Where(p => p.IsActive == true)
			.ToList()
			.ForEach(p => p.IsActive = false);
		dbContext.SaveChanges();
	}
}