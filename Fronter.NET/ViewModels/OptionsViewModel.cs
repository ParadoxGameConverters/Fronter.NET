using Fronter.Models.Configuration.Options;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Fronter.ViewModels; 

public class OptionsViewModel : ViewModelBase {
	public OptionsViewModel(IEnumerable<Option> items) {
		Items = new ObservableCollection<Option>(items);
	}

	public ObservableCollection<Option> Items { get; }
}