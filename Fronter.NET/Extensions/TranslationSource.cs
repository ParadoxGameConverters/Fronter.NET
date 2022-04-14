using Fronter.Services;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace Fronter.Extensions;

// based on https://gist.github.com/jakubfijalkowski/0771bfbd26ce68456d3e
public class TranslationSource : INotifyPropertyChanged {
	public static TranslationSource Instance { get; } = new TranslationSource();

	private readonly ResourceManager resManager = Localization.ResourceManager;
	private CultureInfo? currentCulture = null;

	public string? this[string key] => resManager.GetString(key, currentCulture);

	public CultureInfo? CurrentCulture {
		get => currentCulture;
		set {
			if (currentCulture?.Equals(value) == true) {
				return;
			}

			currentCulture = value;
			var @event = PropertyChanged;
			@event?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;
}