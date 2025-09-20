using Avalonia.Data;

namespace Fronter.Extensions;

// based on https://gist.github.com/jakubfijalkowski/0771bfbd26ce68456d3e
internal sealed class LocExtension : Binding {
	public LocExtension(string locKey): base($"[{locKey}]", BindingMode.OneWay) {
		Source = TranslationSource.Instance;
	}

	public Binding ProvideValue() {
		return this;
	}
}