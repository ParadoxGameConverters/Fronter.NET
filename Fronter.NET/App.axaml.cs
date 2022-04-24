using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using commonItems;
using FluentAvalonia.Styling;
using Fronter.ViewModels;
using Fronter.Views;
using System.IO;

namespace Fronter;

public class App : Application {
	public override void Initialize() {
		Logger.Configure("log4net_Fronter.config");
		
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted() {
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
			var window = MainWindow.Instance;
			desktop.MainWindow = window;
			var mainWindowViewModel = new MainWindowViewModel();
			window.DataContext = mainWindowViewModel;
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