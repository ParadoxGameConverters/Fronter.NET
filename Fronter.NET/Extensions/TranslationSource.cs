using commonItems;
using log4net;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Fronter.Extensions;

// idea based on https://gist.github.com/jakubfijalkowski/0771bfbd26ce68456d3e
public sealed partial class TranslationSource : ReactiveObject {
	private static readonly ILog logger = LogManager.GetLogger("Translator");
	private TranslationSource() {
		const string languagesPath = "languages.txt";
		if (!File.Exists(languagesPath)) {
			logger.Error("No languages dictionary found!");
			return;
		}

		var languagesParser = new Parser();
		languagesParser.RegisterRegex(CommonRegexes.String, (langReader, langKey) => {
			var cultureName = langReader.GetString();

			CultureInfo cultureInfo;
			try {
				cultureInfo = CultureInfo.GetCultureInfo(cultureName);
			} catch (CultureNotFoundException) {
				logger.Debug($"Culture {cultureName} for language {langKey} not found!");
				if (string.Equals(langKey, "english", StringComparison.OrdinalIgnoreCase)) {
					cultureInfo = CultureInfo.InvariantCulture;
				} else {
					return;
				}
			}

			languages.Add(langKey, cultureInfo);
			LoadedLanguages.Add(langKey);
		});
		languagesParser.ParseFile(languagesPath);

		LoadLanguages();

		var fronterLanguagePath = Path.Combine("Configuration", "fronter-language.txt");
		if (File.Exists(fronterLanguagePath)) {
			var parser = new Parser();
			parser.RegisterKeyword("language", reader => CurrentLanguage = reader.GetString());
			parser.ParseFile(fronterLanguagePath);
		}
	}

	public static TranslationSource Instance { get; } = new();

	public string Translate(string key) {
		string toReturn;

		if (translations.TryGetValue(key, out var dictionary)) {
			if (dictionary.TryGetValue(CurrentLanguage, out var text)) {
				toReturn = text;
			} else if (dictionary.TryGetValue("english", out var englishText)) {
				logger.Debug($"{CurrentLanguage} localization not found for key {key}, using english one");
				toReturn = englishText;
			} else {
				logger.Debug($"{CurrentLanguage} localization not found for key {key}");
				return string.Empty;
			}
		} else {
			return string.Empty;
		}

		toReturn = NewLineInStringRegex().Replace(toReturn, Environment.NewLine);
		return toReturn;
	}

	public string TranslateLanguage(string language) {
		return !languages.TryGetValue(language, out CultureInfo? cultureInfo) ? string.Empty : cultureInfo.NativeName;
	}

	public string this[string key] => Translate(key);

	public void SaveLanguage(string languageKey) {
		if (!LoadedLanguages.Contains(languageKey)) {
			return;
		}
		CurrentLanguage = languageKey;

		var langFilePath = Path.Combine("Configuration", "fronter-language.txt");
		using var fs = new FileStream(langFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
		using var writer = new StreamWriter(fs);
		writer.WriteLine($"language={languageKey}");
		writer.Close();
	}
	private void LoadLanguages() {
		var fileNames = SystemUtils.GetAllFilesInFolder("Configuration");

		foreach (var fileName in fileNames) {
			if (!fileName.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)) {
				continue;
			}

			var langFilePath = Path.Combine("Configuration", fileName);
			using var langFileStream = File.OpenRead(langFilePath);
			using var langFileReader = new StreamReader(langFileStream);

			var firstLine = langFileReader.ReadLine();
			if (firstLine?.IndexOf("l_", StringComparison.Ordinal) != 0) {
				logger.Error($"{langFilePath} is not a localization file!");
				continue;
			}
			var pos = firstLine.IndexOf(':');
			if (pos == -1) {
				logger.Error($"Invalid localization language: {firstLine}");
				continue;
			}
			var language = firstLine.Substring(2, pos - 2);

			while (!langFileReader.EndOfStream) {
				var line = langFileReader.ReadLine();
				if (line is null) {
					break;
				}

				pos = line.IndexOf(':');
				if (pos == -1) {
					continue;
				}
				var key = line[..pos].Trim();
				pos = line.IndexOf('\"');
				if (pos == -1) {
					logger.Error($"Invalid localization line: {line}");
					continue;
				}
				var secpos = line.LastIndexOf('\"');
				if (secpos == -1) {
					logger.Error($"Invalid localization line: {line}");
					continue;
				}
				var text = line.Substring(pos + 1, secpos - pos - 1);

				if (translations.TryGetValue(key, out var dictionary)) {
					dictionary[language] = text;
				} else {
					var newDict = new Dictionary<string, string>(StringComparer.Ordinal) { [language] = text };
					translations.Add(key, newDict);
				}
			}
		}
	}

	public IList<string> LoadedLanguages { get; } = [];
	private readonly Dictionary<string, CultureInfo> languages = [];
	private readonly Dictionary<string, Dictionary<string, string>> translations = []; // key, <language, text>

	private string currentLanguage = "english";
	public string CurrentLanguage {
		get => currentLanguage;
		private set {
			currentLanguage = value;
			this.RaisePropertyChanged(nameof(CurrentLanguage));
			this.RaisePropertyChanged("Item");
		}
	}

	[GeneratedRegex(@"\\n")]
	private static partial Regex NewLineInStringRegex();
}