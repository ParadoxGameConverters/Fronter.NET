using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Fronter.NET.Views
{
    public class MarkdownView : UserControl
    {
        public MarkdownView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}