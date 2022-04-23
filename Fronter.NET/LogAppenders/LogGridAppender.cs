using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using commonItems;
using DynamicData;
using DynamicData.Binding;
using Fronter.Models;
using Fronter.ViewModels;
using log4net.Appender;
using log4net.Core;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Fronter.LogAppenders;

public class LogGridAppender : MemoryAppender {
	public ObservableCollection<LogLine> LogLines { get; } = new();
	private ReadOnlyObservableCollection<LogLine> filteredLogLines;
	public ReadOnlyObservableCollection<LogLine> FilteredLogLines => filteredLogLines;

	public Level LogFilterLevel = Level.Warn;

	public DataGrid? LogGrid { get; set; } = null;
	
	public LogGridAppender() {
		LogLines.ToObservableChangeSet()
			.Filter(line => line.Level >= LogFilterLevel)
			.Bind(out filteredLogLines)
			.Subscribe();
	}

	protected override void Append(LoggingEvent loggingEvent) {
		var newLogLine = new LogLine {
			Level = loggingEvent.Level,
			// tab characters are incorrectly displayed in the log grid as of Avalonia 0.10.13
			Message = loggingEvent.RenderedMessage.Replace("\t", "    "),
			Timestamp = GetTimestampString(loggingEvent.TimeStamp),
			//Source = LogLine.MessageSource.UI // todo: find a way to distinguish
		};
		AddToLogGrid(newLogLine);
		ScrollToLogEnd();
		
		base.Append(loggingEvent);
	}

	public static string GetTimestampString(DateTime dateTime) {
		return dateTime.ToString("yyyy’-‘MM’-‘dd’ ’HH’:’mm’:’ss");
	}

	private void AddToLogGrid(LogLine logLine) {
		if (logLine.Level is null) {
			Dispatcher.UIThread.Post(
				() => AppendToLastLogRow(logLine),
				DispatcherPriority.MinValue
			);
		} else {
			Dispatcher.UIThread.Post(
				() => AddRowToLogGrid(logLine),
				DispatcherPriority.MinValue
			);
			if (logLine.Level == LogExtensions.progressLevel) {
				if (ushort.TryParse(logLine.Message.Trim().TrimEnd('%'), out var progressValue)) {
					Dispatcher.UIThread.Post(() => {
						if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) {
							return;
						}

						if (desktop.MainWindow.DataContext is MainWindowViewModel mainWindowDataContext) {
							mainWindowDataContext.Progress = progressValue;
						}
					});
				}
			}
		}
	}
	
	public void ToggleLogFilterLevel() {
		LogLines.ToObservableChangeSet()
			.Filter(line => line.Level >= LogFilterLevel)
			.Bind(out filteredLogLines)
			.Subscribe();
		lastVisibleRow = LogLines.LastOrDefault(line => line.Level >= LogFilterLevel);
	}
	
	private LogLine? lastLogRow;
	private LogLine? lastVisibleRow;
	public void AddRowToLogGrid(LogLine logLine) {
		LogLines.Add(logLine);
		lastLogRow = logLine;
		if (logLine.Level >= LogFilterLevel) {
			lastVisibleRow = logLine;
		}
	}

	private void AppendToLastLogRow(LogLine logLine) {
		if (lastLogRow is null) {
			AddRowToLogGrid(logLine);
		} else {
			lastLogRow.Message += $"\n{logLine.Message}";
		}
	}

	public void ScrollToLogEnd() {
		LogGrid?.ScrollIntoView(lastVisibleRow, null);
	}
}