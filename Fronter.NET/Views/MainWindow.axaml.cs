using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using commonItems;
using Fronter.Models.Configuration;
using Fronter.Services;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Models;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Fronter.Views {
	public partial class MainWindow : Window {
		public MainWindow() {
			InitializeComponent();
#if DEBUG
			this.AttachDevTools();
#endif
		}

		private void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}


		public async void InitializeFrontend() {
			var configuration = new Configuration();
			var loc = new Localization();

			// check for updates on startup
			
			Logger.Debug($"{nameof(configuration.UpdateCheckerEnabled)}: {configuration.UpdateCheckerEnabled}");
			Logger.Debug($"{nameof(configuration.CheckForUpdatesOnStartup)}: {configuration.CheckForUpdatesOnStartup}");
			Logger.Debug($"is update available: {UpdateChecker.IsUpdateAvailable("commit_id.txt", configuration.PagesCommitIdUrl)}");
			if (configuration.UpdateCheckerEnabled &&
			    configuration.CheckForUpdatesOnStartup &&
			    UpdateChecker.IsUpdateAvailable("commit_id.txt", configuration.PagesCommitIdUrl)) {
				var info = UpdateChecker.GetLatestReleaseInfo(configuration.Name);
				
				
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
				var result = await messageBoxWindow.ShowDialog(this);
				Logger.Progress(result);
				if (result == updateNow) {
					if (info.ZipUrl is not null) {
						UpdateChecker.StartUpdaterAndDie(info.ZipUrl, configuration.ConverterFolder);
					} else {
						Process.Start(new ProcessStartInfo
						{
							FileName = configuration.ConverterReleaseForumThread,
							UseShellExecute = true
						});
						Process.Start(new ProcessStartInfo
						{
							FileName = configuration.LatestGitHubConverterReleaseUrl,
							UseShellExecute = true
						});
					}
				}
			}

			// TODO: REMOVE LAUNCHER FROM HERE
			//var converterLauncher = new ConverterLauncher();
			//converterLauncher.LoadConfiguration(configuration);
			//converterLauncher.LaunchConverter(); // TODO: REENABLE
		}
		
		private async void MsBoxCustomImage_Click(object sender, RoutedEventArgs e) {
			InitializeFrontend();
		}
	}
}