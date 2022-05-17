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
	
	public static readonly StyleInclude DataGridFluent = new(new Uri("avares://Fronter/Styles")) {
        Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml")
    };

    public static readonly StyleInclude DataGridDefault = new(new Uri("avares://Fronter/Styles")) {
        Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Default.xaml")
    };

    public static readonly FluentTheme Fluent = new(new Uri("avares://Fronter/Styles"));

    public static readonly Styles DefaultLight = new() {
        new StyleInclude(new Uri("resm:Styles?assembly=Fronter")) {
            Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/AccentColors.xaml")
        },
        new StyleInclude(new Uri("resm:Styles?assembly=Fronter")) {
            Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/Base.xaml")
        },
        new StyleInclude(new Uri("resm:Styles?assembly=Fronter")) {
            Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/BaseLight.xaml")
        },
        new StyleInclude(new Uri("resm:Styles?assembly=Fronter")) {
            Source = new Uri("avares://Avalonia.Themes.Default/Accents/BaseLight.xaml")
        },
        new StyleInclude(new Uri("resm:Styles?assembly=Fronter")) {
            Source = new Uri("avares://Avalonia.Themes.Default/DefaultTheme.xaml")
        }
    };

    public static readonly Styles DefaultDark = new() {
        new StyleInclude(new Uri("resm:Styles?assembly=Fronter")) {
            Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/AccentColors.xaml")
        },
        new StyleInclude(new Uri("resm:Styles?assembly=Fronter")) {
            Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/Base.xaml")
        },
        new StyleInclude(new Uri("resm:Styles?assembly=Fronter")) {
            Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/BaseDark.xaml")
        },
        new StyleInclude(new Uri("resm:Styles?assembly=Fronter")) {
            Source = new Uri("avares://Avalonia.Themes.Default/Accents/BaseDark.xaml")
        },
        new StyleInclude(new Uri("resm:Styles?assembly=Fronter")) {
            Source = new Uri("avares://Avalonia.Themes.Default/DefaultTheme.xaml")
        }
    };
	
	public override void Initialize() {
		ConfigureLogging();
		
		Styles.Insert(0, Fluent);
		Styles.Insert(1, DataGridFluent);

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

		LoadTheme();
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
			return;
		}

		try {
			var themeName = await File.ReadAllTextAsync(fronterThemePath);
			SetTheme(themeName);
		} catch(Exception e) {
			logger.Warn($"Could not load theme; exception: {e.Message}");
		}
	}

	public static void SetTheme(string themeName) {
		var app = Application.Current;
		if (app is null) {
			return;
		}
		switch (themeName) {
			case "FluentLight": {
				if (Fluent.Mode != FluentThemeMode.Light) {
					Fluent.Mode = FluentThemeMode.Light;
				}
				app.Styles[0] = Fluent;
				app.Styles[1] = DataGridFluent;
				break;
			}
			case "FluentDark": {
				if (Fluent.Mode != FluentThemeMode.Dark) {
					Fluent.Mode = FluentThemeMode.Dark;
				}
				app.Styles[0] = Fluent;
				app.Styles[1] = DataGridFluent;
				break;
			}
			case "DefaultLight":
				app.Styles[0] = DefaultLight;
				app.Styles[1] = DataGridDefault;
				break;
			case "DefaultDark":
				app.Styles[0] = DefaultDark;
				app.Styles[1] = DataGridDefault;
				break;
		}
		SaveTheme(themeName);
	}

	private static async void SaveTheme(string themeName) {
		await using var fs = new FileStream(fronterThemePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
		await using var writer = new StreamWriter(fs);
		await writer.WriteAsync(themeName);
		writer.Close();
	}
}