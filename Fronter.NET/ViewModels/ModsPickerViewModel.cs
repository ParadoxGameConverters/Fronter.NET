using DynamicData;
using DynamicData.Binding;
using Fronter.Models.Configuration;
using System;
using System.Collections.ObjectModel;

namespace Fronter.ViewModels;

/// <summary>
///     The ModsPickerViewModel lets the user select paths to various stuff the converter needs to know where to find.
/// </summary>
internal sealed class ModsPickerViewModel : ViewModelBase {
	public ModsPickerViewModel(Config config) {
		config.AutoLocatedMods.ToObservableChangeSet()
			.Bind(out autoLocatedMods)
			.Subscribe();

		if (config.ModAutoGenerationSource is null) {
			ModsDisabled = true;
		}
	}

	private readonly ReadOnlyObservableCollection<Mod> autoLocatedMods;
	public ReadOnlyObservableCollection<Mod> AutoLocatedMods => autoLocatedMods;

	public bool ModsDisabled { get; } = false;
}