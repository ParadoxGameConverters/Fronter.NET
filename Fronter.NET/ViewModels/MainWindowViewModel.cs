using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using commonItems;
using Fronter.Extensions;
using Fronter.Models;
using Fronter.Models.Configuration;
using Fronter.Services;
using Fronter.Views;
using MessageBox.Avalonia;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using MessageBox.Avalonia.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using DynamicData;
using DynamicData.Binding;
using Material.Dialog;
using Material.Styles.Themes;
using Material.Styles.Themes.Base;

namespace Fronter.ViewModels;

public class MainWindowViewModel : ViewModelBase {
	private TranslationSource loc = TranslationSource.Instance;
	public IEnumerable<KeyValuePair<string, string>> Languages => loc.LoadedLanguages
		.ToDictionary(l => l, l => loc.TranslateLanguage(l));

	public Configuration Config { get; private set; } = new();
	
	public OptionsViewModel Options { get; }

	private LogLevel logFilterLevel = LogLevel.Warn;
	public LogLevel LogFilterLevel {
		get => logFilterLevel;
		private set => this.RaiseAndSetIfChanged(ref logFilterLevel, value);
	}

	public ObservableCollection<LogLine> LogLines { get; } = new();
	private ReadOnlyObservableCollection<LogLine> filteredLogLines;
	public ReadOnlyObservableCollection<LogLine> FilteredLogLines => filteredLogLines;

	public MainWindowViewModel() {
		Options = new OptionsViewModel(Config.Options);
		
		LogLines.ToObservableChangeSet()
			.Filter(line => line.LogLevel >= LogFilterLevel)
			.Bind(out filteredLogLines)
			.Subscribe();
	}
	
	public void ToggleLogFilterLevel(string value) {
		LogFilterLevel = (LogLevel)Enum.Parse(typeof(LogLevel), value);
		LogLines.ToObservableChangeSet()
			.Filter(line => line.LogLevel >= LogFilterLevel)
			.Bind(out filteredLogLines)
			.Subscribe();
		this.RaisePropertyChanged(nameof(FilteredLogLines));
		Dispatcher.UIThread.Post(ScrollToLogEnd, DispatcherPriority.MinValue);
	}

	private ushort progress = 30; // todo: remove debug value
	public ushort Progress {
		get => progress;
		set => this.RaiseAndSetIfChanged(ref progress, value);
	}

	private static MainWindow? Window {
		get {
			if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
				return (MainWindow)desktop.MainWindow;
			}

			return null;
		}
	}

	public async void LaunchConverter() {
		var converterLauncher = new ConverterLauncher();
		converterLauncher.LoadConfiguration(Config);
		converterLauncher.LaunchConverter();
	}

	public void DEBUGADDTOLOG() {
		AddRowToLogGrid(new LogLine() {
			LogLevel = LogLevel.Info,
			Message = "TEST FROM BUTTON",
			Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
		});
	}

	public async void CheckForUpdates() {
		var mainWindow = Window;
		if (mainWindow is null) {
			return;
		}

		Logger.Debug($"{nameof(Config.UpdateCheckerEnabled)}: {Config.UpdateCheckerEnabled}");
		Logger.Debug($"{nameof(Config.CheckForUpdatesOnStartup)}: {Config.CheckForUpdatesOnStartup}");
		Logger.Debug($"is update available: {UpdateChecker.IsUpdateAvailable("commit_id.txt", Config.PagesCommitIdUrl)}");
		if (Config.UpdateCheckerEnabled &&
		    Config.CheckForUpdatesOnStartup &&
		    UpdateChecker.IsUpdateAvailable("commit_id.txt", Config.PagesCommitIdUrl)) {
			var info = UpdateChecker.GetLatestReleaseInfo(Config.Name);

			const string updateNow = "Update now";
			const string maybeLater = "Maybe later";
			var msgBody = UpdateChecker.GetUpdateMessageBody(loc.Translate("NEWVERSIONBODY"), info);
			var messageBoxWindow = MessageBoxManager
				.GetMessageBoxCustomWindow(new MessageBoxCustomParams {
					Icon = Icon.Info,
					ContentHeader = "An update is available!",
					ContentTitle = loc.Translate("NEWVERSIONTITLE"),
					ContentMessage = msgBody,
					Markdown = true,
					ButtonDefinitions = new[] {
						new ButtonDefinition {Name = updateNow, IsDefault = true},
						new ButtonDefinition {Name = maybeLater, IsCancel = true}
					},
				});
			var result = await messageBoxWindow.ShowDialog(mainWindow);
			Logger.Progress(result);
			if (result == updateNow) {
				if (info.ZipUrl is not null) {
					UpdateChecker.StartUpdaterAndDie(info.ZipUrl, Config.ConverterFolder);
				} else {
					BrowserLauncher.Open(Config.ConverterReleaseForumThread);
					BrowserLauncher.Open(Config.LatestGitHubConverterReleaseUrl);
				}
			}
		}
	}

	public static void Exit() {
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
			desktop.Shutdown(0);
		}
	}

	public async void OpenAboutDialog() {
		var mainWindow = Window;
		if (mainWindow is null) {
			return;
		}

		var messageBoxWindow = MessageBoxManager
			.GetMessageBoxStandardWindow(new MessageBoxStandardParams {
				ContentTitle = TranslationSource.Instance["ABOUT_TITLE"],
				Icon = Icon.Info,
				ContentHeader = TranslationSource.Instance["ABOUT_HEADER"],
				ContentMessage = TranslationSource.Instance["ABOUT_BODY"],
				ButtonDefinitions = ButtonEnum.Ok,
				SizeToContent = SizeToContent.WidthAndHeight,
				MinHeight = 250,
				ShowInCenter = true,
				WindowStartupLocation = WindowStartupLocation.CenterOwner
			});
		await messageBoxWindow.ShowDialog(mainWindow);
	}

	public static async void OpenPatreonPage() {
		BrowserLauncher.Open("https://www.patreon.com/ParadoxGameConverters");
	}

	public void SetLanguage(string languageKey) {
		loc.SaveLanguage(languageKey);
	}

	public void SetTheme(string themeName) {
		var theme = Application.Current?.LocateMaterialTheme<MaterialThemeBase>();
		if (theme is null) {
			return;
		}

		var newMode = (BaseThemeMode)Enum.Parse(typeof(BaseThemeMode), themeName, true);
		theme.CurrentTheme = App.CreateTheme(newMode);
	}

	private LogLine? lastLogRow;
	public void AddRowToLogGrid(LogLine logLine) {
		LogLines.Add(logLine);
		lastLogRow = logLine;

		ScrollToLogEnd();
	}
	public void AppendToLastLogRow(LogLine logLine) {
		if (lastLogRow is null) {
			AddRowToLogGrid(logLine);
		} else {
			lastLogRow.Message += $"\n{logLine.Message}";
		}
	}

	private void ScrollToLogEnd() {
		var logGrid = Window?.FindControl<DataGrid>("LogGrid");
		logGrid?.ScrollIntoView(lastLogRow, null);
	}

	public void StartWorkerThreads() {
		var logWatcher = new LogWatcher("ImperatorToCK3/log.txt");
		var logThread = new Thread(logWatcher.WatchLog);
		logThread.Start();
	}
}