using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using commonItems;
using Fronter.Extensions;
using Fronter.Models.Configuration;
using Fronter.ViewModels;
using log4net;
using log4net.Core;
using System;
using System.Diagnostics;
using System.IO;

namespace Fronter.Services;

internal class ConverterLauncher {
	private static readonly ILog logger = LogManager.GetLogger("Converter launcher");
	private Level? lastLevelFromBackend;
	internal ConverterLauncher(Configuration config) {
		this.config = config;
	}
	public bool LaunchConverter() {
		var converterFolder = config.ConverterFolder;
		var backendExePath = config.BackendExePath;

		if (string.IsNullOrEmpty(backendExePath)) {
			logger.Error("Converter location has not been set!");
			return false;
		}

		var extension = CommonFunctions.GetExtension(backendExePath);
		if (string.IsNullOrEmpty(extension) && OperatingSystem.IsWindows()) {
			backendExePath += ".exe";
		}
		var backendExePathRelativeToFrontend = Path.Combine(converterFolder, backendExePath);

		if (!File.Exists(backendExePathRelativeToFrontend)) {
			logger.Error("Could not find converter executable!");
			return false;
		}

		var startInfo = new ProcessStartInfo {
			FileName = backendExePathRelativeToFrontend,
			WorkingDirectory = CommonFunctions.GetPath(backendExePathRelativeToFrontend),
			CreateNoWindow = true,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			RedirectStandardInput = true,
		};
		using Process process = new() { StartInfo = startInfo };
		process.OutputDataReceived += (sender, args) => {
			var logLine = MessageSlicer.SliceMessage(args.Data ?? string.Empty);
			var level = logLine.Level;
			if (level is null && string.IsNullOrEmpty(logLine.Message)) {
				return;
			}

			// Get timestamp datetime.
			DateTime timestamp;
			if (string.IsNullOrWhiteSpace(logLine.Timestamp)) {
				timestamp = DateTime.Now;
			} else {
				timestamp = Convert.ToDateTime(logLine.Timestamp);
			}
			
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
		
		process.WaitForExit();
		timer.Stop();

		if (process.ExitCode == 0) {
			logger.Info($"Converter exited at {timer.Elapsed.TotalSeconds} seconds.");
			return true;
		}

		logger.Error("Converter Error! See log.txt for details.");
		logger.Error("If you require assistance please upload log.txt to forums for a detailed postmortem.");
		return false;
	}
	private readonly Configuration config;
}