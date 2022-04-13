using commonItems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Fronter.Services;

public class Localization {
	public Localization() {
		var languagesPath = Path.Combine("Configuration", "converter_languages.yml");
		if (!File.Exists(languagesPath)) {
			Logger.Error("No localization found!");
			return;
		}

		using var fileStream = File.OpenRead(languagesPath);
		using var reader = new StreamReader(fileStream);
		while (!reader.EndOfStream) {
			var line = reader.ReadLine();
			if (line is null) {
				break;
			}
			var pos = line.IndexOf(':');
			if (pos == -1){
				continue;
			}
			var language = line.Substring(2, pos - 2);
			pos = line.IndexOf('\"');
			var secpos = line.LastIndexOf('\"');
			var langText = line.Substring(pos + 1, secpos - pos - 1);
			languages.Add(language, langText);
			LoadedLanguages.Add(language);
		}
		LoadLanguages();

		var fronterLanguagePath = Path.Combine("Configuration", "fronter-language.txt");
		if (File.Exists(fronterLanguagePath)) {
			var parser = new Parser();
			parser.RegisterKeyword("language", reader => SetLanguage = reader.GetString());
			parser.ParseFile(fronterLanguagePath);
		}
	}

	public string Translate(string key) {
		string toReturn;
		if (!translations.ContainsKey(key)) {
			return string.Empty;
		}

		if (translations[key].ContainsKey(SetLanguage)) {
			toReturn = translations[key][SetLanguage];
		}
		else if (translations[key].ContainsKey("english")) {
			Logger.Debug($"{SetLanguage} localization not found for key {key}, using english one");
			toReturn = translations[key]["english"];
		} else {
			Logger.Debug($"{SetLanguage} localization not found for key {key}");
			return string.Empty;
		}

		toReturn = Regex.Replace(toReturn, @"\\n", "\n");
		return toReturn;
	}

	public string TranslateLanguage(string language) {
		return !languages.ContainsKey(language) ? string.Empty : languages[language];
	}

	public void SaveLanguage(string languageKey) {
		if (!LoadedLanguages.Contains(languageKey)) {
			return;
		}
		SetLanguage = languageKey;

		var langFilePath = Path.Combine("Configuration", "fronter-language.txt");
		using var fs = new FileStream(langFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
		using var writer = new StreamWriter(fs);
		writer.WriteLine($"language={languageKey}");
		writer.Close();
	}
	private void LoadLanguages() {
		var fileNames = SystemUtils.GetAllFilesInFolder("Configuration");

		foreach (var fileName in fileNames) {
			if (!fileName.EndsWith(".yml"))
				continue;

			var langFilePath = Path.Combine("Configuration", fileName);
			using var langFileStream = File.OpenRead(langFilePath);
			using var langFileReader = new StreamReader(langFileStream);

			var firstLine = langFileReader.ReadLine();
			if (firstLine?.IndexOf("l_", StringComparison.Ordinal) != 0) {
				Logger.Error($"{langFilePath} is not a localization file!");
				continue;
			}
			var pos = firstLine.IndexOf(':');
			if (pos == -1) {
				Logger.Error($"Invalid localization language: {firstLine}");
				continue;
			}
			var language = firstLine.Substring(2, pos - 2);

			while (!langFileReader.EndOfStream) {
				var line = langFileReader.ReadLine();
				if (line is null) {
					break;
				}

				pos = line.IndexOf(':');
				if (pos == -1)
					continue;
				var key = line.Substring(1, pos - 1);
				pos = line.IndexOf('\"');
				if (pos == -1) {
					Logger.Error($"Invalid localization line: {line}");
					continue;
				}
				var secpos = line.LastIndexOf('\"');
				if (secpos == -1) {
					Logger.Error($"Invalid localization line: {line}");
					continue;
				}
				var text = line.Substring(pos + 1, secpos - pos - 1);


				if (translations.TryGetValue(key, out var dictionary)) {
					dictionary.Add(language, text);
				} else {
					var newDict = new Dictionary<string, string> { [language] = text };
					translations.Add(key, newDict);
				}
			}
		}
	}

	public List<string> LoadedLanguages { get; } = new();
	private Dictionary<string, string> languages = new();
	private Dictionary<string, Dictionary<string, string>> translations = new();
	public string SetLanguage { get; private set; } = "english";
}