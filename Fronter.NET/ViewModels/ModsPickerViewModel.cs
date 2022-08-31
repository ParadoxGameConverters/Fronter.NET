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
		Mods = new ObservableCollection<Mod>(config.AutoLocatedMods);
		
		if (config.ModAutoGenerationSource is null) {
			TabText = "MODSDISABLED";
			return;
		}

		if (Mods.Count == 0) {
			TabText = "MODSNOTFOUND";
			return;
		}

		TabText = "MODSFOUND";
	}

	public ObservableCollection<Mod> Mods { get; }
	public string? TabText { get; private set; } = null;
}