using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Fronter.Views;

public sealed partial class PathPickerView : UserControl {
	public PathPickerView() {
		InitializeComponent();
	}

	private void InitializeComponent() {
		AvaloniaXamlLoader.Load(this);
	}
}