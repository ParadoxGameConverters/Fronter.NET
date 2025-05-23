﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Notification;
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
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;

namespace Fronter.ViewModels;

[SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
internal sealed class MainWindowViewModel : ViewModelBase {
	private static readonly ILog logger = LogManager.GetLogger("Frontend");
	private readonly TranslationSource loc = TranslationSource.Instance;
	public IEnumerable<MenuItemViewModel> LanguageMenuItems => loc.LoadedLanguages
		.Select(l => new MenuItemViewModel {
			Command = SetLanguageCommand,
			CommandParameter = l,
			Header = loc.TranslateLanguage(l),
			Items = Array.Empty<MenuItemViewModel>(),
		});

	public INotificationMessageManager NotificationManager { get; } = new NotificationMessageManager();

	private IdObjectCollection<string, FrontendTheme> Themes { get; } = [
		new() {Id = "Light", LocKey = "THEME_LIGHT"},
		new() {Id = "Dark", LocKey = "THEME_DARK"},
	];
	public IEnumerable<MenuItemViewModel> ThemeMenuItems => Themes
		.Select(theme => new MenuItemViewModel {
			Command = SetThemeCommand,
			CommandParameter = theme.Id,
			Header = loc.Translate(theme.LocKey),
			Items = Array.Empty<MenuItemViewModel>(),
		});

	internal Config Config { get; }

	internal PathPickerViewModel PathPicker { get; }
	internal ModsPickerViewModel ModsPicker { get; }
	public bool ModsPickerTabVisible => Config.ModAutoGenerationSource is not null;
	public OptionsViewModel Options { get; }
	public bool OptionsTabVisible => Options.Items.Any();

	public Level LogFilterLevel {
		get => LogGridAppender.LogFilterLevel;
		private set => this.RaiseAndSetIfChanged(ref LogGridAppender.LogFilterLevel, value);
	}

	public string SaveStatus {
		get;
		set => this.RaiseAndSetIfChanged(ref field, value);
	} = "CONVERTSTATUSPRE";

	public string ConvertStatus {
		get;
		set => this.RaiseAndSetIfChanged(ref field, value);
	} = "CONVERTSTATUSPRE";

	public string CopyStatus {
		get;
		set => this.RaiseAndSetIfChanged(ref field, value);
	} = "CONVERTSTATUSPRE";

	public bool ConvertButtonEnabled {
		get;
		set => this.RaiseAndSetIfChanged(ref field, value);
	} = true;

	public MainWindowViewModel(DataGrid logGrid) {
		Config = new Config();

		var appenders = LogManager.GetRepository().GetAppenders();
		var gridAppender = appenders.First(a => a.Name.Equals("grid"));
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
		var level = LogManager.GetRepository().LevelMap[value];
		if (level is null) {
			logger.Error($"Unknown log level: {value}");
		} else {
			LogFilterLevel = level;
		}
		LogGridAppender.ToggleLogFilterLevel();
		this.RaisePropertyChanged(nameof(FilteredLogLines));
		Dispatcher.UIThread.Post(ScrollToLogEnd, DispatcherPriority.Normal);
	}

	public ushort Progress {
		get;
		set => this.RaiseAndSetIfChanged(ref field, value);
	} = 0;

	public bool IndeterminateProgress {
		get;
		set => this.RaiseAndSetIfChanged(ref field, value);
	} = false;

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
	public async Task LaunchConverter() {
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
		await Task.Run(async () => {
			ConvertStatus = "CONVERTSTATUSIN";

			try {
				var launchConverterTask = converterLauncher.LaunchConverter();
				launchConverterTask.Wait();
				success = launchConverterTask.Result;
			} catch (TaskCanceledException e) {
				logger.Debug($"Converter backend task was cancelled: {e.Message}");
				success = false;
			} catch (Exception e) {
				logger.Error($"Failed to start converter backend: {e.Message}");
				var messageText = $"{loc.Translate("FAILED_TO_START_CONVERTER_BACKEND")}: {e.Message}";
				if (!ElevatedPrivilegesDetector.IsAdministrator) {
					messageText += "\n\n" + loc.Translate("ELEVATED_PRIVILEGES_REQUIRED");
					if (OperatingSystem.IsWindows()) {
						messageText += "\n\n" + loc.Translate("RUN_AS_ADMIN");
					} else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD()) {
						messageText += "\n\n" + loc.Translate("RUN_WITH_SUDO");
					}
				} else {
					messageText += "\n\n" + loc.Translate("FAILED_TO_START_CONVERTER_POSSIBLE_BUG");
				}

				Dispatcher.UIThread.Post(() => MessageBoxManager.GetMessageBoxStandard(
					title: loc.Translate("FAILED_TO_START_CONVERTER"),
					text: messageText,
					ButtonEnum.Ok,
					Icon.Error
				).ShowWindowDialogAsync(MainWindow.Instance).Wait());

				success = false;
			}

			if (success) {
				ConvertStatus = "CONVERTSTATUSPOSTSUCCESS";

				if (Config.CopyToTargetGameModDirectory) {
					CopyToTargetGameModDirectory();
				} else {
					ConvertButtonEnabled = true;
				}
			} else {
				ConvertStatus = "CONVERTSTATUSPOSTFAIL";
				await Dispatcher.UIThread.InvokeAsync(ShowErrorMessageBox);
				ConvertButtonEnabled = true;
			}
		});
	}

	private async Task ShowErrorMessageBox() {
		var messageBoxWindow = MessageBoxManager
			.GetMessageBoxStandard(new MessageBoxStandardParams {
				Icon = Icon.Error,
				ContentTitle = loc.Translate("CONVERSION_FAILED"),
				ContentMessage = loc.Translate("CONVERSION_FAILED_MESSAGE"),
				Markdown = true,
				ButtonDefinitions = ButtonEnum.OkCancel,
			});
		var result = await messageBoxWindow.ShowWindowDialogAsync(MainWindow.Instance);
		if (result == ButtonResult.Ok) {
			BrowserLauncher.Open(Config.ConverterReleaseForumThread);
		}
	}

	public async Task CheckForUpdates() {
		if (!Config.UpdateCheckerEnabled) {
			return;
		}

		bool isUpdateAvailable = await UpdateChecker.IsUpdateAvailable("commit_id.txt", Config.PagesCommitIdUrl);
		if (!isUpdateAvailable) {
			return;
		}

		var info = await UpdateChecker.GetLatestReleaseInfo(Config.Name);
		if (info.AssetUrl is null) {
			return;
		}

		var updateNowStr = loc.Translate("UPDATE_NOW");
		var maybeLaterStr = loc.Translate("MAYBE_LATER");
		var msgBody = UpdateChecker.GetUpdateMessageBody(loc.Translate("NEW_VERSION_BODY"), info);
		var messageBoxWindow = MessageBoxManager
			.GetMessageBoxCustom(new MessageBoxCustomParams {
				Icon = Icon.Info,
				ContentTitle = loc.Translate("NEW_VERSION_TITLE"),
				ContentHeader = loc.Translate("NEW_VERSION_HEADER"),
				ContentMessage = msgBody,
				Markdown = true,
				ButtonDefinitions = [
					new() {Name = updateNowStr, IsDefault = true},
					new() {Name = maybeLaterStr, IsCancel = true},
				],
				MaxWidth = 1280,
				MaxHeight = 720,
			});

		bool performUpdate = false;
		await Dispatcher.UIThread.InvokeAsync(async () => {
			string result = await messageBoxWindow.ShowWindowDialogAsync(MainWindow.Instance);
			performUpdate = result.Equals(updateNowStr);
		}, DispatcherPriority.Normal);

		if (!performUpdate) {
			logger.Info($"Update to version {info.Version} postponed.");
			return;
		}

		// If we can use an installer, download it, run it, and exit.
		if (info.UseInstaller) {
			await UpdateChecker.RunInstallerAndDie(info.AssetUrl, Config, NotificationManager);
		} else {
			UpdateChecker.StartUpdaterAndDie(info.AssetUrl, Config.ConverterFolder);
		}
	}

	public async Task CheckForUpdatesOnStartup() {
		if (!Config.CheckForUpdatesOnStartup) {
			return;
		}
		await CheckForUpdates();
	}

#pragma warning disable CA1822
	public void Exit() {
#pragma warning restore CA1822
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
			desktop.Shutdown(exitCode: 0);
		}
	}

#pragma warning disable CA1822
	public async Task OpenAboutDialog() {
#pragma warning restore CA1822
		var messageBoxWindow = MessageBoxManager
			.GetMessageBoxStandard(new MessageBoxStandardParams {
				ContentTitle = TranslationSource.Instance["ABOUT_TITLE"],
				Icon = Icon.Info,
				ContentHeader = TranslationSource.Instance["ABOUT_HEADER"],
				ContentMessage = TranslationSource.Instance["ABOUT_BODY"],
				ButtonDefinitions = ButtonEnum.Ok,
				SizeToContent = SizeToContent.WidthAndHeight,
				MinHeight = 250,
				ShowInCenter = true,
				WindowStartupLocation = WindowStartupLocation.CenterOwner,
			});
		await messageBoxWindow.ShowWindowDialogAsync(MainWindow.Instance);
	}

#pragma warning disable CA1822
	public void OpenPatreonPage() {
#pragma warning restore CA1822
		BrowserLauncher.Open("https://www.patreon.com/ParadoxGameConverters");
	}

	public void SetLanguage(string languageKey) {
		loc.SaveLanguage(languageKey);
	}

#pragma warning disable CA1822
	public void SetTheme(string themeName) {
#pragma warning restore CA1822
		_ = App.SaveTheme(themeName);
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