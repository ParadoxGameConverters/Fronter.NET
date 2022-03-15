using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace Fronter.Views;

public class LogView : UserControl {
	public LogView() {
		InitializeComponent();
	}

	private void InitializeComponent() {
		AvaloniaXamlLoader.Load(this);
	}

	private void StyledElement_OnDataContextChanged(object? sender, EventArgs e) {
		throw new NotImplementedException();
	}
}