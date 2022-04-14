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
	public LocExtension(string locKey): base($"[{locKey}]") {
		this.Mode = BindingMode.OneWay;
		this.Source = TranslationSource.Instance;
		this.Path = $"[{locKey}]";
	}
	//public LocExtension() {
	//	this.Source = TranslationSource.Instance;
	//}

	//private string locKey;

	//public object ProvideValue() {
	//	return this;
	//}

	public LocExtension ProvideTypedValue() {
		return this;
	}

	public object ProvideValue() {
		return this;
	}
/*
	public Binding ProvideValue(IServiceProvider serviceProvider) {
		var binding = new Binding {
			TypeResolver = (a, b) => typeof(string),
			Path = $"[{locKey}]",
			Mode = BindingMode.OneWay,
			Source = TranslationSource.Instance,

		};

		return binding;
	}
	public Binding ProvideValue() {
		return new Binding {Path=$"[{locKey}]", Mode = BindingMode.OneWay, Source = TranslationSource.Instance};
	}

	private readonly Binding binding;
	*/
}