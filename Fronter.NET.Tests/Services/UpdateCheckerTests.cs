using commonItems;
using Fronter.Models;
using Fronter.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Fronter.Tests.Services;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class UpdateCheckerTests {
	private const string TestImperatorToCK3CommitIdTxtPath = "UpdateChecker/commit_id.txt";
	private const string ImperatorToCK3CommitUrl = "https://paradoxgameconverters.com/commit_ids/ImperatorToCK3.txt";

	[Fact]
	public async Task IncorrectCommitIdTxtPathIsLogged() {
		var stringWriter = new StringWriter();
		Console.SetOut(stringWriter);
		LoggingConfigurator.ConfigureLogging(useConsole: true);

		const string wrongCommitIdTxtPath = "missingFile.txt";

		var isUpdateAvailable = await UpdateChecker.IsUpdateAvailable(wrongCommitIdTxtPath, ImperatorToCK3CommitUrl);
		Assert.False(isUpdateAvailable);

		var consoleOutput = stringWriter.ToString();
		Assert.Contains($"File \"{wrongCommitIdTxtPath}\" does not exist!", consoleOutput);
	}

	[Fact]
	public async Task IncorrectCommitIdUrlIsLogged() {
		var stringWriter = new StringWriter();
		Console.SetOut(stringWriter);
		LoggingConfigurator.ConfigureLogging(useConsole: true);

		const string wrongCommitIdUrl = "https://paradoxgameconverters.com/wrong_url";

		var isUpdateAvailable = await UpdateChecker.IsUpdateAvailable(TestImperatorToCK3CommitIdTxtPath, wrongCommitIdUrl);
		Assert.False(isUpdateAvailable);

		var consoleOutput = stringWriter.ToString();
		Assert.Contains($"Failed to get commit id from \"{wrongCommitIdUrl}\"; status code: NotFound!", consoleOutput);
	}

	[Fact]
	public async Task UpdateCheckerCanFindUpdate() {
		bool isUpdateAvailable = await UpdateChecker.IsUpdateAvailable(TestImperatorToCK3CommitIdTxtPath,
			ImperatorToCK3CommitUrl);
		Assert.True(isUpdateAvailable);
	}

	[Fact]
	public async Task LatestReleaseInfoIsDownloaded() {
		var releaseInfo = new {
			body = "Bug fixes and improvements.",
			name = "1.2.3",
			assets = new[] {
				new {
					name = "ImperatorToCK3-win-x64.exe",
					browser_download_url = "https://github.com/ParadoxGameConverters/ImperatorToCK3/releases/download/1.2.3/ImperatorToCK3-win-x64.exe"
				},
				new {
					name = "ImperatorToCK3-win-x64.zip",
					browser_download_url = "https://github.com/ParadoxGameConverters/ImperatorToCK3/releases/download/1.2.3/ImperatorToCK3-win-x64.zip"
				},
				new {
					name = "ImperatorToCK3-linux-x64.tgz",
					browser_download_url = "https://github.com/ParadoxGameConverters/ImperatorToCK3/releases/download/1.2.3/ImperatorToCK3-linux-x64.tgz"
				},
				new {
					name = "ImperatorToCK3-osx-arm64.tgz",
					browser_download_url = "https://github.com/ParadoxGameConverters/ImperatorToCK3/releases/download/1.2.3/ImperatorToCK3-osx-arm64.tgz"
				}
			}
		};

		string releaseInfoJson = JsonSerializer.Serialize(releaseInfo);
		var httpMessageHandler = new MockHttpMessageHandler(_ => {
			var response = new HttpResponseMessage(HttpStatusCode.OK) {
				Content = new StringContent(releaseInfoJson, Encoding.UTF8, "application/json")
			};
			return response;
		});
		using var testHttpClient = new HttpClient(httpMessageHandler) {Timeout = TimeSpan.FromMinutes(5)};
		UpdateInfoModel info = await UpdateChecker.GetLatestReleaseInfo("ImperatorToCK3", testHttpClient);

			Assert.Equal("1.2.3", info.Version);
			Assert.Equal("Bug fixes and improvements.", info.Description);
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

	private sealed class MockHttpMessageHandler : HttpMessageHandler {
		private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

		public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) {
			_responder = responder;
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
			cancellationToken.ThrowIfCancellationRequested();
			return Task.FromResult(_responder(request));
		}
	}
}