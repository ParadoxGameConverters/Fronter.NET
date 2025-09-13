using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Fronter.Views;

internal sealed partial class ModsPickerView : UserControl {
	public ModsPickerView() {
		InitializeComponent();
	}

	private void InitializeComponent() {
		AvaloniaXamlLoader.Load(this);
	}
}