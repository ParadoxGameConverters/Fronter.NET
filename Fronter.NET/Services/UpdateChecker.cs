using commonItems;
using Fronter.Models;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Fronter.Services;

public class UpdateChecker {
	public bool IsUpdateAvailable(string commitIdFilePath, string commitIdUrl) {
		if (!File.Exists(commitIdFilePath)) {
			return false;
		}

		var webClient = new HttpClient();
		var task = Task.Run(() => webClient.GetAsync(commitIdUrl));
		task.Wait();
		var response = task.Result;
		if (!response.IsSuccessStatusCode) {
			return false;
		}
		var readContentTask = Task.Run(() => response.Content.ReadAsStringAsync());
		readContentTask.Wait();
		var latestReleaseCommitId = readContentTask.Result;
		latestReleaseCommitId = latestReleaseCommitId.Trim();

		using var commitIdFileReader = new StreamReader(commitIdFilePath);
		var bufferedReader = new BufferedReader(commitIdFileReader);
		var localCommitId = bufferedReader.GetString();

		return localCommitId != latestReleaseCommitId;
	}

	public UpdateInfoModel GetLatestReleaseInfo(string converterName) {
		var info = new UpdateInfoModel();
		var apiUrl = $"https://api.github.com/repos/ParadoxGameConverters/{converterName}/releases/latest";

		string osName;
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
			osName = "win";
		} else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
			osName = "linux";
		} else if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
			osName = "osx";
		} else {
			return new UpdateInfoModel();
		}

		var httpClient = new HttpClient();
		var getJsonTask = Task.Run(() => httpClient.GetStringAsync(apiUrl));
		getJsonTask.Wait();

		var jsonObject = JsonConvert.DeserializeObject(getJsonTask.Result);
		if (jsonObject is null) {
			return info;
		}

		dynamic dynJsonObject = jsonObject;
		info.Description = dynJsonObject["body"];
		info.Version = dynJsonObject["name"];
		foreach (var asset in dynJsonObject["assets"]) {
			string assetName = asset.Name;

			assetName = assetName.ToLower();
			if (!assetName.EndsWith($"-{osName}-x64.zip")) {
				continue;
			}

			info.ZipUrl = asset["browser_download_url"];
			break;
		}

		if (info.ZipUrl is null) {
			Logger.Warn($"Release {info.Version} doesn't have a .zip asset.");
		}

		return info;
	}

	public void StartUpdaterAndDie(string zipUrl, string converterBackendDirName) {
		string destUpdaterPath = Path.Combine(".", "Updater", "updater-running");
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
			destUpdaterPath = $"{destUpdaterPath}.exe";
			File.Move(
				Path.Combine(".", "Updater", "updater.exe"),
				destUpdaterPath
			);
		} else {
			File.Move(
				Path.Combine(".", "Updater", "updater"),
				destUpdaterPath
			);
		}

		Process.Start(destUpdaterPath, $"{zipUrl} {converterBackendDirName}");
		// Die. The updater will start Fronter after a successful update.
		System.Environment.Exit(0);
	}
}