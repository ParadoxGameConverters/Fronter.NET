using Fronter.Models.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Fronter.Services;

internal class TargetDbManager {
	public static string? GetLastUpdatedLauncherDbPath(string gameDocsDirectory) {
		var possibleDbFileNames = new List<string> { "launcher-v2.sqlite", "launcher-v2_openbeta.sqlite" };
		var latestDbFilePath = possibleDbFileNames
			.Select(name => Path.Join(gameDocsDirectory, name))
			.Where(File.Exists)
			.OrderByDescending(File.GetLastWriteTimeUtc)
			.FirstOrDefault(defaultValue: null);
		return latestDbFilePath;
	}

	public static LauncherDbContext? GetLauncherDbContext(Config config) {
		var targetGameModsPath = config.TargetGameModsPath;
		if (string.IsNullOrWhiteSpace(targetGameModsPath)) {
			return null;
		}
		var gameDocsDirectory = Directory.GetParent(targetGameModsPath)?.FullName;
		if (gameDocsDirectory is null) {
			return null;
		}
		
		var dbPath = GetLastUpdatedLauncherDbPath(gameDocsDirectory);
		if (dbPath is null) {
			return null;
		}
		
		string connectionString = $"Data Source={dbPath};";
		return new LauncherDbContext(connectionString);
	}
}