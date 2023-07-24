using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using commonItems;
using Fronter.Extensions;
using Fronter.LogAppenders;
using Fronter.Models.Configuration;
using Ionic.Zip;
using log4net;
using log4net.Core;
using Sentry;
using System;
using System.Diagnostics;
using System.IO;
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

		SendMessageToSentry(config, process.ExitCode);
		logger.Error("Converter error! See log.txt for details.");
		logger.Error("If you require assistance please upload log.txt to forums for a detailed postmortem.");
		logger.Debug($"Converter exit code: {process.ExitCode}");
		return false;
	}

	private static void SendMessageToSentry(Configuration config, int processExitCode) {
		// At this point the save location is not going to change, so it can be added to Sentry.
		var saveLocation = config.RequiredFiles.FirstOrDefault(f => f?.Name == "SaveGame", null)?.Value;
		if (saveLocation is not null) {
			Directory.CreateDirectory("temp");
			using var zip = new ZipFile();
			zip.AddFile(saveLocation);
			zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
			zip.MaxOutputSegmentSize = 20*1000*1000;
			zip.Save("temp/SaveGame.zip");

			var segmentsCreated = zip.NumberOfSegmentsForMostRecentSave;
			SentrySdk.ConfigureScope(scope => {
				scope.AddAttachment("temp/SaveGame.zip");
				for (int i = 1; i < segmentsCreated; ++i) {
					scope.AddAttachment($"temp/SaveGame.z{i:00}");
				}
			});
		}
		
		var gridAppender = LogManager.GetRepository().GetAppenders().First(a => a.Name == "grid");
		if (gridAppender is LogGridAppender logGridAppender) {
			var error = logGridAppender.LogLines
				.LastOrDefault(l => l.Level is not null && l.Level >= Level.Error);
			var sentryMessageLevel = error?.Level == Level.Fatal ? SentryLevel.Fatal : SentryLevel.Error;
			var message = error?.Message ?? $"Converter exited with code {processExitCode}";
			SentrySdk.CaptureMessage(message, sentryMessageLevel);
		} else {
			var message = $"Converter exited with code {processExitCode}";
			SentrySdk.CaptureMessage(message, SentryLevel.Error);
		}
	}
	
	private readonly Configuration config;
}