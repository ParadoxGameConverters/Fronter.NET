using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using commonItems.Collections;
using Fronter.Extensions;
using Fronter.LogAppenders;
using Fronter.Models;
using Fronter.Models.Configuration;
using Fronter.Services;
using Fronter.Views;
using log4net;
using log4net.Core;
using MessageBox.Avalonia;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using MessageBox.Avalonia.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading;

namespace Fronter.ViewModels;

[SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
public class MainWindowViewModel : ViewModelBase {
	private static readonly ILog logger = LogManager.GetLogger("Frontend");
	private readonly TranslationSource loc = TranslationSource.Instance;
	public IEnumerable<MenuItemViewModel> LanguageMenuItems => loc.LoadedLanguages
		.Select(l => new MenuItemViewModel {
			Command = SetLanguageCommand,
			CommandParameter = l,
			Header = loc.TranslateLanguage(l),
			Items = Array.Empty<MenuItemViewModel>()
		});
	
	private IdObjectCollection<string, FrontendTheme> Themes { get; } = new() {
		new FrontendTheme {Id = "Light", LocKey = "THEME_LIGHT"},
		new FrontendTheme {Id = "Dark", LocKey = "THEME_DARK"}
	};
	public IEnumerable<MenuItemViewModel> ThemeMenuItems => Themes
		.Select(theme => new MenuItemViewModel {
			Command = SetThemeCommand,
			CommandParameter = theme.Id,
			Header = loc.Translate(theme.LocKey),
			Items = Array.Empty<MenuItemViewModel>()
		});

	public Configuration Config { get; }

	public PathPickerViewModel PathPicker { get; }
	public ModsPickerViewModel ModsPicker { get; }
	public bool ModsPickerTabVisible => Config.ModAutoGenerationSource is not null;
	public OptionsViewModel Options { get; }
	public bool OptionsTabVisible => Options.Items.Any();

	public Level LogFilterLevel {
		get => LogGridAppender.LogFilterLevel;
		private set => this.RaiseAndSetIfChanged(ref LogGridAppender.LogFilterLevel, value);
	}

	private string saveStatus = "CONVERTSTATUSPRE";
	private string convertStatus = "CONVERTSTATUSPRE";
	private string copyStatus = "CONVERTSTATUSPRE";

	public string SaveStatus {
		get => saveStatus;
		set => this.RaiseAndSetIfChanged(ref saveStatus, value);
	}
	public string ConvertStatus {
		get => convertStatus;
		set => this.RaiseAndSetIfChanged(ref convertStatus, value);
	}
	public string CopyStatus {
		get => copyStatus;
		set => this.RaiseAndSetIfChanged(ref copyStatus, value);
	}

	private bool convertButtonEnabled = true;
	public bool ConvertButtonEnabled {
		get => convertButtonEnabled;
		set => this.RaiseAndSetIfChanged(ref convertButtonEnabled, value);
	}

	public MainWindowViewModel(DataGrid logGrid) {
		Config = new Configuration();

		var appenders = LogManager.GetRepository().GetAppenders();
		var gridAppender = appenders.First(a => a.Name == "grid");
		if (gridAppender is not LogGridAppender logGridAppender) {
			throw new LogException($"Log appender \"{gridAppender.Name}\" is not a {typeof(LogGridAppender)}");
		}
		LogGridAppender = logGridAppender;
		LogGridAppender.LogGrid = logGrid;

		PathPicker = new PathPickerViewModel(Config);
		ModsPicker = new ModsPickerViewModel(Config);
		Options = new OptionsViewModel(Config.Options);

		// Create reactive commands.
		ToggleLogFilterLevelCommand = ReactiveCommand.Create<string>(ToggleLogFilterLevel);
		SetLanguageCommand = ReactiveCommand.Create<string>(SetLanguage);
		SetThemeCommand = ReactiveCommand.Create<string>(SetTheme);
	}

	public ReadOnlyObservableCollection<LogLine> FilteredLogLines => LogGridAppender.FilteredLogLines;

	#region Reactive commands

	public ReactiveCommand<string, Unit> ToggleLogFilterLevelCommand { get; }
	public ReactiveCommand<string, Unit> SetLanguageCommand { get; }
	public ReactiveCommand<string, Unit> SetThemeCommand { get; }

	#endregion

	public void ToggleLogFilterLevel(string value) {
		LogFilterLevel = LogManager.GetRepository().LevelMap[value];
		LogGridAppender.ToggleLogFilterLevel();
		this.RaisePropertyChanged(nameof(FilteredLogLines));
		Dispatcher.UIThread.Post(ScrollToLogEnd, DispatcherPriority.Normal);
	}

	private ushort progress = 0;
	public ushort Progress {
		get => progress;
		set => this.RaiseAndSetIfChanged(ref progress, value);
	}

	private bool indeterminateProgress = false;
	public bool IndeterminateProgress {
		get => indeterminateProgress;
		set => this.RaiseAndSetIfChanged(ref indeterminateProgress, value);
	}

	private bool VerifyMandatoryPaths() {
		foreach (var folder in Config.RequiredFolders) {
			if (!folder.Mandatory || Directory.Exists(folder.Value)) {
				continue;
			}

			logger.Error($"Mandatory folder {folder.Name} at {folder.Value} not found.");
			return false;
		}

		foreach (var file in Config.RequiredFiles.Where(file => file.Mandatory && !File.Exists(file.Value))) {
			logger.Error($"Mandatory file {file.Name} at {file.Value} not found.");
			return false;
		}

		return true;
	}

	private void ClearLogGrid() {
		LogGridAppender.LogLines.Clear();
	}

	private void CopyToTargetGameModDirectory() {
		var modCopier = new ModCopier(Config);
		bool copySuccess;
		var copyThread = new Thread(() => {
			IndeterminateProgress = true;
			CopyStatus = "CONVERTSTATUSIN";

			copySuccess = modCopier.CopyMod();
			CopyStatus = copySuccess ? "CONVERTSTATUSPOSTSUCCESS" : "CONVERTSTATUSPOSTFAIL";
			Progress = Config.ProgressOnCopyingComplete;
			IndeterminateProgress = false;

			ConvertButtonEnabled = true;
		});
		copyThread.Start();
	}
	public void LaunchConverter() {
		ConvertButtonEnabled = false;
		ClearLogGrid();

		Progress = 0;
		SaveStatus = "CONVERTSTATUSPRE";
		ConvertStatus = "CONVERTSTATUSPRE";
		CopyStatus = "CONVERTSTATUSPRE";

		if (!VerifyMandatoryPaths()) {
			ConvertButtonEnabled = true;
			return;
		}
		Config.ExportConfiguration();

		var converterLauncher = new ConverterLauncher(Config);
		bool success;
		var converterThread = new Thread(() => {
			ConvertStatus = "CONVERTSTATUSIN";
			var launchConverterTask = converterLauncher.LaunchConverter();
			launchConverterTask.Wait();
			success = launchConverterTask.Result;
			if (success) {
				ConvertStatus = "CONVERTSTATUSPOSTSUCCESS";

				if (Config.CopyToTargetGameModDirectory) {
					CopyToTargetGameModDirectory();
				} else {
					ConvertButtonEnabled = true;
				}
			} else {
				ConvertStatus = "CONVERTSTATUSPOSTFAIL";
				Dispatcher.UIThread.Post(ShowErrorMessageBox);
				ConvertButtonEnabled = true;
			}
		});
		converterThread.Start();
	}

	private async void ShowErrorMessageBox() {
		var messageBoxWindow = MessageBoxManager
			.GetMessageBoxStandardWindow(new MessageBoxStandardParams {
				Icon = Icon.Error,
				ContentTitle = loc.Translate("CONVERSION_FAILED"),
				ContentMessage = loc.Translate("CONVERSION_FAILED_MESSAGE"),
				Markdown = true,
				ButtonDefinitions = ButtonEnum.OkCancel
			});
		var result = await messageBoxWindow.ShowDialog(MainWindow.Instance);
		if (result == ButtonResult.Ok) {
			BrowserLauncher.Open(Config.ConverterReleaseForumThread);
		}
	}

	public async void CheckForUpdates() {
		if (!Config.UpdateCheckerEnabled) {
			return;
		}

		bool isUpdateAvailable = await UpdateChecker.IsUpdateAvailable("commit_id.txt", Config.PagesCommitIdUrl);
		if (!isUpdateAvailable) {
			return;
		}

		var info = await UpdateChecker.GetLatestReleaseInfo(Config.Name);

		var updateNowStr = loc.Translate("UPDATE_NOW");
		var maybeLaterStr = loc.Translate("MAYBE_LATER");
		var msgBody = UpdateChecker.GetUpdateMessageBody(loc.Translate("NEW_VERSION_BODY"), info);
		var messageBoxWindow = MessageBoxManager
			.GetMessageBoxCustomWindow(new MessageBoxCustomParams {
				Icon = Icon.Info,
				ContentTitle = loc.Translate("NEW_VERSION_TITLE"),
				ContentHeader = loc.Translate("NEW_VERSION_HEADER"),
				ContentMessage = msgBody,
				Markdown = true,
				ButtonDefinitions = new[] {
					new ButtonDefinition {Name = updateNowStr, IsDefault = true},
					new ButtonDefinition {Name = maybeLaterStr, IsCancel = true}
				},
				MaxWidth = 1280,
				MaxHeight = 720,
			});
		var result = await messageBoxWindow.ShowDialog(MainWindow.Instance);
		if (result != updateNowStr) {
			logger.Info($"Update to version {info.Version} postponed.");
			return;
		}

		if (info.ArchiveUrl is not null) {
			UpdateChecker.StartUpdaterAndDie(info.ArchiveUrl, Config.ConverterFolder);
		} else {
			BrowserLauncher.Open(Config.ConverterReleaseForumThread);
			BrowserLauncher.Open(Config.LatestGitHubConverterReleaseUrl);
		}
	}

	public void CheckForUpdatesOnStartup() {
		if (!Config.CheckForUpdatesOnStartup) {
			return;
		}
		CheckForUpdates();
	}

	public static void Exit() {
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
			desktop.Shutdown(0);
		}
	}

#pragma warning disable CA1822
	public async void OpenAboutDialog() {
#pragma warning restore CA1822
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
		await messageBoxWindow.ShowDialog(MainWindow.Instance);
	}

	public static void OpenPatreonPage() {
		BrowserLauncher.Open("https://www.patreon.com/ParadoxGameConverters");
	}

	public void SetLanguage(string languageKey) {
		loc.SaveLanguage(languageKey);
	}

	public void SetTheme(string themeName) {
		App.SaveTheme(themeName);
	}

	public string WindowTitle {
		get {
			var displayName = loc.Translate(Config.DisplayName);
			if (string.IsNullOrWhiteSpace(displayName)) {
				displayName = "Converter";
			}
			return $"{displayName} Frontend";
		}
	}

	private LogGridAppender LogGridAppender { get; }

	private void ScrollToLogEnd() {
		LogGridAppender.ScrollToLogEnd();
	}
}