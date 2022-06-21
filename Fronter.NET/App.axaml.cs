using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using commonItems;
using Fronter.ViewModels;
using Fronter.Views;
using log4net;
using log4net.Config;
using System;
using System.IO;

namespace Fronter;

public class App : Application {
	private static readonly ILog logger = LogManager.GetLogger("Frontend");
	private static readonly string fronterThemePath = Path.Combine("Configuration", "fronter-theme.txt");
	private static readonly string defaultTheme = "Light";
	
	public static readonly StyleInclude DataGridFluent = new(new Uri("avares://Fronter/Styles")) {
        Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml")
    };
	public static readonly FluentTheme Fluent = new(new Uri("avares://Fronter/Styles"));
    
	public override void Initialize() {
		ConfigureLogging();
		
		LoadTheme();

		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted() {
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
			var window = MainWindow.Instance;
			desktop.MainWindow = window;
			
			var mainWindowViewModel = new MainWindowViewModel(window.FindControl<DataGrid>("LogGrid"));
			window.DataContext = mainWindowViewModel;

			desktop.MainWindow.Opened += (sender, args) => mainWindowViewModel.CheckForUpdatesOnStartup();
		}

		base.OnFrameworkInitializationCompleted();
	}

	public static void ConfigureLogging() {
		var repository = LogManager.GetRepository();

		// add custom "PROGRESS" level
		repository.LevelMap.Add(LogExtensions.ProgressLevel);

		// configure log4net
		var logConfiguration = new FileInfo("log4net_Fronter.config");
		XmlConfigurator.Configure(logConfiguration);
	}

	private static async void LoadTheme() {
		if (!File.Exists(fronterThemePath)) {
			SetTheme(defaultTheme);
			return;
		}

		try {
			var themeName = await File.ReadAllTextAsync(fronterThemePath);
			SetTheme(themeName);
		} catch(Exception e) {
			logger.Warn($"Could not load theme; exception: {e.Message}");
			SetTheme(defaultTheme);
		}
	}
	
	/// <summary>
	/// Sets a theme
	/// </summary>
	/// <param name="themeName"></param>
	public static void SetTheme(string themeName) {
		var app = Application.Current;
		if (app is null) {
			return;
		}
		
		switch (themeName) {
			case "Light":
				if (Fluent.Mode != FluentThemeMode.Light) {
					Fluent.Mode = FluentThemeMode.Light;
				}
				break;
			case "Dark":
				if (Fluent.Mode != FluentThemeMode.Dark) {
					Fluent.Mode = FluentThemeMode.Dark;
				}
				break;
		}

		if (app.Styles.Count < 2) {
			app.Styles.Insert(0, Fluent);
			app.Styles.Insert(1, DataGridFluent);
		}
	}

	/// <summary>
	/// Sets and saves a theme
	/// </summary>
	/// <param name="themeName"></param>
	public static async void SaveTheme(string themeName) {
		SetTheme(themeName);
		try {
			await using var fs = new FileStream(fronterThemePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
			await using var writer = new StreamWriter(fs);
			await writer.WriteAsync(themeName);
			writer.Close();
		} catch (Exception e) {
			logger.Warn($"Could not save theme \"{themeName}\"; exception: {e.Message}");
		}
	}
}