using commonItems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Fronter.NET.Services;

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
			var pos = line.IndexOf(':');
			if (pos == -1)
				continue;
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
			using var userFileStream = File.OpenRead(fronterLanguagePath);
			using var userFileReader = new StreamReader(userFileStream);
			var line = reader.ReadLine();
			if (line?.IndexOf("language=") == 0) {
				SetLanguage = line[9..];
			}
		}
	}

	public string Translate(string key) {
		string toReturn;
		if (!translations.ContainsKey(key)) {
			return string.Empty;
		}
		if (translations[key].ContainsKey(SetLanguage))
			toReturn = translations[key][SetLanguage];
		else if (translations[key].ContainsKey("english"))
			toReturn = translations[key]["english"];
		else
			return string.Empty;

		toReturn = Regex.Replace(toReturn, @"\\n", "\n");
		return toReturn;
	}

	public string TranslateLanguage(string language) {
		return !languages.ContainsKey(language) ? string.Empty : languages[language];
	}

	public void SaveLanguage(int id) {
		if (id > LoadedLanguages.Count + 1)
			return;
		SetLanguage = LoadedLanguages[id];

		var langFilePath = Path.Combine("Configuration", "fronter-language.txt");
		using var langFileStream = File.OpenWrite(langFilePath);
		using var writer = new StreamWriter(langFilePath);

		writer.WriteLine($"language={LoadedLanguages[id]}");
	}
	private void LoadLanguages() {
		var fileNames = SystemUtils.GetAllFilesInFolder("Configuration");

		foreach (var fileName in fileNames) {
			if (fileName.IndexOf(".yml", StringComparison.Ordinal) == -1)
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