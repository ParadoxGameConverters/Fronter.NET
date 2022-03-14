using Fronter.Models;
using System.Collections.ObjectModel;

namespace Fronter.ViewModels; 

public class LogViewModel : ViewModelBase {
	public ObservableCollection<LogLine> LogLines { get; set; }
}