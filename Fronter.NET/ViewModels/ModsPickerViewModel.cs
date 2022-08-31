using Avalonia.Controls;
using commonItems;
using Fronter.Extensions;
using Fronter.Models.Configuration;
using Fronter.Views;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Fronter.ViewModels;

/// <summary>
///     The ModsPickerViewModel lets the user select paths to various stuff the converter needs to know where to find.
/// </summary>
public class ModsPickerViewModel : ViewModelBase {
	public ModsPickerViewModel(Configuration config) {
		if (config.ModAutoGenerationSource is null) {
			TabText = "MODSDISABLED";
			return;
		}
		if (config.auto)
	}

	public ObservableCollection<RequiredFolder> RequiredFolders { get; }
	public ObservableCollection<RequiredFile> RequiredFiles { get; }
	public string? TabText { get; private set; } = null;
}