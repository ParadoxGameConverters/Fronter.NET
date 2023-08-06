using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Bytewizer.Backblaze.Client;
using commonItems;
using Fronter.Extensions;
using Fronter.LogAppenders;
using Fronter.Models;
using Fronter.Models.Configuration;
using Fronter.Views;
using log4net;
using log4net.Core;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Sentry;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fronter.Services;

internal class ConverterLauncher {
	private static readonly ILog logger = LogManager.GetLogger("Converter launcher");
	private Level? lastLevelFromBackend;
	internal ConverterLauncher(Config config) {
		this.config = config;
	}

	private string? GetBackendExePathRelativeToFrontend() {
		var converterFolder = config.ConverterFolder;
		var backendExePath = config.BackendExePath;

		if (string.IsNullOrEmpty(backendExePath)) {
			logger.Error("Converter location has not been set!");
			return null;
		}

		var extension = CommonFunctions.GetExtension(backendExePath);
		if (string.IsNullOrEmpty(extension) && OperatingSystem.IsWindows()) {
			backendExePath += ".exe";
		}
		var backendExePathRelativeToFrontend = Path.Combine(converterFolder, backendExePath);

		return backendExePathRelativeToFrontend;
	}

	public async Task<bool> LaunchConverter() {
		var backendExePathRelativeToFrontend = GetBackendExePathRelativeToFrontend();
		if (backendExePathRelativeToFrontend is null) {
			return false;
		}

		if (!File.Exists(backendExePathRelativeToFrontend)) {
			logger.Error("Could not find converter executable!");
			return false;
		}

		logger.Debug($"Using {backendExePathRelativeToFrontend} as converter backend...");
		var startInfo = new ProcessStartInfo {
			FileName = backendExePathRelativeToFrontend,
			WorkingDirectory = CommonFunctions.GetPath(backendExePathRelativeToFrontend),
			CreateNoWindow = true,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardInput = true,
		};
		var extension = CommonFunctions.GetExtension(backendExePathRelativeToFrontend);
		if (extension == "jar") {
			startInfo.FileName = "javaw";
			startInfo.Arguments = $"-jar {CommonFunctions.TrimPath(backendExePathRelativeToFrontend)}";
		}

		using Process process = new();
		process.StartInfo = startInfo;
		process.OutputDataReceived += (sender, args) => {
			var logLine = MessageSlicer.SliceMessage(args.Data ?? string.Empty);
			var level = logLine.Level;
			if (level is null && string.IsNullOrEmpty(logLine.Message)) {
				return;
			}

			// Get timestamp datetime.
			DateTime timestamp = logLine.TimestampAsDateTime;

			// Get level to display.
			var logLevel = level ?? lastLevelFromBackend ?? Level.Info;

			logger.LogWithCustomTimestamp(timestamp, logLevel, logLine.Message);

			if (level is not null) {
				lastLevelFromBackend = level;
			}
		};

		var timer = new Stopwatch();
		timer.Start();

		process.Start();
		process.EnableRaisingEvents = true;
		process.PriorityClass = ProcessPriorityClass.RealTime;
		process.PriorityBoostEnabled = OperatingSystem.IsWindows();

		process.BeginOutputReadLine();

		// Kill converter backend when frontend is closed.
		var processId = process.Id;
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
			desktop.ShutdownRequested += (sender, args) => {
				try {
					var backendProcess = Process.GetProcessById(processId);
					logger.Info("Killing converter backend...");
					backendProcess.Kill();
				} catch (ArgumentException) {
					// Process already exited.
				}
			};
		}

		await process.WaitForExitAsync();
		timer.Stop();
		
		if (process.ExitCode == 0) {
			logger.Info($"Converter exited at {timer.Elapsed.TotalSeconds} seconds.");
			return true;
		}
		
		logger.Debug($"Converter exit code: {process.ExitCode}");
		logger.Error("Converter error! See log.txt for details.");
		if (SentrySdk.IsEnabled) {
			bool logProvided = false;
			var saveUploadConsent = await Dispatcher.UIThread.InvokeAsync(GetSaveUploadConsent);
			if (saveUploadConsent) {
				try {
					AttachLogAndSaveToSentry(config);
					logProvided = true;
				} catch (Exception e) {
					var warnMessage = $"Failed to attach log and save to Sentry event: {e.Message}";
					logger.Warn(warnMessage);
					SentrySdk.AddBreadcrumb(warnMessage);
				}
			} 
			SentrySdk.ConfigureScope(scope => {
				scope.SetTag("logProvided", logProvided.ToString());
			});
			
			try {
				SendMessageToSentry(process.ExitCode);
				if (saveUploadConsent) {
					Logger.Notice("Uploaded information about the error, thank you!");
				}
			} catch (Exception e) {
				logger.Warn($"Failed to send message to Sentry: {e.Message}");
			}
		} else {
			logger.Error("If you require assistance, please visit the converter's forum thread " +
			             "for a detailed postmortem.");
		}
		return false;
	}
	
	private static async Task<bool> GetSaveUploadConsent() {
		var saveUploadConsent = await MessageBoxManager.GetMessageBoxStandard(
			title: "Save upload consent",
			text: "Would you like the application to automatically upload your save file to our error database, " +
			      "in order to help us fix this issue?",
			ButtonEnum.OkCancel,
			Icon.Question
		).ShowWindowDialogAsync(MainWindow.Instance);
		return saveUploadConsent == ButtonResult.Ok;
	}

	private static async void AttachLogAndSaveToSentry(Config config) {
		SentrySdk.ConfigureScope(scope => scope.AddAttachment("log.txt"));
		
		var saveLocation = config.RequiredFiles.FirstOrDefault(f => f.Name == "SaveGame")?.Value;
		if (saveLocation is null) {
			return;
		}

		Directory.CreateDirectory("temp");
		
		// Create zip with save file.
		var dateTimeString = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
		var archivePath = $"temp/SaveGame_{dateTimeString}.zip";
		using (var zip = ZipFile.Open(archivePath, ZipArchiveMode.Create)) {
			zip.CreateEntryFromFile(saveLocation, new FileInfo(saveLocation).Name);
		}
		
		// Sentry allows up to 20 MB per compressed request.
		// So we need to calculate whether we can fit the save archive.
		// Otherwise we upload it to Backblaze.
		var logSize = new FileInfo("log.txt").Length; // Size in bytes.
		const int spaceForBaseRequest = 1024 * 1024 / 2; // 0.5 MB, arbitrary.
		var saveSizeLimitForSentry = 20 * 1024 * 1024 - (logSize + spaceForBaseRequest);
		var saveArchiveSize = new FileInfo(archivePath).Length;
		if (saveArchiveSize <= saveSizeLimitForSentry) {
			logger.Debug($"Save file is {saveArchiveSize} bytes, uploading to Sentry.");
			SentrySdk.ConfigureScope(scope => { scope.AddAttachment(archivePath); });
		} else {
			logger.Debug($"Save file is {saveArchiveSize} bytes, uploading to Backblaze.");
			await UploadSaveArchiveToBackblaze(archivePath);
		}
	}

	private static async Task<IPAddress?> GetExternalIpAddress() {
		try {
			var externalIpString = (await new HttpClient().GetStringAsync("https://icanhazip.com/"))
				.Replace(@"\r", "")
				.Replace(@"\n", "")
				.Trim();
			return !IPAddress.TryParse(externalIpString, out var ipAddress) ? null : ipAddress;
		} catch (Exception e) {
			SentrySdk.AddBreadcrumb($"Failed to get IP address: {e.Message}");
			return null;
		}
	}

	private static LogLine? GetFirstErrorLogLineFromGrid() {
		var gridAppender = LogManager.GetRepository().GetAppenders().First(a => a.Name == "grid");
		if (gridAppender is LogGridAppender logGridAppender) {
			return logGridAppender.LogLines
				.FirstOrDefault(l => l.Level is not null && l.Level >= Level.Error);
		}
		return null;
	}

	private static async void SendMessageToSentry(int processExitCode) {
		// Identify user by username or IP address.
		var ip = (await GetExternalIpAddress())?.ToString();
		SentrySdk.ConfigureScope(scope => {
			scope.User = ip is null ? new User {Username = Environment.UserName} : new User {IpAddress = ip};
		});

		var error = GetFirstErrorLogLineFromGrid();
		if (error is not null) {
			var sentryMessageLevel = error.Level == Level.Fatal ? SentryLevel.Fatal : SentryLevel.Error;
			SentrySdk.CaptureMessage(error.Message, sentryMessageLevel);
		} else {
			var message = $"Converter exited with code {processExitCode}";
			SentrySdk.CaptureMessage(message, SentryLevel.Error);
		}
	}

	private static async Task UploadSaveArchiveToBackblaze(string archivePath) {
		// Add Backblaze credentials to breadcrumbs for debugging.
		var client = new BackblazeClient();
		var keyId = Secrets.BackblazeKeyId;
		var applicationKey = Secrets.BackblazeApplicationKey;
		var bucketId = Secrets.BackblazeBucketId;
		SentrySdk.AddBreadcrumb($"Backblaze key ID: \"{keyId}\"");
		SentrySdk.AddBreadcrumb($"Backblaze application key: \"{applicationKey}\"");
		SentrySdk.AddBreadcrumb($"Backblaze bucket ID: \"{bucketId}\"");

		// Init Backblaze B2 client.
		try {
			await client.ConnectAsync(keyId, applicationKey);
		} catch (Exception e) {
			var message = $"Failed to connect to Backblaze: {e.Message}";
			logger.Debug(message);
			SentrySdk.AddBreadcrumb(message);
			return;
		}
			
		// Upload zip to Backblaze B2.
		try {
			await using var stream = File.OpenRead(archivePath);
			var archiveName = new FileInfo(archivePath).Name;
			var results = await client.UploadAsync(bucketId, archiveName, stream);
			if (results.IsSuccessStatusCode) {
				logger.Debug("Uploaded save file to Backblaze.");
				var backblazeFileName = results.Response.FileName;
				var backblazeFileId = results.Response.FileId;
				SentrySdk.AddBreadcrumb($"Backblaze file name: {backblazeFileName}; file ID: {backblazeFileId}");
			} else {
				logger.Debug($"Save archive upload failed with status {results.StatusCode}");
			}
		} catch (Exception e) {
			var message = $"Failed to upload save file to Backblaze: {e.Message}";
			logger.Debug(message);
			SentrySdk.AddBreadcrumb(message);
		}
	}
	
	private readonly Config config;
}