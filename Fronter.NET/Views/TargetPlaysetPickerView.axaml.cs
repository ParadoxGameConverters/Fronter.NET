using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Fronter.Views;

internal sealed partial class TargetPlaysetPickerView : UserControl {
	public TargetPlaysetPickerView() {
		InitializeComponent();
	}

	private void InitializeComponent() {
		AvaloniaXamlLoader.Load(this);
	}
}