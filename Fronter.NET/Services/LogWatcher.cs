using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using commonItems;
using Fronter.ViewModels;
using Fronter.Views;
using System;
using System.IO;

namespace Fronter.Services; 

public class LogWatcher : IDisposable {
	public LogWatcher(string logFile) {
		tailSource = logFile;
		Logger.Debug("TAILSOURCE ASSIGNED");
		using var fs = new FileStream(tailSource, FileMode.Open, FileAccess.Read, FileShare.Delete);
		Logger.Debug("FILESTREAM CREATED");
		logStreamReader = new StreamReader(fs);
		Logger.Debug("StreamReader CREATED");
		
		if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
			windowDataContext = (MainWindowViewModel?)((MainWindow)desktop.MainWindow).DataContext;
			Logger.Debug("windowDataContext CREATED");
		}
	}

	public void Dispose() {
		logStreamReader.Close();
		GC.SuppressFinalize(this);
	}
	public void WatchLog() {
		Logger.Debug($"WATCHING {tailSource}!!!");

		var oneLastRun = false;
		while (!terminate || oneLastRun) {
			while (!logStreamReader.EndOfStream) {
				var line = logStreamReader.ReadLine();
				if (line is null) {
					continue;
				}

				var logMessage = MessageSlicer.SliceMessage(line);
				if (TranscriberMode) {
					Logger.Debug(logMessage.Message); // TODO: MAKE LEVEL MATCH logMessage.LogLevel
				}

				if (EmitterMode) {
					Dispatcher.UIThread.Post(
						() => windowDataContext?.AddRowToLogGrid(logMessage.Message),
						DispatcherPriority.MinValue
					);
				}
			}

			if (terminate) {
				if (oneLastRun) {
					break;
				}
				oneLastRun = true;
			}
		}
	}

	private readonly string tailSource;
	public bool TranscriberMode { get; set; } = false;
	public bool EmitterMode { get; set; } = false;

	public void Terminate() {
		terminate = true;
	}
	private bool terminate = false;

	private MainWindowViewModel? windowDataContext;
	private StreamReader logStreamReader;
}