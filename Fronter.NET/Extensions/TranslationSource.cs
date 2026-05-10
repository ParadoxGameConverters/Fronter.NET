using Avalonia.Threading;
using commonItems;
using log4net;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Fronter.Extensions;

// idea based on https://gist.github.com/jakubfijalkowski/0771bfbd26ce68456d3e
internal sealed partial class TranslationSource : ReactiveObject {
	private static readonly ILog logger = LogManager.GetLogger("Translator");
	private const string DefaultLanguage = "english";
	private readonly Lock translationsLock = new();
	private readonly string baseDirectory;
	private readonly Dictionary<string, List<string>> localizationFilePathsByLanguage = new(StringComparer.Ordinal);
	private readonly HashSet<string> loadedTranslationLanguages = new(StringComparer.Ordinal);
	private int deferredTranslationsLoadStarted;
	private Task? deferredTranslationsLoadTask;

	private TranslationSource(): this(AppContext.BaseDirectory) {
	}

	internal TranslationSource(string baseDirectory) {
		this.baseDirectory = baseDirectory;

		string languagesPath = Path.Combine(baseDirectory, "languages.txt");
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
				if (string.Equals(langKey, DefaultLanguage, StringComparison.OrdinalIgnoreCase)) {
					cultureInfo = CultureInfo.InvariantCulture;
				} else {
					return;
				}
			}

			languages.Add(langKey, cultureInfo);
			LoadedLanguages.Add(langKey);
		});
		languagesParser.ParseFile(languagesPath);

		IndexLocalizationFiles();

		var savedLanguage = LoadSavedLanguage();
		if (!string.IsNullOrWhiteSpace(savedLanguage)) {
			CurrentLanguage = savedLanguage;
		}

		LoadTranslations(GetStartupLanguages(savedLanguage), notifyCurrentLanguageRefresh: false);
	}

	public static TranslationSource Instance { get; } = new();

	public string Translate(string key) {
		string? toReturn = null;

		lock (translationsLock) {
			if (translations.TryGetValue(key, out var dictionary)) {
				if (dictionary.TryGetValue(CurrentLanguage, out var text)) {
					toReturn = text;
				} else if (dictionary.TryGetValue(DefaultLanguage, out var englishText)) {
					logger.Debug($"{CurrentLanguage} localization not found for key {key}, using english one");
					toReturn = englishText;
				} else {
					logger.Debug($"{CurrentLanguage} localization not found for key {key}");
				}
			}
		}

		return toReturn is null ? string.Empty : NewLineInStringRegex().Replace(toReturn, Environment.NewLine);
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

		var langFilePath = Path.Combine(baseDirectory, "Configuration/fronter-language.txt");
		using var fs = new FileStream(langFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
		using var writer = new StreamWriter(fs);
		writer.WriteLine($"language={languageKey}");
		writer.Close();
	}

	public Task StartDeferredTranslationsLoad() {
		if (Interlocked.Exchange(ref deferredTranslationsLoadStarted, 1) == 1) {
			return deferredTranslationsLoadTask ?? Task.CompletedTask;
		}

		var languagesToLoad = GetRemainingLanguagesToLoad();
		if (languagesToLoad.Count == 0) {
			deferredTranslationsLoadTask = Task.CompletedTask;
			return deferredTranslationsLoadTask;
		}

		deferredTranslationsLoadTask = Task.Run(() => LoadTranslations(languagesToLoad, notifyCurrentLanguageRefresh: true));
		return deferredTranslationsLoadTask;
	}

	private string? LoadSavedLanguage() {
		var fronterLanguagePath = Path.Combine(baseDirectory, "Configuration/fronter-language.txt");
		if (!File.Exists(fronterLanguagePath)) {
			return null;
		}

		string? savedLanguage = null;
		var parser = new Parser();
		parser.RegisterKeyword("language", reader => savedLanguage = reader.GetString());
		parser.ParseFile(fronterLanguagePath);
		return savedLanguage;
	}

	private IEnumerable<string> GetStartupLanguages(string? savedLanguage) {
		yield return DefaultLanguage;

		if (!string.IsNullOrWhiteSpace(savedLanguage) && !string.Equals(savedLanguage, DefaultLanguage, StringComparison.Ordinal)) {
			yield return savedLanguage;
		}
	}

	private List<string> GetRemainingLanguagesToLoad() {
		lock (translationsLock) {
			return [.. localizationFilePathsByLanguage.Keys.Where(language => !loadedTranslationLanguages.Contains(language))];
		}
	}

	private void IndexLocalizationFiles() {
		var configurationPath = Path.Combine(baseDirectory, "Configuration");
		if (!Directory.Exists(configurationPath)) {
			return;
		}

		foreach (var fileName in SystemUtils.GetAllFilesInFolder(configurationPath)) {
			if (!fileName.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)) {
				continue;
			}

			var langFilePath = Path.Combine(configurationPath, fileName);
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

			var language = firstLine[2..pos];
			if (!localizationFilePathsByLanguage.TryGetValue(language, out var filePaths)) {
				filePaths = [];
				localizationFilePathsByLanguage[language] = filePaths;
			}

			filePaths.Add(langFilePath);
		}
	}

	private void LoadTranslations(IEnumerable<string> languagesToLoad, bool notifyCurrentLanguageRefresh) {
		bool refreshCurrentLanguage = false;

		foreach (var language in languagesToLoad.Distinct(StringComparer.Ordinal)) {
			if (!localizationFilePathsByLanguage.TryGetValue(language, out var filePaths)) {
				continue;
			}

			foreach (var filePath in filePaths) {
				LoadTranslationFile(filePath, language);
			}

			lock (translationsLock) {
				loadedTranslationLanguages.Add(language);
			}

			refreshCurrentLanguage |= notifyCurrentLanguageRefresh && string.Equals(CurrentLanguage, language, StringComparison.Ordinal);
		}

		if (refreshCurrentLanguage) {
			NotifyTranslationsChanged();
		}
	}

	private void LoadTranslationFile(string langFilePath, string language) {
		using var langFileStream = File.OpenRead(langFilePath);
		using var langFileReader = new StreamReader(langFileStream);

		var firstLine = langFileReader.ReadLine();
		if (firstLine?.IndexOf("l_", StringComparison.Ordinal) != 0) {
			logger.Error($"{langFilePath} is not a localization file!");
			return;
		}

		while (!langFileReader.EndOfStream) {
			var line = langFileReader.ReadLine();
			if (line is null) {
				break;
			}

			var pos = line.IndexOf(':');
			if (pos == -1) {
				continue;
			}

			var key = line[..pos].Trim();
			pos = line.IndexOf('"');
			if (pos == -1) {
				logger.Error($"Invalid localization line: {line}");
				continue;
			}

			var secpos = line.LastIndexOf('"');
			if (secpos == -1) {
				logger.Error($"Invalid localization line: {line}");
				continue;
			}

			var text = line.Substring(pos + 1, secpos - pos - 1);

			lock (translationsLock) {
				if (translations.TryGetValue(key, out var dictionary)) {
					dictionary[language] = text;
				} else {
					var newDict = new Dictionary<string, string>(StringComparer.Ordinal) { [language] = text };
					translations.Add(key, newDict);
				}
			}
		}
	}

	private void NotifyTranslationsChanged() {
		Dispatcher.UIThread.Post(() => {
			this.RaisePropertyChanged(nameof(CurrentLanguage));
			this.RaisePropertyChanged("Item");
		});
	}

	public List<string> LoadedLanguages { get; } = [];
	private readonly Dictionary<string, CultureInfo> languages = [];
	private readonly Dictionary<string, Dictionary<string, string>> translations = []; // key, <language, text>

	public string CurrentLanguage {
		get;
		private set {
			field = value;
			this.RaisePropertyChanged(nameof(CurrentLanguage));
			this.RaisePropertyChanged("Item");
		}
	} = DefaultLanguage;

	[GeneratedRegex(@"\\n")]
	private static partial Regex NewLineInStringRegex();
}
