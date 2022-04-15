using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using commonItems;
using FluentAvalonia.Styling;
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
		public static Services.Localization Loc { get; } = new();

		public override void Initialize() {
			File.Delete("log.txt");
			AvaloniaXamlLoader.Load(this);
		}

		public override void OnFrameworkInitializationCompleted() {
			if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
				var mainWindowViewModel = new MainWindowViewModel();
				desktop.MainWindow = new MainWindow {
					DataContext = mainWindowViewModel
				};
				
				mainWindowViewModel.StartWorkerThreads();
			}

			base.OnFrameworkInitializationCompleted();

			LoadTheme();
		}

		private void LoadTheme() {
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
	}
}