using System;
using System.IO;

namespace Fronter;

// Central place for resolving app-relative file paths.
// All paths must be rooted at AppContext.BaseDirectory (the directory containing
// the executable) rather than the current working directory: on macOS, launching
// the app from Finder/Dock sets the working directory to "/", which used to make
// the app unable to find its files (see ImperatorToCK3 issue #2471).
internal static class FronterPaths {
	public static string BaseDirectory => AppContext.BaseDirectory;

	public static string ConfigurationDirectoryPath => Path.Combine(BaseDirectory, "Configuration");

	public static string ThemeFilePath => Path.Combine(ConfigurationDirectoryPath, "fronter-theme.txt");

	public static string LogFilePath => Path.Combine(BaseDirectory, "log.txt");

	public static string TempDirectoryPath => Path.Combine(BaseDirectory, "temp");

	public static string UpdaterDirectoryPath => Path.Combine(BaseDirectory, "Updater");

	public static string UpdaterRunningDirectoryPath => Path.Combine(BaseDirectory, "Updater-running");
}