using Fronter.Services;
using System;
using System.IO;
using Xunit;

namespace Fronter.Tests.Services;

public class TargetDbManagerTests {
	[Fact]
	public void GetLastUpdatedLauncherDbPath_ReturnsNewestExistingLauncherDatabase() {
		var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(tempDir);

		try {
			var olderPath = Path.Combine(tempDir, "launcher-v2.sqlite");
			var newerPath = Path.Combine(tempDir, "launcher-v2_openbeta.sqlite");

			File.WriteAllText(olderPath, "older");
			File.WriteAllText(newerPath, "newer");

			File.SetLastWriteTimeUtc(olderPath, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
			File.SetLastWriteTimeUtc(newerPath, new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc));

			Assert.Equal(newerPath, TargetDbManager.GetLastUpdatedLauncherDbPath(tempDir));
		} finally {
			if (Directory.Exists(tempDir)) {
				Directory.Delete(tempDir, recursive: true);
			}
		}
	}

	[Fact]
	public void GetLastUpdatedLauncherDbPath_ReturnsNullWhenNoDatabaseExists() {
		var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(tempDir);

		try {
			Assert.Null(TargetDbManager.GetLastUpdatedLauncherDbPath(tempDir));
		} finally {
			if (Directory.Exists(tempDir)) {
				Directory.Delete(tempDir, recursive: true);
			}
		}
	}
}
