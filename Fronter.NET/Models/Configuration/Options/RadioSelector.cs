using commonItems;
using log4net;
using System.Collections.Generic;
using System.Linq;

namespace Fronter.Models.Configuration.Options;

internal sealed class RadioSelector {
	private static readonly ILog logger = LogManager.GetLogger("Radio selector");
	public RadioSelector(BufferedReader reader) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("radioOption", reader => {
			++optionCounter;
			var newOption = new ToggleableOption(reader, optionCounter);
			RadioOptions.Add(newOption);
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}

	public string GetSelectedValue() {
		foreach (ToggleableOption option in RadioOptions.Where(option => option.Value)) {
			return option.Name;
		}
		return string.Empty;
	}

	public int GetSelectedId() {
		return RadioOptions.Where(option => option.Value).Select(option => option.Id).FirstOrDefault();
	}

	// helper returning the option marked default (pending initial value)
	// or the first option if none has that flag.
	private ToggleableOption? GetDefaultOption() {
		return RadioOptions.FirstOrDefault(opt => opt.PendingInitialValue == true)
		       ?? RadioOptions.FirstOrDefault();
	}

	public void SetSelectedId(int selection) {
		var isSet = false;
		foreach (var option in RadioOptions) {
			if (option.Id == selection) {
				option.Value = true;
				isSet = true;
			} else {
				option.Value = false;
			}
		}

		if (!isSet) {
			var def = GetDefaultOption();
			if (def is not null) {
				foreach (var option in RadioOptions) {
					option.Value = option == def;
				}
				logger.Warn($"Attempted setting a radio selector ID that does not exist! Falling back to default '{def.Name}' (id {def.Id}).");
			} else {
				logger.Warn("Attempted setting a radio selector ID that does not exist and no default option is available!");
			}
		}
	}
	public void SetSelectedValue(string selection) {
		var isSet = false;
		foreach (var option in RadioOptions) {
			if (option.Name.Equals(selection)) {
				option.Value = true;
				isSet = true;
			} else {
				option.Value = false;
			}
		}
		if (!isSet) {
			var def = GetDefaultOption();
			if (def is not null) {
				foreach (var option in RadioOptions) {
					option.Value = option == def;
				}
				logger.Debug($"Attempted setting a radio selector value that does not exist! Falling back to default '{def.Name}' (id {def.Id}).");
			} else {
				logger.Warn("Attempted setting a radio selector value that does not exist and no default option is available!");
			}
		}
	}

	public ToggleableOption? SelectedOption {
		get => RadioOptions.FirstOrDefault(option => option!.Value, defaultValue: null);
		set {
			foreach (var option in RadioOptions) {
				option.Value = option == value;
			}
		}
	}

	private int optionCounter = 0;
	public List<ToggleableOption> RadioOptions { get; } = [];
}