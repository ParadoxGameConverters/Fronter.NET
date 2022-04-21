using commonItems;
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
		RequiredFolders = new ObservableCollection<RequiredFolder>(config.RequiredFolders);
		RequiredFiles = new ObservableCollection<RequiredFile>(config.RequiredFiles);

		foreach (RequiredFolder requiredFolder in RequiredFolders) {
			Logger.Error(requiredFolder.DisplayName);
			Logger.Warn(requiredFolder.Name);
			Logger.Info(requiredFolder.Tooltip);
		}

		foreach (RequiredFile requiredFile in RequiredFiles) {
			Logger.Error(requiredFile.DisplayName);
			Logger.Warn(requiredFile.Name);
			Logger.Info(requiredFile.Tooltip);
		}
	}

	public ObservableCollection<RequiredFolder> RequiredFolders { get; }
	public ObservableCollection<RequiredFile> RequiredFiles { get; }
}