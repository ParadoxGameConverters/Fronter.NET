using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Markup.Xaml;
using AvaloniaEdit.Utils;
using ReactiveUI;
using System;
using System.ComponentModel;

namespace Fronter.Extensions;

// based on https://gist.github.com/jakubfijalkowski/0771bfbd26ce68456d3e
public class LocExtension : Binding {
	public LocExtension(string locKey): base($"[{locKey}]", BindingMode.OneWay) {
		Source = TranslationSource.Instance;
	}

	public Binding ProvideValue() {
		return this;
	}
}