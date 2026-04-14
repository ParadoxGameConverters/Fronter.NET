using Avalonia;
using Avalonia.Data;
using Fronter.ValueConverters;

namespace Fronter.Extensions;

internal sealed class DynamicLocExtension {
	private readonly string _path;

	public DynamicLocExtension(string path) {
		_path = path;
	}

	public object? FallbackValue { get; set; }

	// ReSharper disable once UnusedMember.Global
	public MultiBinding ProvideValue() {
		var multiBinding = new MultiBinding {
			Converter = new LocKeyToValueConverter(),
			FallbackValue = FallbackValue ?? AvaloniaProperty.UnsetValue
		};
		multiBinding.Bindings.Add(new Binding(_path));
		multiBinding.Bindings.Add(new Binding { Path = "CurrentLanguage", Source = TranslationSource.Instance });
		return multiBinding;
	}
}