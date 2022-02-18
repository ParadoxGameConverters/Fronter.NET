using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Fronter.Services;
using Fronter.ViewModels;
using Fronter.Views;

namespace Fronter {
	public class App : Application {
		public override void Initialize() {
			AvaloniaXamlLoader.Load(this);



			// TODO: REMOVE LAUNCHER FROM HERE
			new ConverterLauncher().LaunchConverter();
		}

		public override void OnFrameworkInitializationCompleted() {
			if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
				desktop.MainWindow = new MainWindow {
					DataContext = new MainWindowViewModel(),
				};
			}

			base.OnFrameworkInitializationCompleted();
		}
	}
}