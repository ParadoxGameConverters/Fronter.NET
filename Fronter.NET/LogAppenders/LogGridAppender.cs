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
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace Fronter.LogAppenders;

internal sealed class LogGridAppender : AppenderSkeleton {
	private readonly ConcurrentQueue<LogLine> pendingLogLines = new();
	private IDisposable? logFilterSubscription;
	private int flushScheduled;
	private ushort? latestProgressValue;
	private LogLine? lastLogRow;
	private LogLine? lastVisibleRow;

	public ObservableCollection<LogLine> LogLines { get; } = [];
	private ReadOnlyObservableCollection<LogLine> filteredLogLines;
	public ReadOnlyObservableCollection<LogLine> FilteredLogLines => filteredLogLines;

	public Level LogFilterLevel = Level.Warn;

	public DataGrid? LogGrid { get; set; }

	public LogGridAppender() {
		filteredLogLines = new ReadOnlyObservableCollection<LogLine>([]);
		RebuildFilterBinding();
	}

	protected override bool RequiresLayout => false;

	protected override void Append(LoggingEvent loggingEvent) {
		// Tab characters are incorrectly displayed in the log grid as of Avalonia 0.10.18.
		string message = loggingEvent.RenderedMessage?.Replace("\t", "    ") ?? string.Empty;
		var newLogLine = new LogLine(loggingEvent.TimeStamp, loggingEvent.Level, message);
		pendingLogLines.Enqueue(newLogLine);

		if (loggingEvent.Level == LogExtensions.ProgressLevel &&
		    ushort.TryParse(message.Trim().TrimEnd('%'), out var progressValue)) {
			latestProgressValue = progressValue;
		}

		ScheduleFlush();
	}

	public void ToggleLogFilterLevel() {
		RebuildFilterBinding();
		lastVisibleRow = filteredLogLines.LastOrDefault();
	}

	public void ClearDisplayedLogLines() {
		while (pendingLogLines.TryDequeue(out _)) {
		}

		latestProgressValue = null;
		Dispatcher.UIThread.Post(() => {
			LogLines.Clear();
			lastLogRow = null;
			lastVisibleRow = null;
		}, DispatcherPriority.Normal);
	}

	protected override void OnClose() {
		logFilterSubscription?.Dispose();
		base.OnClose();
	}

	private void RebuildFilterBinding() {
		logFilterSubscription?.Dispose();
		logFilterSubscription = LogLines.ToObservableChangeSet()
			.Filter(IsVisibleForCurrentFilter)
			.Bind(out filteredLogLines)
			.Subscribe();
	}

	private bool IsVisibleForCurrentFilter(LogLine line) {
		return line.Level == Level.Notice || line.Level is not null && line.Level >= LogFilterLevel;
	}

	private void ScheduleFlush() {
		if (Interlocked.Exchange(ref flushScheduled, 1) == 1) {
			return;
		}

		Dispatcher.UIThread.Post(FlushPendingLogLines, DispatcherPriority.Background);
	}

	private void FlushPendingLogLines() {
		try {
			bool shouldScroll = false;
			while (pendingLogLines.TryDequeue(out var logLine)) {
				if (logLine.Level is null) {
					AppendToLastLogRow(logLine);
					continue;
				}

				LogLines.Add(logLine);
				lastLogRow = logLine;
				if (IsVisibleForCurrentFilter(logLine)) {
					lastVisibleRow = logLine;
					shouldScroll = true;
				}
			}

			UpdateProgressIfNeeded();

			if (shouldScroll) {
				ScrollToLogEnd();
			}
		} finally {
			Interlocked.Exchange(ref flushScheduled, 0);
			if (!pendingLogLines.IsEmpty) {
				ScheduleFlush();
			}
		}
	}

	private void UpdateProgressIfNeeded() {
		if (latestProgressValue is not ushort progressValue) {
			return;
		}

		latestProgressValue = null;
		if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) {
			return;
		}

		if (desktop.MainWindow?.DataContext is MainWindowViewModel mainWindowDataContext) {
			mainWindowDataContext.Progress = progressValue;
		}
	}

	private void AppendToLastLogRow(LogLine logLine) {
		if (lastLogRow is null) {
			LogLines.Add(logLine);
			lastLogRow = logLine;
			return;
		}

		lastLogRow.Message += $"\n{logLine.Message}";
		if (IsVisibleForCurrentFilter(lastLogRow)) {
			lastVisibleRow = lastLogRow;
		}
	}

	public void ScrollToLogEnd() {
		Dispatcher.UIThread.Post(() => LogGrid?.ScrollIntoView(lastVisibleRow, column: null), DispatcherPriority.Background);
	}
}