using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Notification;
using commonItems;
using Fronter.Extensions;
using Fronter.Models;
using Fronter.Models.Configuration;
using Fronter.Views;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Fronter.Services;

internal static class UpdateChecker {
	private static readonly ILog Logger = LogManager.GetLogger("Update checker");
	private static readonly HttpClient SharedHttpClient = CreateSharedHttpClient();

	private static HttpClient CreateSharedHttpClient() => new() {Timeout = TimeSpan.FromMinutes(5)};
	public static async Task<bool> IsUpdateAvailable(string commitIdFilePath, string commitIdUrl, HttpClient? httpClient = null) {
		if (!File.Exists(commitIdFilePath)) {
			Logger.Debug($"File \"{commitIdFilePath}\" does not exist!");
			return false;
		}

		var client = httpClient ?? SharedHttpClient;
		try {
			var response = await client.GetAsync(commitIdUrl);
			if (!response.IsSuccessStatusCode) {
				Logger.Warn($"Failed to get commit id from \"{commitIdUrl}\"; status code: {response.StatusCode}!");
				return false;
			}

			var latestReleaseCommitId = await response.Content.ReadAsStringAsync();
			latestReleaseCommitId = latestReleaseCommitId.Trim();

			using var commitIdFileReader = new StreamReader(commitIdFilePath);
			var localCommitId = (await commitIdFileReader.ReadLineAsync())?.Trim();

			return localCommitId is not null && !localCommitId.Equals(latestReleaseCommitId);
		} catch (Exception e) {
			Logger.Warn($"Failed to get commit id from \"{commitIdUrl}\"; {e}!");
			return false;
		}
	}

	public static async Task<UpdateInfoModel> GetAvailableSemverUpdateInfo(string converterName, string converterFolder, HttpClient? httpClient = null) {
		if (!TryGetLocalConverterVersion(converterFolder, out var localVersion)) {
			Logger.Debug("Skipping semver update check because converter version could not be determined.");
			return new UpdateInfoModel();
		}

		return await GetAvailableSemverUpdateInfo(converterName, localVersion, httpClient);
	}

	internal static async Task<UpdateInfoModel> GetAvailableSemverUpdateInfo(string converterName, Version localVersion, HttpClient? httpClient = null) {
		var releases = await GetReleaseInfos(converterName, httpClient);
		var newerStableReleases = releases
			.Select(release => new {
				Release = release,
				Version = TryGetStableReleaseVersion(release, out var releaseVersion) ? releaseVersion : null,
			})
			.Where(item => item.Version is not null && item.Version > localVersion)
			.OrderByDescending(item => item.Version)
			.ToList();

		if (newerStableReleases.Count == 0) {
			return new UpdateInfoModel();
		}

		var latestRelease = newerStableReleases[0].Release;
		var latestVersion = newerStableReleases[0].Version;
		if (latestVersion is null) {
			return new UpdateInfoModel();
		}

		var osNameAndArch = GetOSNameAndArch();
		if (osNameAndArch is null) {
			return new UpdateInfoModel();
		}

		var info = new UpdateInfoModel {
			Version = NormalizeVersion(latestVersion),
			Description = BuildCombinedChangelog(newerStableReleases.Select(item => item.Release)),
		};
		DetermineReleaseBuildUrl(latestRelease, info, osNameAndArch.Value.Item1, osNameAndArch.Value.Item2);

		if (info.AssetUrl is null) {
			Logger.Debug($"Release {info.Version} doesn't have a release build for this platform.");
			return new UpdateInfoModel();
		}

		return info;
	}

	private static (string, string)? GetOSNameAndArch() {
		if (OperatingSystem.IsWindows()) {
			return ("win", "x64");
		}
		if (OperatingSystem.IsLinux()) {
			return ("linux", "x64");
		}
		if (OperatingSystem.IsMacOS()) {
			return ("osx", "arm64");
		}
		return null;
	}

	public static async Task<UpdateInfoModel> GetLatestReleaseInfo(string converterName, HttpClient? httpClient = null) {
		var osNameAndArch = GetOSNameAndArch();
		if (osNameAndArch is null) {
			return new UpdateInfoModel();
		}

		var osName = osNameAndArch.Value.Item1;
		var architecture = osNameAndArch.Value.Item2;

		var info = new UpdateInfoModel();
		var releaseInfo = await GetLatestRelease(converterName, httpClient);
		if (releaseInfo is null) {
			return info;
		}

		info.Description = releaseInfo.Body;
		info.Version = GetDisplayVersion(releaseInfo);

		DetermineReleaseBuildUrl(releaseInfo, info, osName, architecture);

		if (info.AssetUrl is null) {
			Logger.Debug($"Release {info.Version} doesn't have a release build for this platform.");
		}

		return info;
	}

	private static async Task<ConverterReleaseInfo?> GetLatestRelease(string converterName, HttpClient? httpClient) {
		var apiUrl = $"https://api.github.com/repos/ParadoxGameConverters/{converterName}/releases/latest";
		var requestMessage = CreateGitHubRequest(apiUrl);

		var client = httpClient ?? SharedHttpClient;
		try {
			using var responseMessage = await client.SendAsync(requestMessage);
			if (!responseMessage.IsSuccessStatusCode) {
				Logger.Warn($"Failed to get release info from \"{apiUrl}\"; status code: {responseMessage.StatusCode}!");
				return null;
			}

			await using var responseStream = await responseMessage.Content.ReadAsStreamAsync();
			return await JsonSerializer.DeserializeAsync<ConverterReleaseInfo>(responseStream);
		} catch (Exception e) {
			Logger.Warn($"Failed to get release info from \"{apiUrl}\": {e}!");
			return null;
		}
	}

	private static async Task<ConverterReleaseInfo[]> GetReleaseInfos(string converterName, HttpClient? httpClient) {
		var apiUrl = $"https://api.github.com/repos/ParadoxGameConverters/{converterName}/releases";
		var requestMessage = CreateGitHubRequest(apiUrl);

		var client = httpClient ?? SharedHttpClient;
		try {
			using var responseMessage = await client.SendAsync(requestMessage);
			if (!responseMessage.IsSuccessStatusCode) {
				Logger.Warn($"Failed to get release info from \"{apiUrl}\"; status code: {responseMessage.StatusCode}!");
				return [];
			}

			await using var responseStream = await responseMessage.Content.ReadAsStreamAsync();
			return await JsonSerializer.DeserializeAsync<ConverterReleaseInfo[]>(responseStream) ?? [];
		} catch (Exception e) {
			Logger.Warn($"Failed to get release info from \"{apiUrl}\": {e}!");
			return [];
		}
	}

	private static HttpRequestMessage CreateGitHubRequest(string apiUrl) {
		var requestMessage = new HttpRequestMessage(HttpMethod.Get, apiUrl);
		requestMessage.Headers.Add("User-Agent", "ParadoxGameConverters");
		return requestMessage;
	}

	private static bool TryGetLocalConverterVersion(string converterFolder, out Version version) {
		version = new Version();
		var versionFilePath = Path.Combine(converterFolder, "configurables/version.txt");
		if (!File.Exists(versionFilePath)) {
			return false;
		}

		var converterVersion = new ConverterVersion();
		converterVersion.LoadVersion(versionFilePath);
		return TryParseSemanticVersion(converterVersion.Version, out version, out _);
	}

	private static bool TryGetStableReleaseVersion(ConverterReleaseInfo release, out Version version) {
		version = new Version();
		if (release.Draft || release.Prerelease) {
			return false;
		}

		string? rawVersion = release.TagName;
		if (string.IsNullOrWhiteSpace(rawVersion)) {
			rawVersion = release.Name;
		}

		return TryParseSemanticVersion(rawVersion, out version, out var isPrerelease) && !isPrerelease;
	}

	private static bool TryParseSemanticVersion(string? rawVersion, out Version version, out bool isPrerelease) {
		version = new Version();
		isPrerelease = false;
		if (string.IsNullOrWhiteSpace(rawVersion)) {
			return false;
		}

		var normalized = rawVersion.Trim();
		if (normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase)) {
			normalized = normalized[1..];
		}

		var metadataSeparatorIndex = normalized.IndexOf('+');
		if (metadataSeparatorIndex >= 0) {
			normalized = normalized[..metadataSeparatorIndex];
		}

		var prereleaseSeparatorIndex = normalized.IndexOf('-');
		if (prereleaseSeparatorIndex >= 0) {
			isPrerelease = true;
			normalized = normalized[..prereleaseSeparatorIndex];
		}

		if (ContainsPrereleaseLabel(rawVersion ?? string.Empty)) {
			isPrerelease = true;
		}

		if (!Version.TryParse(normalized, out Version? parsedVersion) || parsedVersion is null) {
			return false;
		}

		version = parsedVersion;
		return true;
	}

	private static bool ContainsPrereleaseLabel(string value) {
		return value.Contains("alpha", StringComparison.OrdinalIgnoreCase)
			|| value.Contains("beta", StringComparison.OrdinalIgnoreCase)
			|| value.Contains("rc", StringComparison.OrdinalIgnoreCase)
			|| value.Contains("pre", StringComparison.OrdinalIgnoreCase);
	}

	private static string BuildCombinedChangelog(IEnumerable<ConverterReleaseInfo> releases) {
		var stringBuilder = new StringBuilder();
		foreach (var release in releases) {
			if (stringBuilder.Length > 0) {
				stringBuilder.AppendLine();
				stringBuilder.AppendLine();
			}

			stringBuilder.Append("Version ");
			stringBuilder.AppendLine(GetDisplayVersion(release));

			var body = release.Body?.Trim();
			if (string.IsNullOrWhiteSpace(body)) {
				stringBuilder.AppendLine("No changelog provided.");
				continue;
			}

			stringBuilder.AppendLine(body);
		}

		return stringBuilder.ToString();
	}

	private static string GetDisplayVersion(ConverterReleaseInfo release) {
		if (!string.IsNullOrWhiteSpace(release.TagName)) {
			return release.TagName;
		}

		return release.Name ?? string.Empty;
	}

	private static string NormalizeVersion(Version version) {
		return version.Build >= 0 ? version.ToString(3) : version.ToString(2);
	}

	private static void DetermineReleaseBuildUrl(ConverterReleaseInfo releaseInfo, UpdateInfoModel info, string osName, string architecture) {
		var assets = releaseInfo.Assets;
		foreach (var asset in assets) {
			string? assetName = asset.Name;

			if (assetName is null) {
				continue;
			}

			assetName = assetName.ToLower();
			var extension = CommonFunctions.GetExtension(assetName);
			if (extension is not "zip" and not "tgz" and not "exe") {
				continue;
			}

			// For Windows, prefer an installer over an archive.
			if (extension.Equals("exe") && osName.Equals("win")) {
				info.AssetUrl = asset.BrowserDownloadUrl;
				break;
			}

			var assetNameWithoutExtension = CommonFunctions.TrimExtension(assetName);
			if (!assetNameWithoutExtension.EndsWith($"-{osName}-{architecture}", StringComparison.OrdinalIgnoreCase)) {
				continue;
			}

			info.AssetUrl = asset.BrowserDownloadUrl;
			break;
		}
	}

	public static string GetUpdateMessageBody(string baseBody, UpdateInfoModel updateInfo) {
		var stringBuilder = new StringBuilder(baseBody);
		stringBuilder.AppendLine();

		var version = updateInfo.Version;
		if (version is not null) {
			stringBuilder.AppendLine();
			stringBuilder.Append("Version: ");
			stringBuilder.AppendLine(version);
		}

		var description = updateInfo.Description;
		if (description is not null) {
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(description);
		}

		return stringBuilder.ToString();
	}

	private static async Task DownloadFileAsync(string installerUrl, string fileName) {
		var responseBytes = await SharedHttpClient.GetByteArrayAsync(installerUrl);
		await File.WriteAllBytesAsync(fileName, responseBytes);
	}

	public static async Task RunInstallerAndDie(string installerUrl, Config config, INotificationMessageManager notificationManager) {
		Logger.Debug("Downloading installer...");
		var downloadingMessage = notificationManager.CreateMessage()
			.Accent(Brushes.Gray)
			.Background(Brushes.Gray)
			.HasMessage("Downloading installer...")
			.WithOverlay(new ProgressBar {
				VerticalAlignment = VerticalAlignment.Bottom,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				Height = 3,
				BorderThickness = new Thickness(0),
				Foreground = Brushes.Green,
				Background = Brushes.Gray,
				IsIndeterminate = true,
				IsHitTestVisible = false
			})
			.Queue();

		var fileName = Path.GetTempFileName();
		try {
			await DownloadFileAsync(installerUrl, fileName);
		} catch (Exception ex) {
			Logger.Debug($"Failed to download installer: {ex.Message}");
			notificationManager
				.CreateError()
				.HasMessage("Failed to download installer, probably because of network issues. \n" +
				            "Try updating the converter manually.")
				.SuggestManualUpdate(config)
				.Queue();
			return;
		}

		notificationManager.Dismiss(downloadingMessage);

		Logger.Debug("Running installer...");
		var proc = new Process();
		proc.StartInfo.FileName = fileName;
		try {
			proc.Start();
		} catch (Exception ex) {
			Logger.Debug($"Installer process failed to start: {ex.Message}");
			notificationManager
				.CreateError()
				.HasMessage("Failed to start installer, probably because of an antivirus. \n" +
				            "Try updating the converter manually.")
				.SuggestManualUpdate(config)
				.Queue();
			return;
		}

		// Die. The installer will handle the rest.
		MainWindow.Instance.Close();
	}

	public static void StartUpdaterAndDie(string archiveUrl, string converterBackendDirName) {
		var updaterDirPath = Path.Combine(".", "Updater");
		var updaterRunningDirPath = Path.Combine(".", "Updater-running");

		const string manualUpdateHint = "Try updating the converter manually.";
		if (Directory.Exists(updaterRunningDirPath) && !FileSystemHelper.TryDeleteFolder(updaterRunningDirPath)) {
			Logger.Warn($"Failed to delete Updater-running folder! {manualUpdateHint}");
			return;
		}

		if (!SystemUtils.TryCopyFolder(updaterDirPath, updaterRunningDirPath)) {
			Logger.Warn($"Failed to create Updater-running folder! {manualUpdateHint}");
			return;
		}

		string updaterRunningPath = Path.Combine(updaterRunningDirPath, "updater");
		if (OperatingSystem.IsWindows()) {
			updaterRunningPath += ".exe";
		}

		var proc = new Process();
		proc.StartInfo.FileName = updaterRunningPath;
		proc.StartInfo.Arguments = $"{archiveUrl} {converterBackendDirName}";
		try {
			proc.Start();
		} catch (Exception ex) {
			Logger.Debug($"Updater process failed to start: {ex.Message}");
			Logger.Error($"Failed to start updater, probably because of an antivirus. {manualUpdateHint}");
			return;
		}

		// Die. The updater will start Fronter after a successful update.
		MainWindow.Instance.Close();
	}
}