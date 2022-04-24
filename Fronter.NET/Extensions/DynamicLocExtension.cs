using Avalonia.Data;
using Fronter.ValueConverters;

namespace Fronter.Extensions;

public class DynamicLocExtension : MultiBinding {
	public DynamicLocExtension(string path) {
		Bindings.Add(new Binding(path));
		
		var binding = new Binding {Path = "CurrentLanguage", Source = TranslationSource.Instance};
		Bindings.Add(binding);

		Converter = new LocKeyToValueConverter();
	}

	// ReSharper disable once UnusedMember.Global
	public MultiBinding ProvideValue() {
		return this;
	}
}