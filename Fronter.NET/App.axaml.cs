using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Fronter.ViewModels;
using Fronter.Views;
using Material.Dialog;
using Material.Styles.Themes;
using Material.Styles.Themes.Base;
using Newtonsoft.Json;
using System;
using System.IO;
using Color = Avalonia.Media.Color;

namespace Fronter;

public class App : Application {
	public override void Initialize() {
		File.Delete("log.txt");
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted() {
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
			var mainWindowViewModel = new MainWindowViewModel();
			desktop.MainWindow = new MainWindow {DataContext = mainWindowViewModel};

			mainWindowViewModel.StartWorkerThreads();
		}

		base.OnFrameworkInitializationCompleted();

		LoadTheme();
	}

	private void LoadTheme() {
		var themeBootstrap = this.LocateMaterialTheme<MaterialThemeBase>();
		themeBootstrap.CurrentTheme = LoadOrCreateDefaultTheme();

		themeBootstrap.CurrentThemeChanged.Subscribe(newTheme => {
			var configText = JsonConvert.SerializeObject(newTheme);
			File.WriteAllText("theme-config.json", configText);
		});
	}
	private static ITheme LoadOrCreateDefaultTheme() {
		try {
			var text = File.ReadAllText("theme-config.json");
			var loadedTheme = JsonConvert.DeserializeObject<Theme>(text);
			return loadedTheme ?? CreateTheme(BaseThemeMode.Inherit);
		} catch (Exception) {
			// In case of any exception or file missing, etc
			// Fallback to creating default theme
			return CreateTheme(BaseThemeMode.Inherit);
		}
	}

	// colors based on https://material.io/design/color/the-color-system.html
	private static readonly Color PrimaryColor = Color.FromRgb(255, 106, 0);
	private static readonly Color SecondaryColor = Color.FromRgb(216, 90, 0);

	public static ITheme CreateTheme(BaseThemeMode mode) {
		return Theme.Create(mode.GetBaseTheme(), PrimaryColor, SecondaryColor);
	}
}