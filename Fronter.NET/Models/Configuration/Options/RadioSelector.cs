using commonItems;
using System.Collections.Generic;
using System.Linq;

namespace Fronter.Models.Configuration.Options;

public class RadioSelector : Selector {
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
		foreach (ToggleableOption option in RadioOptions.Where(option => option.Value)) {
			return option.Id;
		}
		return 0;
	}

	public void SetSelectedId(int selection) {
		var isSet = false;
		foreach (var option in RadioOptions) {
			if (option.Id == selection) {
				option.Value = true;
				isSet = true;
			} else
				option.Value = false;
		}

		if (!isSet) {
			Logger.Warn("Attempted setting a radio selector ID that does not exist!");
		}
	}
	public void SetSelectedValue(string selection) {
		var isSet = false;
		foreach (var option in RadioOptions) {
			if (option.Name == selection) {
				option.Value = true;
				isSet = true;
			} else
				option.Value = false;
		}
		if (!isSet)
			Logger.Warn("Attempted setting a radio selector value that does not exist!");
	}

	public ToggleableOption? SelectedOption {
		get => RadioOptions.FirstOrDefault(option => option!.Value, null);
		set {
			foreach (var option in RadioOptions) {
				option.Value = option == value;
			}
			
			//todo: remove debug loop
			foreach (var option in RadioOptions) {
				Logger.Info($"========================================");
				Logger.Info($"RADIOSELECTOR VALUE CHANGED: {value.DisplayName}");
				Logger.Debug($"{option.DisplayName}: {option.Value}");
			}
		}
	}

	private int optionCounter = 0;
	public List<ToggleableOption> RadioOptions { get; } = new();
}