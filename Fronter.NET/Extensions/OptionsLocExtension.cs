using Avalonia.Data;
using Fronter.ValueConverters;

namespace Fronter.Extensions;

public class OptionsLocExtension : MultiBinding {
	public OptionsLocExtension(string path) {
		Bindings.Add(new Binding(path));
		
		var binding = new Binding {Path = "CurrentLanguage", Source = TranslationSource.Instance};
		Bindings.Add(binding);

		Converter = new LocKeyToValueConverter();
	}

	public MultiBinding ProvideValue() {
		return this;
	}
}