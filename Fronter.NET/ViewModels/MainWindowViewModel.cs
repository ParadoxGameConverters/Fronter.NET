using Avalonia.Controls;
using commonItems;
using Fronter.Models.Configuration;
using Fronter.Services;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.ViewModels;
using ReactiveUI;
using System.Threading.Tasks;

namespace Fronter.ViewModels {
	public class MainWindowViewModel : ViewModelBase {
		public string Greeting => "Welcome to Avalonia!";
	}
}