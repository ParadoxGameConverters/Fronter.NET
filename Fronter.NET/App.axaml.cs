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
	private static readonly string defaultTheme = "FluentLight";
	
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

		IStyle mainStyle = Fluent;
		IStyle dataGridStyle = DataGridFluent;
		
		switch (themeName) {
			case "FluentLight": {
				if (Fluent.Mode != FluentThemeMode.Light) {
					Fluent.Mode = FluentThemeMode.Light;
				}
				mainStyle = Fluent;
				dataGridStyle = DataGridFluent;
				break;
			}
			case "FluentDark": {
				if (Fluent.Mode != FluentThemeMode.Dark) {
					Fluent.Mode = FluentThemeMode.Dark;
				}
				mainStyle = Fluent;
				dataGridStyle = DataGridFluent;
				break;
			}
			case "DefaultLight":
				mainStyle = DefaultLight;
				dataGridStyle = DataGridDefault;
				break;
			case "DefaultDark":
				mainStyle = DefaultDark;
				dataGridStyle = DataGridDefault;
				break;
		}

		if (app.Styles.Count < 2) {
			app.Styles.Insert(0, mainStyle);
			app.Styles.Insert(1, dataGridStyle);
		} else {
			app.Styles[0] = mainStyle;
			app.Styles[1] = dataGridStyle;
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