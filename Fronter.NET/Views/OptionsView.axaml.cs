using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using commonItems;
using Fronter.Models.Configuration.Options;

namespace Fronter.Views; 

public partial class OptionsView : UserControl {
	public OptionsView() {
		InitializeComponent();
	}

	private void InitializeComponent() {
		AvaloniaXamlLoader.Load(this);
	}

	private void AvaloniaObject_OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e) {
		if (e.Property != ToggleButton.IsCheckedProperty) {
			return;
		}

		if (sender is not RadioButton radioButton) {
			return;
		} 
		
		var context = radioButton.DataContext;
		if (context is not TogglableOption radioOption) {
			return;
		}

		if (radioOption.PendingInitialValue is not null) {
			radioOption.Value = (bool)radioOption.PendingInitialValue;
			radioOption.PendingInitialValue = null;
			return;
		}
		
		radioOption.Value = (bool)(e.NewValue ?? false);
		Logger.Info($"value:  {radioOption.Value}");
	}
}