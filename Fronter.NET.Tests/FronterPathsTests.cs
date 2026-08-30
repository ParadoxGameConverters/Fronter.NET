using System;
using System.IO;
using Xunit;

namespace Fronter.Tests;

// This collection disables parallelization, so changing the process working
// directory in these tests cannot affect other tests.
[Collection("Sequential")]
public sealed class FronterPathsTests {
	[Fact]
	public void AllPathsAreRootedInBaseDirectoryIndependentlyOfWorkingDirectory() {
		// Reproduce the macOS launch scenario: the process working directory is
		// NOT the directory containing the executable (Finder/Dock set CWD to "/").
		var decoyDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		var previousWorkingDirectory = Directory.GetCurrentDirectory();
		try {
			Directory.CreateDirectory(decoyDirectory);
			Directory.SetCurrentDirectory(decoyDirectory);

			var baseDirectory = FronterPaths.BaseDirectory;
			Assert.Equal(AppContext.BaseDirectory, baseDirectory);
			Assert.Equal(Path.Combine(baseDirectory, "log.txt"), FronterPaths.LogFilePath);
			Assert.Equal(Path.Combine(baseDirectory, "Configuration", "fronter-theme.txt"), FronterPaths.ThemeFilePath);
			Assert.Equal(Path.Combine(baseDirectory, "temp"), FronterPaths.TempDirectoryPath);
			Assert.Equal(Path.Combine(baseDirectory, "Updater"), FronterPaths.UpdaterDirectoryPath);
			Assert.Equal(Path.Combine(baseDirectory, "Updater-running"), FronterPaths.UpdaterRunningDirectoryPath);

			// Every path must be absolute and point inside the base directory.
			string[] paths = [
				FronterPaths.LogFilePath,
				FronterPaths.ThemeFilePath,
				FronterPaths.TempDirectoryPath,
				FronterPaths.UpdaterDirectoryPath,
				FronterPaths.UpdaterRunningDirectoryPath,
			];
			foreach (var path in paths) {
				Assert.True(Path.IsPathFullyQualified(path), $"Path is not fully qualified: {path}");
				Assert.StartsWith(baseDirectory, path);
			}
		}
		finally {
			Directory.SetCurrentDirectory(previousWorkingDirectory);
			if (Directory.Exists(decoyDirectory)) {
				Directory.Delete(decoyDirectory, recursive: true);
			}
		}
	}
}