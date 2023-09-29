using Fronter.Models;
using Fronter.Services;
using log4net;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Xunit;
using FluentAssertions;

namespace Fronter.Tests.Services;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class UpdateCheckerTests {
	private const string TestImperatorToCK3CommitIdTxtPath = "UpdateChecker/commit_id.txt";
	private const string ImperatorToCK3CommitUrl = "https://paradoxgameconverters.com/commit_ids/ImperatorToCK3.txt";

	[Fact]
	public async void IncorrectCommitIdTxtPathIsLogged() {
		// redirect console output to textwriter
		var stringWriter = new StringWriter();
		System.Console.SetOut(stringWriter);

		
		LoggingConfigurator.ConfigureLogging(useConsole: true);

/* 		var logManager = LogManager.GetRepository();
		logManager.GetAppenders()
			.Select(a => a.GetType().ToString())
			.Should().Equal("file", "grid");
		Assert.Equal(2, logManager.GetAppenders().Count()); // TODO: REMOVE */

		const string wrongCommitIdTxtPath = "missingFile.txt";

		var isUpdateAvailable = await UpdateChecker.IsUpdateAvailable(wrongCommitIdTxtPath, ImperatorToCK3CommitUrl);
		Assert.False(isUpdateAvailable);

		var consoleOutput = stringWriter.ToString();
		Assert.Contains($"File \"{wrongCommitIdTxtPath}\" does not exist!", consoleOutput);
	}

	[Fact]
	public async void IncorrectCommitIdUrlIsLogged() {
		const string wrongCommitIdUrl = "https://paradoxgameconverters.com/wrong_url";

		var isUpdateAvailable = await UpdateChecker.IsUpdateAvailable(TestImperatorToCK3CommitIdTxtPath, wrongCommitIdUrl);
		Assert.False(isUpdateAvailable);

		await using var fileStream = new FileStream("log.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		using var reader = new StreamReader(fileStream);
		Assert.Contains($"Failed to get commit id from \"{wrongCommitIdUrl}\"; status code: NotFound!", await reader.ReadToEndAsync());
	}

	[Fact]
	public async void UpdateCheckerCanFindUpdate() {
		bool isUpdateAvailable = await UpdateChecker.IsUpdateAvailable(TestImperatorToCK3CommitIdTxtPath,
			ImperatorToCK3CommitUrl);
		Assert.True(isUpdateAvailable);
	}

	[Fact]
	public async void LatestReleaseInfoIsDownloaded() {
		UpdateInfoModel info = await UpdateChecker.GetLatestReleaseInfo("ImperatorToCK3");

		var versionRegex = new Regex(@"^\d+\.\d+\.\d+$");
		Assert.Matches(versionRegex, info.Version);
		Assert.False(string.IsNullOrWhiteSpace(info.Description));
		Assert.NotNull(info.ArchiveUrl);
		Assert.StartsWith($"https://github.com/ParadoxGameConverters/ImperatorToCK3/releases/download/{info.Version}/ImperatorToCK3", info.ArchiveUrl);

		var expectedExtension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".zip" : ".tgz";
		Assert.EndsWith(expectedExtension, info.ArchiveUrl);
	}
}