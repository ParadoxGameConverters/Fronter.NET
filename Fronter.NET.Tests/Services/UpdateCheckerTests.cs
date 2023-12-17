using commonItems;
using Fronter.Models;
using Fronter.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace Fronter.Tests.Services;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class UpdateCheckerTests {
	private const string TestImperatorToCK3CommitIdTxtPath = "UpdateChecker/commit_id.txt";
	private const string ImperatorToCK3CommitUrl = "https://paradoxgameconverters.com/commit_ids/ImperatorToCK3.txt";

	[Fact]
	public async void IncorrectCommitIdTxtPathIsLogged() {
		var stringWriter = new StringWriter();
		System.Console.SetOut(stringWriter);
		LoggingConfigurator.ConfigureLogging(useConsole: true);

		const string wrongCommitIdTxtPath = "missingFile.txt";

		var isUpdateAvailable = await UpdateChecker.IsUpdateAvailable(wrongCommitIdTxtPath, ImperatorToCK3CommitUrl);
		Assert.False(isUpdateAvailable);

		var consoleOutput = stringWriter.ToString();
		Assert.Contains($"File \"{wrongCommitIdTxtPath}\" does not exist!", consoleOutput);
	}

	[Fact]
	public async void IncorrectCommitIdUrlIsLogged() {
		var stringWriter = new StringWriter();
		System.Console.SetOut(stringWriter);
		LoggingConfigurator.ConfigureLogging(useConsole: true);

		const string wrongCommitIdUrl = "https://paradoxgameconverters.com/wrong_url";

		var isUpdateAvailable = await UpdateChecker.IsUpdateAvailable(TestImperatorToCK3CommitIdTxtPath, wrongCommitIdUrl);
		Assert.False(isUpdateAvailable);

		var consoleOutput = stringWriter.ToString();
		Assert.Contains($"Failed to get commit id from \"{wrongCommitIdUrl}\"; status code: NotFound!", consoleOutput);
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
		Assert.NotNull(info.AssetUrl);
		Assert.StartsWith($"https://github.com/ParadoxGameConverters/ImperatorToCK3/releases/download/{info.Version}/ImperatorToCK3", info.AssetUrl);

		string extension = CommonFunctions.GetExtension(info.AssetUrl);
		if (OperatingSystem.IsWindows()) {
			List<string> expectedExtensions = ["exe", "zip"];
			Assert.Contains(extension, expectedExtensions);
		} else {
			Assert.Equal("tgz", extension);
		}
	}
}