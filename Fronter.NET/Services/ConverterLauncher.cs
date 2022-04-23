using Avalonia.Threading;
using commonItems;
using Fronter.Models.Configuration;
using Fronter.ViewModels;
using System;
using System.Diagnostics;
using System.IO;

namespace Fronter.Services;

internal class ConverterLauncher {
	internal ConverterLauncher(MainWindowViewModel mainWindowViewModel) {
		parent = mainWindowViewModel;
		config = mainWindowViewModel.Config;
	}
	public void LaunchConverter() {
		var converterFolder = config.ConverterFolder;
		var backendExePath = config.BackendExePath;

		if (string.IsNullOrEmpty(backendExePath)) {
			Logger.Error("Converter location has not been set!");
			return;
		}

		var extension = CommonFunctions.GetExtension(backendExePath);
		if (string.IsNullOrEmpty(extension) && OperatingSystem.IsWindows()) {
			backendExePath += ".exe";
		}
		var backendExePathRelativeToFrontend = Path.Combine(converterFolder, backendExePath);

		if (!File.Exists(backendExePathRelativeToFrontend)) {
			Logger.Error("Could not find converter executable!");
			return;
		}

		var startInfo = new ProcessStartInfo() {
			FileName = backendExePathRelativeToFrontend,
			WorkingDirectory = CommonFunctions.GetPath(backendExePathRelativeToFrontend),
			CreateNoWindow = true,
			UseShellExecute = false,
			RedirectStandardOutput = true,
		};
		using Process process = new() { StartInfo = startInfo };
		process.OutputDataReceived += (sender, args) => {
			var logLine = MessageSlicer.SliceMessage(args.Data ?? string.Empty);
			
			if (logLine.LogLevel is null) {
				Dispatcher.UIThread.Post(
					() => parent.AppendToLastLogRow(logLine),
					DispatcherPriority.MinValue
				);
			} else {
				Dispatcher.UIThread.Post(
					() => parent.AddRowToLogGrid(logLine),
					DispatcherPriority.MinValue
				);
				if (logLine.LogLevel == LogLevel.Progress) {
					if (ushort.TryParse(logLine.Message.Trim().TrimEnd('%'), out var progressValue)) {
						parent.Progress = progressValue;
					}
				}
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
		} else {
			Logger.Error("Converter Error! See log.txt for details.");
			Logger.Error("If you require assistance please upload log.txt to forums for a detailed post-mortem.");
		}
	}

	private readonly MainWindowViewModel parent;
	private readonly Configuration config;
}