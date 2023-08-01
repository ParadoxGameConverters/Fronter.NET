using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Bytewizer.Backblaze.Client;
using commonItems;
using Fronter.Extensions;
using Fronter.LogAppenders;
using Fronter.Models.Configuration;
using Fronter.Views;
using log4net;
using log4net.Core;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Sentry;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace Fronter.Services;

internal class ConverterLauncher {
	private static readonly ILog logger = LogManager.GetLogger("Converter launcher");
	private Level? lastLevelFromBackend;
	internal ConverterLauncher(Configuration config) {
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
			// When Sentry is enabled, every event is reported but log and save are only uploaded if the user consents.
			var saveUploadConsent = await MessageBoxManager.GetMessageBoxStandard(
				title: "Save upload consent",
				text: "Would you like the application to automatically upload your save file to our error database, " +
				      "in order to help us fix this issue?",
				ButtonEnum.OkCancel,
				Icon.Question
				).ShowWindowDialogAsync(MainWindow.Instance);
			bool logProvided = false;
			if (saveUploadConsent == ButtonResult.Ok) {
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
			} catch (Exception e) {
				logger.Warn($"Failed to send message to Sentry: {e.Message}");
			}
		} else {
			logger.Error("If you require assistance please visit the converter's forum thread for a detailed postmortem.");
		}
		return false;
	}

	private static async void AttachLogAndSaveToSentry(Configuration config) {
		SentrySdk.ConfigureScope(scope => scope.AddAttachment("log.txt"));
		
		var saveLocation = config.RequiredFiles.FirstOrDefault(f => f.Name == "SaveGame")?.Value;
		if (saveLocation is null) {
			return;
		}

		Directory.CreateDirectory("temp");
		
		// Create zip with save file.
		var dateTimeString = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
		var archivePath = $"temp/SaveGame_{dateTimeString}.zip";
		using (var zip = ZipFile.Open(archivePath, ZipArchiveMode.Create)) {
			zip.CreateEntryFromFile(saveLocation, new FileInfo(saveLocation).Name);
		}

		var archiveSize = new FileInfo(archivePath).Length; // Size in bytes.
		if (archiveSize <= 19 * 1024 * 1024) {
			// Sentry allows up to 20 MB per compressed request.
			// We leave 1 MB for the rest of the request, including log.txt attachment.
			logger.Debug("Save file is equal or less than 19 MB, uploading to Sentry.");
			SentrySdk.ConfigureScope(scope => { scope.AddAttachment(archivePath); });
		} else {
			logger.Debug("Save file is larger than 19 MB, uploading to Backblaze.");
			await UploadSaveArchiveToBackblaze(archivePath);
		}
	}

	private static void SendMessageToSentry(int processExitCode) {
		var gridAppender = LogManager.GetRepository().GetAppenders().First(a => a.Name == "grid");
		if (gridAppender is LogGridAppender logGridAppender) {
			var error = logGridAppender.LogLines
				.FirstOrDefault(l => l.Level is not null && l.Level >= Level.Error);
			var sentryMessageLevel = error?.Level == Level.Fatal ? SentryLevel.Fatal : SentryLevel.Error;
			var message = error?.Message ?? $"Converter exited with code {processExitCode}";
			SentrySdk.CaptureMessage(message, sentryMessageLevel);
		} else {
			var message = $"Converter exited with code {processExitCode}";
			SentrySdk.CaptureMessage(message, SentryLevel.Error);
		}
	}

	private static async Task UploadSaveArchiveToBackblaze(string archivePath) {
		// Init Backblaze B2 client.
		var client = new BackblazeClient();
		var backblazeKeyId = Environment.GetEnvironmentVariable("BACKBLAZE_KEY_ID");
		var backblazeApplicationKey = Environment.GetEnvironmentVariable("BACKBLAZE_APPLICATION_KEY");
		await client.ConnectAsync(backblazeKeyId, backblazeApplicationKey);
			
		// Upload zip to Backblaze B2.
		await using var stream = File.OpenRead(archivePath);
		var archiveName = new FileInfo(archivePath).Name;
		var backblazeBucketId = Environment.GetEnvironmentVariable("BACKBLAZE_BUCKET_ID");
		var results = await client.UploadAsync(backblazeBucketId, archiveName, stream);
		if (results.IsSuccessStatusCode) {
			logger.Debug("Uploaded save file to Backblaze.");
			var backblazeFileName = results.Response.FileName;
			var backblazeFileId = results.Response.FileId;
			SentrySdk.AddBreadcrumb($"Backblaze file name: {backblazeFileName}; file ID: {backblazeFileId}"); 
		} else {
			logger.Debug($"Save archive upload failed with status {results.StatusCode}");
		}
	}
	
	private readonly Configuration config;
}