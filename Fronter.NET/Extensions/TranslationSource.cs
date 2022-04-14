using commonItems;
using Fronter.Services;
using ReactiveUI;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace Fronter.Extensions;

// based on https://gist.github.com/jakubfijalkowski/0771bfbd26ce68456d3e
public class TranslationSource : ReactiveObject {
	public TranslationSource() {

		this.PropertyChanged += (sender, args) => {
			LangCHangedCounter++;
			//	Logger.Notice($"|| {sender} {args.PropertyName}");
		};
	}
	
	public int LangCHangedCounter { get; private set; } = 0; // TODO: REMOVE DEBUG
	public static TranslationSource Instance { get; } = new TranslationSource();

	private readonly ResourceManager resManager = Localization.ResourceManager;
	private CultureInfo? currentCulture = null;

	public string? this[string key] {
		get { var DEBUGVALUE = resManager.GetString(key, currentCulture); Logger.Info("??  " + DEBUGVALUE.ToString());
			return DEBUGVALUE;
		}
	}
	private int counter = 0;

	public CultureInfo? CurrentCulture {
		get => currentCulture;
		set {
			this.currentCulture = value;
			this.RaisePropertyChanged(string.Empty);
			//this.RaisePropertyChanged(String.Empty);
			//this.RaiseAndSetIfChanged(ref currentCulture, value);
			Logger.Progress("== " + counter++ + " " + currentCulture + "      " + resManager.GetString("PATHSTAB", currentCulture));
		}
	}
}