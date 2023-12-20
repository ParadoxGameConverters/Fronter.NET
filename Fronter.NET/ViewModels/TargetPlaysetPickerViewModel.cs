using DynamicData;
using DynamicData.Binding;
using Fronter.Models.Configuration;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Fronter.ViewModels;

/// <summary>
///     The TargetPlaysetPickerViewModel lets the user select paths to various stuff the converter needs to know where to find.
/// </summary>
public class TargetPlaysetPickerViewModel : ViewModelBase {
	private Config _config;
	
	public TargetPlaysetPickerViewModel(Config config) {
		_config = config;
		
		config.AutoLocatedPlaysets.ToObservableChangeSet()
			.Bind(out targetPlaysets)
			.Subscribe();

		if (!config.TargetPlaysetSelectionEnabled) {
			TabDisabled = true;
		}
	}

	private readonly ReadOnlyObservableCollection<TargetPlayset> targetPlaysets;
	public ReadOnlyObservableCollection<TargetPlayset> TargetPlaysets => targetPlaysets;

	public bool TabDisabled { get; } = false;

	public void ReloadPlaysets() {
		selectedPlaysetID = string.Empty;
		_config.AutoLocatePlaysets();
	}

	private string selectedPlaysetID = string.Empty;
	
	public TargetPlayset? SelectedPlayset {
		get => TargetPlaysets.FirstOrDefault(p => p.Id == selectedPlaysetID);
		set {
			if (value is null) {
				return;
			}
			selectedPlaysetID = value.Id;
		}
	}
}