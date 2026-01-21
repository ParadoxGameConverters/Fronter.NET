using DynamicData;
using DynamicData.Binding;
using Fronter.Models.Configuration;
using Fronter.Models.Database;
using log4net;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Fronter.ViewModels;

/// <summary>
///     The TargetPlaysetPickerViewModel lets the user select paths to various stuff the converter needs to know where to find.
/// </summary>
internal class TargetPlaysetPickerViewModel : ViewModelBase {
	private readonly Config config;
	private readonly ILog logger = LogManager.GetLogger("Mod copier");
	
	public TargetPlaysetPickerViewModel(Config config) {
		this.config = config;
		
		config.AutoLocatedPlaysets.ToObservableChangeSet()
			.Bind(out targetPlaysets)
			.Subscribe();

		// Keep a ComboBox-friendly list in sync, but with an extra leading null entry ("no selection").
		config.AutoLocatedPlaysets.ToObservableChangeSet()
			.ToCollection()
			.Subscribe(playsets => {
				TargetPlaysetsWithBlank = new(new Playset?[] { null }.Concat(playsets));
			});

		if (!config.TargetPlaysetSelectionEnabled) {
			TabDisabled = true;
		}
	}

	private readonly ReadOnlyObservableCollection<Playset> targetPlaysets;
	public ReadOnlyObservableCollection<Playset> TargetPlaysets => targetPlaysets;

	public ObservableCollection<Playset?> TargetPlaysetsWithBlank { get; private set; } = [];

	public bool TabDisabled { get; } = false;

	public void ReloadPlaysets() {
		config.SelectedPlayset = null;
		config.AutoLocatePlaysets();
	}

	public Playset? SelectedPlayset {
		get => config.SelectedPlayset;
		set {
			config.SelectedPlayset = value;

			if (value is null) {
				logger.Info("Unset the target playset.");
			} else {
				logger.Info("Set the target playset to \"" + value.Name + "\".");
			}
		}
	}
}