using commonItems;
using Fronter.Models.Configuration;
using log4net;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace Fronter.Services; 

public class ModCopier {
	private readonly Configuration config;
	private readonly ILog logger = LogManager.GetLogger("Mod copier");
	public ModCopier(Configuration config) {
		this.config = config;
	}
	
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

		var requiredFolders = config.RequiredFolders;
		var targetGameModPath = requiredFolders.FirstOrDefault(f => f?.Name == "targetGameModPath", null);
		if (targetGameModPath is null) {
			logger.Error("Copy failed - Target Folder isn't loaded!");
			return false;
		}
		var destModsFolder = targetGameModPath.Value;
		if (!Directory.Exists(destModsFolder)) {
			logger.Error("Copy failed - Target Folder does not exist!");
			return false;
		}
		var options = config.Options;
		string? targetName = null;
		foreach (var option in options) {
			var value = option.GetValue();
			if (option.Name == "output_name" && !string.IsNullOrEmpty(value)) {
				targetName = value;
			}
		}
		var requiredFiles = config.RequiredFiles;
		if (string.IsNullOrEmpty(targetName)) {
			var saveGame = requiredFiles.FirstOrDefault(f=> f?.Name == "SaveGame", null);
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

		var modFilePath = Path.Combine(outputFolder, $"{targetName}.mod");
		if (!File.Exists(modFilePath)) {
			logger.Error($"Copy Failed - Could not find mod: {modFilePath}");
			return false;
		}
		var modFolderPath = Path.Combine(outputFolder, targetName);
		if (!Directory.Exists(modFolderPath)) {
			logger.Error($"Copy Failed - Could not find mod folder: {modFolderPath}");
			return false;
		}

		var destModFilePath = Path.Combine(destModsFolder, $"{targetName}.mod");
		if (File.Exists(destModFilePath)) {
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
			if (!SystemUtils.TryCopyFile(modFilePath, destModFilePath)) {
				logger.Error($"Could not copy file: {modFilePath}\nto {destModFilePath}");
			}

			if (!SystemUtils.TryCopyFolder(modFolderPath, destModFolderPath)) {
				logger.Error($"Could not copy folder: {modFolderPath}\nto {destModFolderPath}");
			}
		}
		catch (Exception e) {
			logger.Error(e.ToString());
			return false;
		}
		logger.Notice($"Mod successfully copied to: {destModFolderPath}");
		
		CreatePlayset(destModsFolder, targetName, destModFolderPath);

		return true;
	}

	public void CreatePlayset(string targetModsDirectory, string modName, string destModFolder) {
		logger.Info("Setting up playset...");
		
		var gameDocsDirectory = Directory.GetParent(targetModsDirectory)?.FullName;
		if (gameDocsDirectory is null) {
			logger.Warn($"Couldn't get parent directory of \"{targetModsDirectory}\".");
			return;
		}

		var launcherDbPath = Path.Join(gameDocsDirectory, "launcher-v2_openbeta.sqlite");
		if (!File.Exists(launcherDbPath)) {
			logger.Warn("Launcher's database not found.");
			return;
		}

		string connectionString = $"URI=file:{launcherDbPath}";

		try {
			logger.Debug("Connecting to launcher's DB...");
			using var connection = new SQLiteConnection(connectionString);
			connection.Open();

			using var cmd = new SQLiteCommand(connection);
			
			var playsetName = $"{config.Name}: {modName}";
			
			// Check if a playset with the same name already exists.
			cmd.CommandText = $"SELECT COUNT(*) FROM playsets WHERE name='{playsetName}'";
			bool playsetExists = Convert.ToBoolean(cmd.ExecuteScalar());
			if (playsetExists) {
				if (config.OverwritePlayset) {
					DeactivateCurrentPlayset(cmd);
					
					logger.Debug("Updating playset...");
					cmd.CommandText = "UPDATE playsets SET isActive=true, createdOn=date('now') " +
					                  "WHERE name='{playsetName}'";
					cmd.ExecuteNonQuery();
					
					logger.Notice("Updated existing playset.");
				} else {
					logger.Notice("Playset already exists.");
				}
			} else {
				DeactivateCurrentPlayset(cmd);
				
				logger.Debug("Creating new playset...");
				var playsetId = Guid.NewGuid().ToString();
				cmd.CommandText = "INSERT INTO playsets(id, name, isActive, isRemoved, hasNotApprovedChanges, createdOn) " +
				                  $"VALUES('{playsetId}', '{playsetName}', true, false, false, date('now'))";
				cmd.ExecuteNonQuery();
			
				logger.Debug("Saving generated mod to DB...");
				var modId = Guid.NewGuid().ToString();
				var gameRegistryId = Path.Join("mod", $"{modName}.mod").Replace('\\', '/');
				cmd.CommandText = "INSERT INTO mods(id, status, source, version, gameRegistryId, name, dirPath) " +
				                  $"VALUES('{modId}', 'ready_to_play', 'local', 1, '{gameRegistryId}', '{modName}', '{destModFolder}')";
				cmd.ExecuteNonQuery();
			
				logger.Debug("Adding mod to created playset...");
				cmd.CommandText = $"INSERT INTO playsets_mods(playsetId, modId) VALUES('{playsetId}', '{modId}')";
				cmd.ExecuteNonQuery();

				logger.Notice("Successfully created a new playset with the generated mod.");
			}
		} catch(Exception e) {
			logger.Error(e);
		}
	}

	private void DeactivateCurrentPlayset(IDbCommand cmd) {
		logger.Debug("Deactivating currently active playset...");
		cmd.CommandText = "UPDATE playsets SET isActive=false";
		cmd.ExecuteNonQuery();
	}
}