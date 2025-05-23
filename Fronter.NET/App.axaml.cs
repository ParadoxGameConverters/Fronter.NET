﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using commonItems;
using Fronter.ViewModels;
using Fronter.Views;
using log4net;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

namespace Fronter;

public sealed class App : Application {
	private static readonly ILog logger = LogManager.GetLogger("Frontend");
	private const string FronterThemePath = "Configuration/fronter-theme.txt";
	private const string DefaultTheme = "Dark";

	public override void Initialize() {
		LoggingConfigurator.ConfigureLogging();

		AvaloniaXamlLoader.Load(this);

		_ = Task.Run(LoadTheme);
	}

	public override void OnFrameworkInitializationCompleted() {
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
			var window = MainWindow.Instance;
			desktop.MainWindow = window;

			var mainWindowViewModel = new MainWindowViewModel(window.FindControl<DataGrid>("LogGrid")!);
			window.DataContext = mainWindowViewModel;

			desktop.MainWindow.Opened += (sender, args) => _ = Task.Run(DebugInfo.LogEverything);
			desktop.MainWindow.Opened += (sender, args) => _ = mainWindowViewModel.CheckForUpdatesOnStartup();
		}

		base.OnFrameworkInitializationCompleted();
	}

	private static async Task LoadTheme() {
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
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	public static void SetTheme(string themeName) {
		var app = Application.Current;
		if (app is null) {
			return;
		}

		Dispatcher.UIThread.Post(() => {
			switch (themeName) {
				case "Light":
					if (app.RequestedThemeVariant != ThemeVariant.Light) {
						app.RequestedThemeVariant = ThemeVariant.Light;
					}

					break;
				case "Dark":
					if (app.RequestedThemeVariant != ThemeVariant.Dark) {
						app.RequestedThemeVariant = ThemeVariant.Dark;
					}

					break;
			}
		});
	}

	/// <summary>
	/// Sets and saves a theme
	/// </summary>
	/// <param name="themeName" >Name of the theme to set and save.</param>
	public static async Task SaveTheme(string themeName) {
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