using commonItems;
using Fronter.Models;
using Fronter.Views;
using log4net;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Fronter.Services;

public static class UpdateChecker {
	private static readonly ILog Logger = LogManager.GetLogger("Update checker");
	private static readonly HttpClient HttpClient = new();
	public static async Task<bool> IsUpdateAvailable(string commitIdFilePath, string commitIdUrl) {
		if (!File.Exists(commitIdFilePath)) {
			Logger.Warn($"File \"{commitIdFilePath}\" does not exist!");
			return false;
		}

		try {
			var response = await HttpClient.GetAsync(commitIdUrl);
			if (!response.IsSuccessStatusCode) {
				Logger.Warn($"Failed to get commit id from \"{commitIdUrl}\"; status code: {response.StatusCode}!");
				return false;
			}

			var latestReleaseCommitId = await response.Content.ReadAsStringAsync();
			latestReleaseCommitId = latestReleaseCommitId.Trim();

			using var commitIdFileReader = new StreamReader(commitIdFilePath);
			var localCommitId = (await commitIdFileReader.ReadLineAsync())?.Trim();

			return localCommitId is not null && localCommitId != latestReleaseCommitId;
		} catch (Exception e) {
			Logger.Warn($"Failed to get commit id from \"{commitIdUrl}\"; {e}!");
			return false;
		}
	}

	private static (string, string)? GetOSNameAndArch() {
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
			return ("win", "x64");
		}
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
			return ("linux", "x64");
		}
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
			return ("osx", "arm64");
		}
		return null;
	}

	public static async Task<UpdateInfoModel> GetLatestReleaseInfo(string converterName) {
		var osNameAndArch = GetOSNameAndArch();
		if (osNameAndArch is null) {
			return new UpdateInfoModel();
		}

		var osName = osNameAndArch.Value.Item1;
		var architecture = osNameAndArch.Value.Item2;

		var info = new UpdateInfoModel();
		var apiUrl = $"https://api.github.com/repos/ParadoxGameConverters/{converterName}/releases/latest";
		var requestMessage = new HttpRequestMessage(HttpMethod.Get, apiUrl);
		requestMessage.Headers.Add("User-Agent", "ParadoxGameConverters");

		var responseMessage = await HttpClient.SendAsync(requestMessage);
		await using var responseStream = await responseMessage.Content.ReadAsStreamAsync();

		var releaseInfo = await JsonSerializer.DeserializeAsync<ConverterReleaseInfo>(responseStream);
		if (releaseInfo is null) {
			return info;
		}

		info.Description = releaseInfo.Body;
		info.Version = releaseInfo.Name;

		var assets = releaseInfo.Assets;
		foreach (var asset in assets) {
			string? assetName = asset.Name;

			if (assetName is null) {
				continue;
			}

			assetName = assetName.ToLower();
			var extension = CommonFunctions.GetExtension(assetName);
			if (extension is not "zip" and not "tgz") {
				continue;
			}

			var assetNameWithoutExtension = CommonFunctions.TrimExtension(assetName);
			if (!assetNameWithoutExtension.EndsWith($"-{osName}-{architecture}")) {
				continue;
			}

			info.ArchiveUrl = asset.BrowserDownloadUrl;
			break;
		}

		if (info.ArchiveUrl is null) {
			Logger.Debug($"Release {info.Version} doesn't have a .zip or .tgz asset for this platform.");
		}

		return info;
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

	public static void StartUpdaterAndDie(string archiveUrl, string converterBackendDirName) {
		var updaterDirPath = Path.Combine(".", "Updater");
		var updaterRunningDirPath = Path.Combine(".", "Updater-running");
		
		const string manualUpdateHint = "Try updating the converter manually.";
		if (Directory.Exists(updaterRunningDirPath) && !SystemUtils.TryDeleteFolder(updaterRunningDirPath)) {
			Logger.Warn($"Failed to delete Updater-running folder! {manualUpdateHint}");
			return;
		}

		if (!SystemUtils.TryCopyFolder(updaterDirPath, updaterRunningDirPath)) {
			Logger.Warn($"Failed to create Updater-running folder! {manualUpdateHint}");
			return;
		}

		string updaterRunningPath = Path.Combine(updaterRunningDirPath, "updater");
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
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