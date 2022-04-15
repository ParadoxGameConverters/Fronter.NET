using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using commonItems;
using Fronter.ViewModels;
using System;
using System.IO;

namespace Fronter.Services;

public class LogWatcher : IDisposable {
	public LogWatcher(string logFile) {
		tailSource = logFile;
		Logger.Debug("TAILSOURCE ASSIGNED");
		logStream = new FileStream(tailSource, FileMode.Open, FileAccess.Read, FileShare.Delete);
		Logger.Debug("FILESTREAM CREATED");
		logStreamReader = new StreamReader(logStream);
		Logger.Debug("StreamReader CREATED");

		if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
			windowDataContext = (MainWindowViewModel?)desktop.MainWindow.DataContext;
			Logger.Debug("windowDataContext CREATED");
		}
	}

	public void Dispose() {
		logStreamReader.Close();
		GC.SuppressFinalize(this);
	}
	public void WatchLog() {
		try {
			//Logger.Notice($"WATCHING {tailSource}!!!"); // TODO: REMOVE DEBUG

			var oneLastRun = false;
			Logger.Notice($"oneLastRun: {oneLastRun}"); // TODO: REMOVE DEBUG
			while (!terminate || oneLastRun) {
				//Logger.Notice($"INSIDE FIRST LOOP"); // TODO: REMOVE DEBUG
				while (!logStreamReader.EndOfStream) {
					//Logger.Notice($"INSIDE SECOND LOOP"); // TODO: REMOVE DEBUG
					var line = logStreamReader.ReadLine();
					if (line is null) {
						continue;
					}
					//Logger.Notice($"LOG LINE:\t\t\t{line}"); // TODO: REMOVE DEBUG

					var logLine = MessageSlicer.SliceMessage(line);
					//Logger.Notice($"logMessage:\t\t\t{logMessage.Message}"); // TODO: REMOVE DEBUG
					if (TranscriberMode) {
						var logLevel = logLine.LogLevel ?? Logger.LogLevel.Info;
						Logger.Log(logLevel, logLine.Message);
					}

					if (EmitterMode) {
						if (logLine.LogLevel is null) {
							Dispatcher.UIThread.Post(
								() => windowDataContext?.AppendToLastLogRow(logLine),
								DispatcherPriority.MinValue
							);
						} else {
							Dispatcher.UIThread.Post(
								() => windowDataContext?.AddRowToLogGrid(logLine),
								DispatcherPriority.MinValue
							);
						}
					}
				}

				if (terminate) {
					if (oneLastRun) {
						break;
					}
					oneLastRun = true;
				}
			}
		} catch (Exception e) {
			Console.WriteLine(e);
			throw;
		}
	}

	private readonly string tailSource;
	public bool TranscriberMode { get; set; } = true;
	public bool EmitterMode { get; set; } = true;

	public void Terminate() {
		terminate = true;
	}
	private bool terminate = false;

	private MainWindowViewModel? windowDataContext;

	private FileStream logStream;
	private StreamReader logStreamReader;
}