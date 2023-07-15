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
	public TargetPlaysetPickerViewModel(Configuration config) {
		config.TargetPlaysets.ToObservableChangeSet()
			.Bind(out targetPlaysets)
			.Subscribe();

		if (config.TargetPlaysetsSource is null) {
			TabDisabled = true;
		}
	}

	private readonly ReadOnlyObservableCollection<TargetPlayset> targetPlaysets;
	public ReadOnlyObservableCollection<TargetPlayset> TargetPlaysets => targetPlaysets;

	public bool TabDisabled { get; } = false;
}