using commonItems;
using Fronter.Models.Configuration;
using Microsoft.CodeAnalysis;
using System;
using System.Diagnostics;
using System.IO;

namespace Fronter.Services;

internal class ConverterLauncher {
	internal ConverterLauncher(Configuration config) {
		this.config = config;
	}
	public void LaunchConverter() {
		var converterFolder = config.ConverterFolder;
		var backendExePath = config.BackendExePath;
		var backendExePathRelativeToFrontend = Path.Combine(converterFolder, backendExePath);

		var extension = CommonFunctions.GetExtension(backendExePathRelativeToFrontend);
		if (string.IsNullOrEmpty(extension) && OperatingSystem.IsWindows()) {
			backendExePathRelativeToFrontend += ".exe";
		}

		if (string.IsNullOrEmpty(backendExePath)) {
			Logger.Error("Converter location has not been set!");
			return;
		}

		if (!File.Exists(backendExePathRelativeToFrontend)) {
			Logger.Error("Could not find converter executable!");
			return;
		}


		var backendExeName = CommonFunctions.TrimPath(backendExePath);
		var workDir = CommonFunctions.GetPath(backendExePath);

		using Process process = new();
		string currentDir = Directory.GetCurrentDirectory();
		string executablePath = backendExePathRelativeToFrontend;

		process.StartInfo.WorkingDirectory = workDir;
		process.StartInfo.UseShellExecute = false;
		process.StartInfo.FileName = backendExeName;
		process.StartInfo.CreateNoWindow = true;

		var timer = new Stopwatch();
		timer.Start();
		process.Start();
		process.WaitForExit();
		timer.Stop();

		if (process.ExitCode == 0) {
			Logger.Info($"Converter exited at {timer.Elapsed.TotalSeconds} seconds.");
		} else {
			Logger.Error("Converter Error! See log.txt for details.");
			Logger.Error("If you require assistance please upload log.txt to forums for a detailed post-mortem.");
		}
	}

	private readonly Configuration config;
}