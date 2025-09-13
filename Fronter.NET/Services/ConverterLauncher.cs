using Amazon.S3;
using Amazon.S3.Transfer;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using commonItems;
using Fronter.Extensions;
using Fronter.Models.Configuration;
using Fronter.Views;
using log4net;
using log4net.Core;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace Fronter.Services;

internal sealed class ConverterLauncher {
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
		if (string.Equals(extension, "jar", StringComparison.OrdinalIgnoreCase)) {
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

			// Get level to display.
			var logLevel = level ?? lastLevelFromBackend ?? Level.Info;

			logger.LogWithCustomTimestamp(logLine.Timestamp, logLevel, logLine.Message);

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
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
			var processId = process.Id;
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

		if (process.ExitCode == 1) {
			// Exit code 1 is for user errors, so we don't need to send it to Sentry.
			logger.Error($"Converter failed and exited at {timer.Elapsed.TotalSeconds} seconds.");
			return false;
		}

		if (process.ExitCode == -532462766) {
			logger.Error("Converter exited with code -532462766. This is a most likely an antivirus issue.");
			logger.Notice("Please add the converter to your antivirus' whitelist.");
		} else {
			logger.Debug($"Converter exit code: {process.ExitCode}");
			logger.Error("Converter error! See log.txt for details.");
		}

		var helpPageOpened = await TryOpenHelpPage(process.ExitCode);

		if (!helpPageOpened && config.SentryDsn is not null) {
			var saveUploadConsent = await Dispatcher.UIThread.InvokeAsync(GetSaveUploadConsent);
			if (!saveUploadConsent) {
				return false;
			}

			var sentryHelper = new SentryHelper(config);
			try {
				await AttachLogAndSaveToSentry(config, sentryHelper);
			} catch (Exception e) {
				var warnMessage = $"Failed to attach log and save to Sentry event: {e.Message}";
				logger.Warn(warnMessage);
				sentryHelper.AddBreadcrumb(warnMessage);
			}

			try {
				sentryHelper.SendMessageToSentry(process.ExitCode);
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
			title: TranslationSource.Instance.Translate("SAVE_UPLOAD_CONSENT_TITLE"),
			text: TranslationSource.Instance.Translate("SAVE_UPLOAD_CONSENT_BODY"),
			ButtonEnum.OkCancel,
			Icon.Question
		).ShowWindowDialogAsync(MainWindow.Instance);
		return saveUploadConsent == ButtonResult.Ok;
	}

	private static async Task AttachLogAndSaveToSentry(Config config, SentryHelper sentryHelper) {
		sentryHelper.AddAttachment("log.txt");

		var saveLocation = config.RequiredFiles.FirstOrDefault(f => f.Name.Equals("SaveGame"))?.Value;
		if (saveLocation is null) {
			return;
		}

		Directory.CreateDirectory("temp");

		// Create zip with save file.
		var dateTimeString = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
		var asciiSaveName = CommonFunctions.TrimExtension(Path.GetFileName(saveLocation)).FoldToASCII();
		var archivePath = $"temp/SaveGame_{dateTimeString}_{asciiSaveName}.zip";
		using (var zip = ZipFile.Open(archivePath, ZipArchiveMode.Create)) {
			zip.CreateEntryFromFile(saveLocation, new FileInfo(saveLocation).Name);
		}

		// Sentry allows up to 20 MB per compressed request.
		// So we need to calculate whether we can fit the save archive.
		// Otherwise we upload it to Backblaze.
		var logSize = new FileInfo("log.txt").Length; // Size in bytes.
		const int spaceForBaseRequest = 1024 * 1024 / 2; // 0.5 MB, arbitrary.
		var saveSizeLimitForSentry = (20 * 1024 * 1024) - (logSize + spaceForBaseRequest);
		var saveArchiveSize = new FileInfo(archivePath).Length;
		if (saveArchiveSize <= saveSizeLimitForSentry) {
			logger.Debug($"Save file is {saveArchiveSize} bytes, uploading to Sentry.");
			sentryHelper.AddAttachment(archivePath);
		} else {
			logger.Debug($"Save file is {saveArchiveSize} bytes, uploading to Backblaze.");
			await UploadSaveArchiveToBackblaze(archivePath, sentryHelper);
		}
	}

	private static async Task UploadSaveArchiveToBackblaze(string archivePath, SentryHelper sentryHelper) {
		// Add Backblaze credentials to breadcrumbs for debugging.
		var keyId = Secrets.BackblazeKeyId;
		var applicationKey = Secrets.BackblazeApplicationKey;
		var bucketId = Secrets.BackblazeBucketId;
		sentryHelper.AddBreadcrumb($"Backblaze key ID: \"{keyId}\"");
		sentryHelper.AddBreadcrumb($"Backblaze application key: \"{applicationKey}\"");
		sentryHelper.AddBreadcrumb($"Backblaze bucket ID: \"{bucketId}\"");
		sentryHelper.AddBreadcrumb($"Archive name: {Path.GetFileName(archivePath)}");

		var s3Config = new AmazonS3Config {
			ServiceURL = "https://s3.eu-central-003.backblazeb2.com",
		};

		var s3Client = new AmazonS3Client(keyId, applicationKey, s3Config);
		var fileTransferUtility = new TransferUtility(s3Client);

		try {
			await fileTransferUtility.UploadAsync(archivePath, "save-zips");
			Logger.Info("Upload completed.");
		}
		catch (AmazonS3Exception e) {
			string message = $"Error encountered on server. Message:'{e.Message}' when writing an object.";
			Logger.Error(message);
			sentryHelper.AddBreadcrumb(message);
		}
		catch (Exception e) {
			string message = $"Unknown encountered on server. Message:'{e.Message}' when writing an object.";
			Logger.Error(message);
			sentryHelper.AddBreadcrumb(message);
		}
	}

	/// <summary>
	/// Tries to open a help page based on the converter backend exit code.
	/// </summary>
	/// <param name="exitCode">Exit code of the converter backend.</param>
	/// <returns>true if a help page was opened, otherwise false</returns>
	private static async Task<bool> TryOpenHelpPage(int exitCode) {
		if (OperatingSystem.IsWindows()) {
			var exitCodeToHelpDict = new Dictionary<int, string> {
				{-1073741790, "https://answers.microsoft.com/en-us/windows/forum/all/the-application-was-unable-to-start-correctly/e06ee08a-26c5-447a-80bd-ed339488d0f3"}, // -1073741790 = 0xC0000022
				{-1073741795, "https://ugetfix.com/ask/how-to-fix-file-system-error-1073741795-in-windows/"},
				{-532462766, "https://www.thewindowsclub.com/add-file-or-folder-to-antivirus-exception-list-in-windows"},
			};
			if (!exitCodeToHelpDict.TryGetValue(exitCode, out var helpLink)) {
				return false;
			}

			var msgBoxResult = await Dispatcher.UIThread.InvokeAsync(() => MessageBoxManager.GetMessageBoxStandard(
				title: "Fix suggestion",
				text: "Would you like to open a help page with instructions on how to fix this issue?",
				ButtonEnum.YesNo,
				Icon.Info
			).ShowWindowDialogAsync(MainWindow.Instance));

			if (msgBoxResult == ButtonResult.Yes) {
				BrowserLauncher.Open(helpLink);
				return true;
			}
			Logger.Debug("User declined to open help page.");
		}

		return false;
	}

	private readonly Config config;
}