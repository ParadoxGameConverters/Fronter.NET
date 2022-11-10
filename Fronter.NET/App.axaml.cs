﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
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
	private const string FronterThemePath = "Configuration/fronter-theme.txt";
	private const string DefaultTheme = "Dark";

	public override void Initialize() {
		ConfigureLogging();
		
		AvaloniaXamlLoader.Load(this);
		
		LoadTheme();
	}

	public override void OnFrameworkInitializationCompleted() {
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
			var window = MainWindow.Instance;
			desktop.MainWindow = window;
			
			var mainWindowViewModel = new MainWindowViewModel(window.FindControl<DataGrid>("LogGrid")!);
			window.DataContext = mainWindowViewModel;

			desktop.MainWindow.Opened += (sender, args) => mainWindowViewModel.CheckForUpdatesOnStartup();
			desktop.MainWindow.Opened += (sender, args) => DebugInfo.LogEverything();
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
		if (!File.Exists(FronterThemePath)) {
			SetTheme(DefaultTheme);
			return;
		}

		try {
			var themeName = await File.ReadAllTextAsync(FronterThemePath);
			SetTheme(themeName);
		} catch(Exception e) {
			logger.Warn($"Could not load theme; exception: {e.Message}");
			SetTheme(DefaultTheme);
		}
	}
	
	/// <summary>
	/// Sets a theme
	/// </summary>
	/// <param name="themeName">Name of the theme to set.</param>
	public static void SetTheme(string themeName) {
		var app = Application.Current;
		if (app is null) {
			return;
		}
		
		var fluentTheme = (FluentTheme)app.Styles[0];
		switch (themeName) {
			case "Light":
				if (fluentTheme.Mode != FluentThemeMode.Light) {
					fluentTheme.Mode = FluentThemeMode.Light;
				}
				break;
			case "Dark":
				if (fluentTheme.Mode != FluentThemeMode.Dark) {
					fluentTheme.Mode = FluentThemeMode.Dark;
				}
				break;
		}
	}

	/// <summary>
	/// Sets and saves a theme
	/// </summary>
	/// <param name="themeName" >Name of the theme to set and save.</param>
	public static async void SaveTheme(string themeName) {
		SetTheme(themeName);
		try {
			await using var fs = new FileStream(FronterThemePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
			await using var writer = new StreamWriter(fs);
			await writer.WriteAsync(themeName);
			writer.Close();
		} catch (Exception e) {
			logger.Warn($"Could not save theme \"{themeName}\"; exception: {e.Message}");
		}
	}
}