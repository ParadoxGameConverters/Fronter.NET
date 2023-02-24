using commonItems;
using log4net;
using System.Collections.Generic;
using System.Linq;

namespace Fronter.Models.Configuration.Options;

public class RadioSelector : Selector {
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
			logger.Warn("Attempted setting a radio selector ID that does not exist!");
		}
	}
	public void SetSelectedValue(string selection) {
		var isSet = false;
		foreach (var option in RadioOptions) {
			if (option.Name == selection) {
				option.Value = true;
				isSet = true;
			} else {
				option.Value = false;
			}
		}
		if (!isSet)
			logger.Warn("Attempted setting a radio selector value that does not exist!");
	}

	public ToggleableOption? SelectedOption {
		get => RadioOptions.FirstOrDefault(option => option!.Value, null);
		set {
			foreach (var option in RadioOptions) {
				option.Value = option == value;
			}
		}
	}

	private int optionCounter = 0;
	public List<ToggleableOption> RadioOptions { get; } = new();
}