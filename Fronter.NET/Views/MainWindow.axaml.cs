using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Fronter.Views;

public sealed partial class MainWindow : Window {
	public static MainWindow Instance { get; } = new();

	public MainWindow() {
		InitializeComponent();
#if DEBUG
		this.AttachDevTools();
#endif
	}

	private void InitializeComponent() {
		AvaloniaXamlLoader.Load(this);
	}
}