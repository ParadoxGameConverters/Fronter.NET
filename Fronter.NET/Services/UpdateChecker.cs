﻿using Avalonia;
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
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Fronter.Services;

internal static class UpdateChecker {
	private static readonly ILog Logger = LogManager.GetLogger("Update checker");
	private static readonly HttpClient HttpClient = new() {Timeout = TimeSpan.FromMinutes(5)};
	public static async Task<bool> IsUpdateAvailable(string commitIdFilePath, string commitIdUrl) {
		if (!File.Exists(commitIdFilePath)) {
			Logger.Debug($"File \"{commitIdFilePath}\" does not exist!");
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

			return localCommitId is not null && !localCommitId.Equals(latestReleaseCommitId);
		} catch (Exception e) {
			Logger.Warn($"Failed to get commit id from \"{commitIdUrl}\"; {e}!");
			return false;
		}
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

		HttpResponseMessage responseMessage;
		try {
			responseMessage = await HttpClient.SendAsync(requestMessage);
		} catch (Exception e) {
			Logger.Warn($"Failed to get release info from \"{apiUrl}\": {e}!");
			return info;
		}
		await using var responseStream = await responseMessage.Content.ReadAsStreamAsync();

		var releaseInfo = await JsonSerializer.DeserializeAsync<ConverterReleaseInfo>(responseStream);
		if (releaseInfo is null) {
			return info;
		}

		info.Description = releaseInfo.Body;
		info.Version = releaseInfo.Name;

		DetermineReleaseBuildUrl(releaseInfo, info, osName, architecture);

		if (info.AssetUrl is null) {
			Logger.Debug($"Release {info.Version} doesn't have a release build for this platform.");
		}

		return info;
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
		var responseBytes = await HttpClient.GetByteArrayAsync(installerUrl);
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
		if (Directory.Exists(updaterRunningDirPath) && !SystemUtils.TryDeleteFolder(updaterRunningDirPath)) {
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