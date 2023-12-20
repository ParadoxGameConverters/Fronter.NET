using DynamicData;
using DynamicData.Binding;
using Fronter.Models.Configuration;
using Fronter.Models.Database;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Fronter.ViewModels;

/// <summary>
///     The TargetPlaysetPickerViewModel lets the user select paths to various stuff the converter needs to know where to find.
/// </summary>
public class TargetPlaysetPickerViewModel : ViewModelBase {
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
		selectedPlaysetId = string.Empty;
		config.AutoLocatePlaysets();
	}

	private string selectedPlaysetId = string.Empty;
	
	public Playset? SelectedPlayset {
		get => TargetPlaysets.FirstOrDefault(p => p.Id == selectedPlaysetId);
		set {
			if (value is null) {
				return;
			}
			selectedPlaysetId = value.Id;
		}
	}
}