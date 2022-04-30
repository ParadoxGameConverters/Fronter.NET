using Fronter.Models;
using Fronter.Services;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Xunit;

namespace Fronter.Tests.Services;

public class UpdateCheckerTests {
	[Fact]
	public void LatestReleaseInfoIsDownloaded() {
		UpdateInfoModel info = UpdateChecker.GetLatestReleaseInfo("ImperatorToCK3");

		var versionRegex = new Regex(@"^\d+\.\d+\.\d+$");
		Assert.Matches(versionRegex, info.Version);
		Assert.False(string.IsNullOrWhiteSpace(info.Description));
		Assert.NotNull(info.ArchiveUrl);
		Assert.StartsWith($"https://github.com/ParadoxGameConverters/ImperatorToCK3/releases/download/{info.Version}/ImperatorToCK3", info.ArchiveUrl);
		
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
			Assert.EndsWith(".zip", info.ArchiveUrl);
		} else {
			Assert.EndsWith(".tgz", info.ArchiveUrl);
		}
	}
}