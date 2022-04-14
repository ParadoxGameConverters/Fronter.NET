using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Markup.Xaml;
using AvaloniaEdit.Utils;
using System;

namespace Fronter.Extensions;

// based on https://gist.github.com/jakubfijalkowski/0771bfbd26ce68456d3e
public class LocExtension : Binding {
	public LocExtension(string name) : base($"[{name}]") {
		binding = new Binding($"[{name}]") {Mode = BindingMode.OneWay, Source = Extensions.TranslationSource.Instance};
	}

	private Binding binding;

	public Binding ProvideValue() {
		return binding;
	}
}