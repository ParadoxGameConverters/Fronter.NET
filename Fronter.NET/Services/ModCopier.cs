using commonItems;
using Fronter.Models.Configuration;
using log4net;
using System;
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
		Logger.Notice("Mod Copying Started.");
		var converterFolder = config.ConverterFolder;
		if (!Directory.Exists(converterFolder)) {
			logger.Error("Copy failed - where is the converter?");
			return false;
		}
		if (!Directory.Exists(converterFolder + "/output")) {
			logger.Error("Copy failed - where is the converter's output folder?");
			return false;
		}

		var requiredFolders = config.RequiredFolders;
		var targetGameModPath = requiredFolders.FirstOrDefault(f => f?.Name == "targetGameModPath", null);
		if (targetGameModPath is null) {
			logger.Error("Copy failed - Target Folder isn't loaded!");
			return false;
		}
		var destinationFolder = targetGameModPath.Value;
		if (!Directory.Exists(destinationFolder)) {
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

		var modFilePath = Path.Combine(converterFolder, "output", $"{targetName}.mod");
		if (!File.Exists(modFilePath)) {
			logger.Error($"Copy Failed - Could not find mod: {modFilePath}");
			return false;
		}
		var modFolderPath = Path.Combine(converterFolder, "output", targetName);
		if (!Directory.Exists(modFolderPath)) {
			logger.Error($"Copy Failed - Could not find mod folder: {modFolderPath}");
			return false;
		}

		var destModFilePath = Path.Combine(destinationFolder, $"{targetName}.mod");
		if (File.Exists(destModFilePath)) {
			logger.Info("Previous mod file found, deleting...");
			File.Delete(destModFilePath);
		}

		var destModFolderPath = Path.Combine(destinationFolder, targetName);
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
		return true;
	}
}