using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using commonItems;
using Fronter.Models.Configuration;
using Fronter.Services;
using Fronter.Views;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.ViewModels;
using ReactiveUI;
using System.Threading.Tasks;
using commonItems;
using FluentAvalonia.Styling;
using Fronter.Models;
using MessageBox.Avalonia;
using MessageBox.Avalonia.Enums;
using MessageBox.Avalonia.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fronter.ViewModels;

public class MainWindowViewModel : ViewModelBase {
	public IEnumerable<KeyValuePair<string, string>> Languages =>
		loc.LoadedLanguages.ToDictionary(l => l, l => loc.TranslateLanguage(l)); // language key, language loc
	
	private Configuration config = new Configuration();
	private Services.Localization loc = new Services.Localization();

	public MainWindowViewModel() {
		var theme = AvaloniaLocator.Current.GetService<FluentAvaloniaTheme>();
		if (theme is not null) {
			var fronterThemePath = Path.Combine("Configuration", "fronter-theme.txt");
			if (File.Exists(fronterThemePath)) {
				var parser = new Parser();
				parser.RegisterKeyword("theme", reader => theme.RequestedTheme = reader.GetString());
				parser.ParseFile(fronterThemePath);
			}

			theme.RequestedThemeChanged += (sender, args) => {
				using var fs = new FileStream(fronterThemePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
				using var writer = new StreamWriter(fs);
				writer.WriteLine($"theme={args.NewTheme}");
				writer.Close();
			};
		}
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
		converterLauncher.LoadConfiguration(config);
		converterLauncher.LaunchConverter();
	}

	public async void CheckForUpdates() {
		var mainWindow = Window;
		if (mainWindow is null) {
			return;
		}

		Logger.Debug($"{nameof(config.UpdateCheckerEnabled)}: {config.UpdateCheckerEnabled}");
		Logger.Debug($"{nameof(config.CheckForUpdatesOnStartup)}: {config.CheckForUpdatesOnStartup}");
		Logger.Debug($"is update available: {UpdateChecker.IsUpdateAvailable("commit_id.txt", config.PagesCommitIdUrl)}");
		if (config.UpdateCheckerEnabled &&
		    config.CheckForUpdatesOnStartup &&
		    UpdateChecker.IsUpdateAvailable("commit_id.txt", config.PagesCommitIdUrl)) {
			var info = UpdateChecker.GetLatestReleaseInfo(config.Name);

			const string updateNow = "Update now";
			const string maybeLater = "Maybe later";
			var msgBody = UpdateChecker.GetUpdateMessageBody(Localization.NEWVERSIONBODY, info);
			var messageBoxWindow = MessageBoxManager
				.GetMessageBoxCustomWindow(new MessageBoxCustomParams {
					Icon = Icon.Info,
					ContentHeader = "An update is available!",
					ContentTitle = Localization.NEWVERSIONTITLE,
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
					UpdateChecker.StartUpdaterAndDie(info.ZipUrl, config.ConverterFolder);
				} else {
					BrowserLauncher.Open(config.ConverterReleaseForumThread);
					BrowserLauncher.Open(config.LatestGitHubConverterReleaseUrl);
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
				ContentTitle = Localization.ABOUT_TITLE,
				Icon = Icon.Info,
				ContentHeader = Localization.ABOUT_HEADER,
				ContentMessage = Localization.ABOUT_BODY,
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
		var theme = AvaloniaLocator.Current.GetService<FluentAvaloniaTheme>();
		if (theme is null) {
			return;
		}
		theme.RequestedTheme = themeName;
	}
	

	public void AddRowToLogGrid(LogLine logLine) {
		LogLines.Add(logLine);

		var logGrid = Window?.FindControl<DataGrid>("LogGrid");
		logGrid?.ScrollIntoView(logLine, null);
	}

	public void AddRowsToLogGrid() { // todo: remove debug
		int counter = 0;
		while (counter++ < 10000) {
			var logLine = new LogLine {Message = $"Message no. {counter}"};
			Dispatcher.UIThread.Post(
				() => AddRowToLogGrid(logLine),
				DispatcherPriority.MinValue
			);
		}
	}

	public void StartWorkerThreads() {
		var logWatcher = new LogWatcher("ImperatorToCK3/log.txt");
		var logThread = new Thread(logWatcher.WatchLog);
		logThread.Start();
	}

	public ObservableCollection<LogLine> LogLines { get; } = new();
}