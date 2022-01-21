using System.Collections.Generic;

namespace Fronter.NET.Services;

public class Localization {
	public Localization() {

	}

	public string Translate(string key) {

	}

	public string TranslateLanguage(string language) {

	}

	public void SaveLanguage(int id) {

	}
	private void LoadLanguages() {

	}

	public List<string> LoadedLanguages { get; } = new();
	private Dictionary<string, string> languages;
	private Dictionary<string, Dictionary<string, string>> translations = new();
	public string SetLanguage { get; private set; } = "english";
}