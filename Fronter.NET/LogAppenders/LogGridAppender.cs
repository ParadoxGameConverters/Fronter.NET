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
using System.Globalization;
using System.Linq;

namespace Fronter.LogAppenders;

public sealed class LogGridAppender : MemoryAppender {
	public ObservableCollection<LogLine> LogLines { get; } = [];
	private ReadOnlyObservableCollection<LogLine> filteredLogLines;
	public ReadOnlyObservableCollection<LogLine> FilteredLogLines => filteredLogLines;

	public Level LogFilterLevel = Level.Warn;

	public DataGrid? LogGrid { get; set; }

	public LogGridAppender() {
		// The idea of notice in the converters was to display the notice regardless of filtering level.
		LogLines.ToObservableChangeSet()
			.Filter(line => line.Level == Level.Notice || line.Level >= LogFilterLevel)
			.Bind(out filteredLogLines)
			.Subscribe();
	}

	protected override void Append(LoggingEvent loggingEvent) {
		var newLogLine = new LogLine {
			Level = loggingEvent.Level,
			// Tab characters are incorrectly displayed in the log grid as of Avalonia 0.10.18.
			Message = loggingEvent.RenderedMessage?.Replace("\t", "    ") ?? string.Empty,
			Timestamp = GetTimestampString(loggingEvent.TimeStamp),
		};
		AddToLogGrid(newLogLine);
		ScrollToLogEnd();

		base.Append(loggingEvent);
	}

	private static string GetTimestampString(DateTime dateTime) {
		return dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
	}

	private void AddToLogGrid(LogLine logLine) {
		if (logLine.Level is null) {
			Dispatcher.UIThread.Post(
				() => AppendToLastLogRow(logLine),
				DispatcherPriority.Normal
			);
		} else {
			AddRowToLogGrid(logLine);
			if (logLine.Level == LogExtensions.ProgressLevel) {
				if (ushort.TryParse(logLine.Message.Trim().TrimEnd('%'), out var progressValue)) {
					Dispatcher.UIThread.Post(() => {
						if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) {
							return;
						}

						if (desktop.MainWindow?.DataContext is MainWindowViewModel mainWindowDataContext) {
							mainWindowDataContext.Progress = progressValue;
						}
					});
				}
			}
		}
	}

	public void ToggleLogFilterLevel() {
		LogLines.ToObservableChangeSet()
			.Filter(line => line.Level is null || line.Level == Level.Notice || line.Level >= LogFilterLevel)
			.Bind(out filteredLogLines)
			.Subscribe();
		lastVisibleRow = filteredLogLines.LastOrDefault();
	}

	private LogLine? lastLogRow;
	private LogLine? lastVisibleRow;
	private void AddRowToLogGrid(LogLine logLine) {
		Dispatcher.UIThread.Post(() => LogLines.Add(logLine));
		lastLogRow = logLine;
		if (logLine.Level is not null && logLine.Level >= LogFilterLevel) {
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
		Dispatcher.UIThread.Post(() => LogGrid?.ScrollIntoView(lastVisibleRow, column: null), DispatcherPriority.Background);
	}
}