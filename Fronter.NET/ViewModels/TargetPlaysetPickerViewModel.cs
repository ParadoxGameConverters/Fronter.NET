using DynamicData;
using DynamicData.Binding;
using Fronter.Models.Configuration;
using Fronter.Models.Database;
using System;
using System.Collections.ObjectModel;

namespace Fronter.ViewModels;

/// <summary>
///     The TargetPlaysetPickerViewModel lets the user select paths to various stuff the converter needs to know where to find.
/// </summary>
internal class TargetPlaysetPickerViewModel : ViewModelBase {
	private Config config;
	
	public TargetPlaysetPickerViewModel(Config config) {
		this.config = config;
		
		config.AutoLocatedPlaysets.ToObservableChangeSet()
			.Bind(out targetPlaysets)
			.Subscribe();

		if (!config.TargetPlaysetSelectionEnabled) {
			TabDisabled = true;
		}
	}

	private readonly ReadOnlyObservableCollection<Playset> targetPlaysets;
	public ReadOnlyObservableCollection<Playset> TargetPlaysets => targetPlaysets;

	public bool TabDisabled { get; } = false;

	public void ReloadPlaysets() {
		config.SelectedPlayset = null;
		config.AutoLocatePlaysets();
	}
	
	public Playset? SelectedPlayset {
		get => config.SelectedPlayset;
		set => config.SelectedPlayset = value;
	}
}