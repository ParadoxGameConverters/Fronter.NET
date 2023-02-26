using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Fronter.ViewModels;

namespace Fronter.Views;

public partial class MainWindow : Window {
	public static MainWindow Instance { get; } = new();

	public MainWindow() {
		InitializeComponent();
#if DEBUG
		this.AttachDevTools();
#endif

		(DataContext as MainWindowViewModel)?.CheckForUpdatesOnStartup();
	}

	private void InitializeComponent() {
		AvaloniaXamlLoader.Load(this);
	}
}