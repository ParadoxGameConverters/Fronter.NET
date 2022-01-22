using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Fronter.Views {
	public class PathsView : UserControl {
		public PathsView() {
			InitializeComponent();
		}

		private void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}
	}
}