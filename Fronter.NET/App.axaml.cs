using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using commonItems;
using Fronter.Models.Configuration;
using Fronter.Services;
using Fronter.ViewModels;
using Fronter.Views;
using MessageBox.Avalonia.DTO;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Fronter {
	public class App : Application {
		public override void Initialize() {
			File.Delete("log.txt");
			AvaloniaXamlLoader.Load(this);
		}

		public override void OnFrameworkInitializationCompleted() {
			if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
				var mainWindow = new MainWindow {
					DataContext = new MainWindowViewModel(),
				};
				desktop.MainWindow = mainWindow;
			}

			base.OnFrameworkInitializationCompleted();
		}
	}
}