using DynamicData;
using DynamicData.Binding;
using Fronter.Models.Configuration;
using System;
using System.Collections.ObjectModel;

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
}