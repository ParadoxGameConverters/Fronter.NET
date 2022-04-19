using Fronter.Models.Configuration;
using Fronter.Models.Configuration.Options;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Fronter.ViewModels;
using System.Collections.ObjectModel;

namespace Fronter.ViewModels; 

/// <summary>
///     The PathPickerViewModel lets the user select paths to various stuff the converter needs to know where to find.
/// </summary>
public class PathPickerViewModel : ViewModelBase {
	public PathPickerViewModel(Configuration config) {
		RequiredFolders = new ObservableCollection<RequiredFolder>(config.RequiredFolders.Values);
		RequiredFiles = new ObservableCollection<RequiredFile>(config.RequiredFiles.Values);
	}

	public ObservableCollection<RequiredFolder> RequiredFolders { get; }
	public ObservableCollection<RequiredFile> RequiredFiles { get; }
}