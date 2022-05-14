using Fronter.Models;
using Fronter.Services;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Xunit;

namespace Fronter.Tests.Services;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class UpdateCheckerTests {
	private const string TestImperatorToCK3CommitIdTxtPath = "UpdateChecker/commit_id.txt";
	private const string ImperatorToCK3CommitUrl = "https://paradoxgameconverters.com/commit_ids/ImperatorToCK3.txt";

	static UpdateCheckerTests() {
		App.ConfigureLogging();
	}
	
	[Fact]
	public void IncorrectCommitIdTxtPathIsLogged() {
		const string wrongCommitIdTxtPath = "missingFile.txt";
		
		var isUpdateAvailable = UpdateChecker.IsUpdateAvailable(wrongCommitIdTxtPath, ImperatorToCK3CommitUrl);
		Assert.False(isUpdateAvailable);
		
		using var fileStream = new FileStream("log.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		using var reader = new StreamReader(fileStream);
		Assert.Contains($"File \"{wrongCommitIdTxtPath}\" does not exist!", reader.ReadToEnd());
	}
	
	[Fact]
	public void IncorrectCommitIdUrlIsLogged() {
		const string wrongCommitIdUrl = "https://paradoxgameconverters.com/wrong_url";
		
		var isUpdateAvailable = UpdateChecker.IsUpdateAvailable(TestImperatorToCK3CommitIdTxtPath, wrongCommitIdUrl);
		Assert.False(isUpdateAvailable);
		
		using var fileStream = new FileStream("log.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		using var reader = new StreamReader(fileStream);
		Assert.Contains($"Failed to get commit id from \"{wrongCommitIdUrl}\"!", reader.ReadToEnd());
	}
	
	[Fact]
	public void UpdateCheckerCanFindUpdate() {
		Assert.True(UpdateChecker.IsUpdateAvailable(TestImperatorToCK3CommitIdTxtPath,
			ImperatorToCK3CommitUrl));
	}
	
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