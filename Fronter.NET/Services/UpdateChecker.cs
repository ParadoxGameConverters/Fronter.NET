using commonItems;
using Fronter.Models;
using Fronter.Views;
using log4net;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Fronter.Services;

public static class UpdateChecker {
	private static readonly ILog logger = LogManager.GetLogger("Update checker");
	private static readonly HttpClient HttpClient = new();
	public static async Task<bool> IsUpdateAvailable(string commitIdFilePath, string commitIdUrl) {
		if (!File.Exists(commitIdFilePath)) {
			logger.Warn($"File \"{commitIdFilePath}\" does not exist!");
			return false;
		}

		try {
			var response = await HttpClient.GetAsync(commitIdUrl);
			if (!response.IsSuccessStatusCode) {
				logger.Warn($"Failed to get commit id from \"{commitIdUrl}\"; status code: {response.StatusCode}!");
				return false;
			}
			
			var latestReleaseCommitId = await response.Content.ReadAsStringAsync();
			latestReleaseCommitId = latestReleaseCommitId.Trim();

			using var commitIdFileReader = new StreamReader(commitIdFilePath);
			var localCommitId = (await commitIdFileReader.ReadLineAsync())?.Trim();

			return localCommitId is not null && localCommitId != latestReleaseCommitId;
		} catch (Exception e) {
			logger.Warn($"Failed to get commit id from \"{commitIdUrl}\"; {e}!");
			return false;
		}
	}

	public static async Task<UpdateInfoModel> GetLatestReleaseInfo(string converterName) {
		var info = new UpdateInfoModel();
		var apiUrl = $"https://api.github.com/repos/ParadoxGameConverters/{converterName}/releases/latest";

		string osName;
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
			osName = "win";
		} else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
			osName = "linux";
		} else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
			osName = "osx";
		} else {
			return new UpdateInfoModel();
		}

		var requestMessage = new HttpRequestMessage(HttpMethod.Get, apiUrl);
		requestMessage.Headers.Add("User-Agent", "ParadoxGameConverters");

		var responseMessage = await HttpClient.SendAsync(requestMessage);
		await using var responseStream = await responseMessage.Content.ReadAsStreamAsync();
		using var responseReader = new StreamReader(responseStream);

		var jsonObject = JsonConvert.DeserializeObject(await responseReader.ReadToEndAsync());
		if (jsonObject is null) {
			return info;
		}

		dynamic dynJsonObject = jsonObject;
		var body = dynJsonObject["body"];
		if (body is not null) {
			info.Description = body;
		}
		var name = dynJsonObject["name"];
		if (name is not null) {
			info.Version = name;
		}

		var assets = dynJsonObject["assets"];
		if (assets is not null) {
			foreach (var asset in assets) {
				string? assetName = asset["name"];
				if (assetName is null) {
					continue;
				}

				assetName = assetName.ToLower();
				var extension = CommonFunctions.GetExtension(assetName);
				if (extension is not "zip" and not "tgz") {
					continue;
				}
			
				var assetNameWithoutExtension = CommonFunctions.TrimExtension(assetName);
				if (!assetNameWithoutExtension.EndsWith($"-{osName}-x64")) {
					continue;
				}

				info.ArchiveUrl = asset["browser_download_url"];
				break;
			}
		}

		if (info.ArchiveUrl is null) {
			logger.Warn($"Release {info.Version} doesn't have a .zip or .tgz asset.");
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

		if (Directory.Exists(updaterRunningDirPath) && !SystemUtils.TryDeleteFolder(updaterRunningDirPath)) {
			return;
		}
		
		if (!SystemUtils.TryCopyFolder(updaterDirPath, updaterRunningDirPath)) {
			return;
		}

		string updaterRunningPath = Path.Combine(updaterRunningDirPath, "updater");
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
			updaterRunningPath += ".exe";
		}

		var proc = new Process();
		proc.StartInfo.FileName = updaterRunningPath;
		proc.StartInfo.Arguments = $"{archiveUrl} {converterBackendDirName}";
		proc.Start();
		
		// Die. The updater will start Fronter after a successful update.
		MainWindow.Instance.Close();
	}
}