using commonItems;
using Fronter.Models.Configuration;
using log4net;
using log4net.Core;
using System;
using System.Diagnostics;
using System.IO;

namespace Fronter.Services;

internal class ConverterLauncher {
	private static readonly ILog Logger = LogManager.GetLogger("Converter Launcher");
	private Level? lastLevelFromBackend = null;
	internal ConverterLauncher(Configuration config) {
		this.config = config;
	}
	public bool LaunchConverter() {
		var converterFolder = config.ConverterFolder;
		var backendExePath = config.BackendExePath;

		if (string.IsNullOrEmpty(backendExePath)) {
			Logger.Error("Converter location has not been set!");
			return false;
		}

		var extension = CommonFunctions.GetExtension(backendExePath);
		if (string.IsNullOrEmpty(extension) && OperatingSystem.IsWindows()) {
			backendExePath += ".exe";
		}
		var backendExePathRelativeToFrontend = Path.Combine(converterFolder, backendExePath);

		if (!File.Exists(backendExePathRelativeToFrontend)) {
			Logger.Error("Could not find converter executable!");
			return false;
		}

		var startInfo = new ProcessStartInfo {
			FileName = backendExePathRelativeToFrontend,
			WorkingDirectory = CommonFunctions.GetPath(backendExePathRelativeToFrontend),
			CreateNoWindow = true,
			UseShellExecute = false,
			RedirectStandardOutput = true,
		};
		using Process process = new() { StartInfo = startInfo };
		process.OutputDataReceived += (sender, args) => {
			var logLine = MessageSlicer.SliceMessage(args.Data ?? string.Empty);
			var level = logLine.Level;
			if (level is null && string.IsNullOrEmpty(logLine.Message)) {
				return;
			}
			Logger.Log(level ?? lastLevelFromBackend ?? Level.Info, logLine.Message);
			if (level is not null) {
				lastLevelFromBackend = level;
			}
		};

		var timer = new Stopwatch();
		timer.Start();

		process.Start();
		process.BeginOutputReadLine();
		process.WaitForExit();

		timer.Stop();

		if (process.ExitCode == 0) {
			Logger.Info($"Converter exited at {timer.Elapsed.TotalSeconds} seconds.");
			return true;
		}

		Logger.Error("Converter Error! See log.txt for details.");
		Logger.Error("If you require assistance please upload log.txt to forums for a detailed post-mortem.");
		return false;
	}
	private readonly Configuration config;
}