using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Fronter.Views;

internal sealed partial class OptionsView : UserControl {
	public OptionsView() {
		InitializeComponent();
	}

	private void InitializeComponent() {
		AvaloniaXamlLoader.Load(this);
	}
}