using commonItems;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace Fronter.Extensions;

// based on https://gist.github.com/jakubfijalkowski/0771bfbd26ce68456d3e
public class TranslationSource : INotifyPropertyChanged {
	public static TranslationSource Instance { get; } = new TranslationSource();

	private CultureInfo? currentCulture = null;

	public string? this[string key] => Localization.ResourceManager.GetString(key, currentCulture);

	public CultureInfo? CurrentCulture {
		get => currentCulture;
		set {
			if (currentCulture is not null && currentCulture.Equals(value)) {
				return;
			}
			
			currentCulture = value;

			PropertyChanged?.Invoke(this,new PropertyChangedEventArgs("Item"));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;
}