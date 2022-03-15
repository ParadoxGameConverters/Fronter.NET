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
using Fronter.Models;
using Fronter.Models.Configuration;
using Fronter.Services;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using MessageBox.Avalonia.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fronter.ViewModels; 

public class MainWindowViewModel : ViewModelBase {
	public string Greeting => "Welcome to Avalonia!";
	public IEnumerable<KeyValuePair<string, string>> Languages => loc.LoadedLanguages.ToDictionary(l=>l, l=>loc.TranslateLanguage(l)); // language key, language loc

	private Configuration config = new Configuration();
	private Localization loc = new Localization();

	private static MainWindow? Window {
		get {
			if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
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
			var msgBody = UpdateChecker.GetUpdateMessageBody(loc.Translate("NEWVERSIONBODY"), info);
			var messageBoxWindow = MessageBox.Avalonia.MessageBoxManager
				.GetMessageBoxCustomWindow(new MessageBoxCustomParams {
					Icon = MessageBox.Avalonia.Enums.Icon.Info,
					ContentHeader = "An update is available!",
					ContentTitle = loc.Translate("NEWVERSIONTITLE"),
					ContentMessage = msgBody,
					//Markdown = true, // TODO: ENABLE THIS WHEN https://github.com/AvaloniaCommunity/MessageBox.Avalonia/pull/99 IS MERGED
					ButtonDefinitions = new[]
					{
						new ButtonDefinition { Name = updateNow, IsDefault = true },
						new ButtonDefinition { Name = maybeLater, IsCancel = true }
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
		if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
			desktop.Shutdown(0);
		} 
	}
	public async void OpenAboutDialog() {
		var mainWindow = Window;
		if (mainWindow is null) {
			return;
		}
		
		var messageBoxWindow = MessageBox.Avalonia.MessageBoxManager
			.GetMessageBoxStandardWindow(new MessageBoxStandardParams {
				ContentTitle = loc.Translate("ABOUT_TITLE"),
				Icon = Icon.Info,
				ContentHeader = loc.Translate("ABOUT_HEADER"),
				ContentMessage = loc.Translate("ABOUT_BODY"),
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

	public void AddItemToLog(string message) {
		var newLine = new LogLine {
			LogLevel = MessageSlicer.LogLevel.Error,
			Message = message,
			Source = MessageSlicer.MessageSource.UI
		};
		LogLines.Add(newLine);
		
		var logGrid = Window.FindControl<DataGrid>("LogGrid");
		logGrid.ScrollIntoView(newLine, null);
	}

	public void AddItemsToLog() {
		int counter = 0;
		while (counter++ < 10000) {
			var message = $"Message no. {counter}";
			Dispatcher.UIThread.Post(
				()=>AddItemToLog(message),
				DispatcherPriority.MinValue
			);
		}
	}
	public void StartWorkerThreads() {
		var logThread = new Thread(AddItemsToLog);
		logThread.Start();
	}
	
	public ObservableCollection<LogLine> LogLines { get; } = new() { // TODO: REMOVE THIS PROPERTY FROM MAINWINDOW
		// TODO: REMOVE DEBUG ITEMS
		new LogLine() {Message = "Info message", Timestamp = "2000.1.2"},
		new LogLine() {Message = "Debug messageaSAS", LogLevel = MessageSlicer.LogLevel.Debug},
		new LogLine() {Message = "Debug messagea2", LogLevel = MessageSlicer.LogLevel.Debug}
	};
}