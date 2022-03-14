using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Fronter.Views;

public class LogView : UserControl {
	public LogView() {
		InitializeComponent();
	}

	private void InitializeComponent() {
		AvaloniaXamlLoader.Load(this);
	}
}