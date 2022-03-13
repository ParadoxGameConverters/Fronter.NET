using commonItems;
using Fronter.Models;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Fronter.Services;

public static class UpdateChecker {
	public static bool IsUpdateAvailable(string commitIdFilePath, string commitIdUrl) {
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

	public static UpdateInfoModel GetLatestReleaseInfo(string converterName) {
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
		var requestMessage = new HttpRequestMessage(HttpMethod.Get, apiUrl);
		requestMessage.Headers.Add("User-Agent", "ParadoxGameConverters");

		var responseMessage = httpClient.Send(requestMessage);
		using var responseStream = responseMessage.Content.ReadAsStream();
		using var responseReader = new StreamReader(responseStream);

		var jsonObject = JsonConvert.DeserializeObject(responseReader.ReadToEnd());
		if (jsonObject is null) {
			return info;
		}

		dynamic dynJsonObject = jsonObject;
		info.Description = dynJsonObject["body"];
		info.Version = dynJsonObject["name"];

		foreach (var asset in dynJsonObject["assets"]) {
			string assetName = asset["name"];

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

	public static string GetUpdateMessageBody(string baseBody, UpdateInfoModel updateInfo) {
		return $"{baseBody}\n\nVersion: {updateInfo.Version}\n\n{StripMarkdownTags(updateInfo.Description)}";
	}

	/// <summary>
	/// Strips Markdown tags from regular text
	/// https://gist.github.com/dennisslimmers/4b63db37e640d74acb29d4e1f24e9acd
	/// </summary>
	/// <param name="content"></param>
	private static string StripMarkdownTags(string content) {
		// Headers
		content = Regex.Replace(content, "/\n={2,}/g", "\n");
		// Strikethrough
		content = Regex.Replace(content, "/~~/g", "");
		// Codeblocks
		content = Regex.Replace(content, "/`{3}.*\n/g", "");
		// HTML Tags
		content = Regex.Replace(content, "/<[^>]*>/g", "");
		// Remove setext-style headers
		content = Regex.Replace(content, "/^[=\\-]{2,}\\s*$/g", "");
		// Footnotes
		content = Regex.Replace(content, "/\\[\\^.+?\\](\\: .*?$)?/g", "");
		content = Regex.Replace(content, "/\\s{0,2}\\[.*?\\]: .*?$/g", "");
		// Images
		content = Regex.Replace(content, "/\\!\\[.*?\\][\\[\\(].*?[\\]\\)]/g", "");
		// Links
		content = Regex.Replace(content, "/\\[(.*?)\\][\\[\\(].*?[\\]\\)]/g", "$1");
		return content;
	}

	public static void StartUpdaterAndDie(string zipUrl, string converterBackendDirName) {
		string destUpdaterPath = Path.Combine(".", "Updater", "updater-running");
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
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