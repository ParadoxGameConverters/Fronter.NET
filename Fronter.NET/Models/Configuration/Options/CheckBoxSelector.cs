﻿using commonItems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fronter.Models.Configuration.Options;

internal sealed class CheckBoxSelector {
	public CheckBoxSelector(BufferedReader reader) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("checkBoxOption", reader => {
			++optionCounter;
			var newOption = new ToggleableOption(reader, optionCounter);
			CheckBoxOptions.Add(newOption);
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}

	public HashSet<string> GetSelectedValues() {
		var toReturn = new HashSet<string>(StringComparer.Ordinal);
		foreach (ToggleableOption option in CheckBoxOptions.Where(option => option.Value)) {
			toReturn.Add(option.Name);
		}
		return toReturn;
	}

	public HashSet<int> GetSelectedIds() {
		var toReturn = new HashSet<int>();
		foreach (ToggleableOption option in CheckBoxOptions.Where(option => option.Value)) {
			toReturn.Add(option.Id);
		}
		return toReturn;
	}

	public void SetSelectedIds(ISet<int> selection) {
		foreach (var option in CheckBoxOptions) {
			option.Value = selection.Contains(option.Id);
		}
	}
	public void SetSelectedValues(ISet<string> selection) {
		foreach (var option in CheckBoxOptions) {
			option.Value = selection.Contains(option.Name);
		}
	}

	private int optionCounter = 0;
	public bool Preloaded { get; set; } = false;
	public List<ToggleableOption> CheckBoxOptions { get; } = [];
}