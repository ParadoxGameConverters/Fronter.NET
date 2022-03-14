using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using commonItems;
using Fronter.Models.Configuration;
using Fronter.Services;
using Fronter.Views;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.ViewModels;
using ReactiveUI;
using System.Threading.Tasks;
using commonItems;
using Fronter.Models.Configuration;
using Fronter.Services;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using MessageBox.Avalonia.Models;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Fronter.ViewModels; 

public class MainWindowViewModel : ViewModelBase {
	public string Greeting => "Welcome to Avalonia!";
	public IEnumerable<string> LanguageNames => loc.LoadedLanguages.Select(l=>loc.TranslateLanguage(l));

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
	
	public async void SetLanguage() {
		var languages = loc.LoadedLanguages;
	}
}