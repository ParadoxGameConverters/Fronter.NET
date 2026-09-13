using Fronter.Extensions;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Fronter.Tests.Extensions;

[Collection("Sequential")]
public sealed class TranslationSourceTests {
	[Fact]
	public void StartupLoadIncludesEnglishAndSavedLanguage() {
		var tempDir = CreateLocalizationRoot();
		try {
			WriteSavedLanguage(tempDir, "german");
			WriteLocalizationFile(tempDir, "english", "EXIT", "Exit");
			WriteLocalizationFile(tempDir, "german", "EXIT", "Verlassen");
			WriteLocalizationFile(tempDir, "french", "EXIT", "Quitter");

			var source = new TranslationSource(tempDir);

			Assert.Equal("german", source.CurrentLanguage);
			Assert.Equal("Verlassen", source.Translate("EXIT"));
		}
		finally {
			Cleanup(tempDir);
		}
	}

	[Fact]
	public async Task DeferredLoadMakesRemainingLanguagesAvailable() {
		var tempDir = CreateLocalizationRoot();
		try {
			WriteLocalizationFile(tempDir, "english", "EXIT", "Exit");
			WriteLocalizationFile(tempDir, "french", "EXIT", "Quitter");

			var source = new TranslationSource(tempDir);

			source.SaveLanguage("french");
			Assert.Equal("Exit", source.Translate("EXIT"));

			source.SaveLanguage("english");
			await source.StartDeferredTranslationsLoad();
			source.SaveLanguage("french");

			Assert.Equal("Quitter", source.Translate("EXIT"));
		}
		finally {
			Cleanup(tempDir);
		}
	}

	[Fact]
	public void InvalidSavedLanguageFallsBackToEnglish() {
		var tempDir = CreateLocalizationRoot();
		try {
			WriteSavedLanguage(tempDir, "spanish");
			WriteLocalizationFile(tempDir, "english", "EXIT", "Exit");
			WriteLocalizationFile(tempDir, "german", "EXIT", "Verlassen");

			var source = new TranslationSource(tempDir);

			Assert.Equal("spanish", source.CurrentLanguage);
			Assert.Equal("Exit", source.Translate("EXIT"));
		}
		finally {
			Cleanup(tempDir);
		}
	}

	[Fact]
	public void TranslationsAreLoadedFromBaseDirectoryNotWorkingDirectory() {
		// Reproduce the macOS launch scenario: CWD is a different directory that
		// happens to contain its own localization files with different content.
		var realBaseDir = CreateLocalizationRoot();
		var decoyWorkingDirectory = CreateLocalizationRoot();
		var previousWorkingDirectory = Directory.GetCurrentDirectory();
		try {
			WriteLocalizationFile(realBaseDir, "english", "EXIT", "Exit");
			WriteLocalizationFile(decoyWorkingDirectory, "english", "EXIT", "Decoy");

			Directory.SetCurrentDirectory(decoyWorkingDirectory);

			var source = new TranslationSource(realBaseDir);

			// Translations must come from the base directory, not the working directory.
			Assert.Equal("Exit", source.Translate("EXIT"));

			source.SaveLanguage("english");

			// The saved language file must be written next to the executable's files.
			Assert.True(File.Exists(Path.Combine(realBaseDir, "Configuration", "fronter-language.txt")));
			Assert.False(File.Exists(Path.Combine(decoyWorkingDirectory, "Configuration", "fronter-language.txt")));
		}
		finally {
			Directory.SetCurrentDirectory(previousWorkingDirectory);
			Cleanup(realBaseDir);
			Cleanup(decoyWorkingDirectory);
		}
	}

	private static string CreateLocalizationRoot() {
		var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(Path.Combine(tempDir, "Configuration"));
		File.WriteAllText(Path.Combine(tempDir, "languages.txt"), "english=en\nfrench=fr\ngerman=de\n");
		return tempDir;
	}

	private static void WriteSavedLanguage(string rootPath, string language) {
		File.WriteAllText(Path.Combine(rootPath, "Configuration/fronter-language.txt"), $"language={language}\n");
	}

	private static void WriteLocalizationFile(string rootPath, string language, string key, string text) {
		var content = $"l_{language}:\n  {key}: \"{text}\"\n";
		File.WriteAllText(Path.Combine(rootPath, $"Configuration/converter_l_{language}.yml"), content);
	}

	private static void Cleanup(string path) {
		if (Directory.Exists(path)) {
			Directory.Delete(path, recursive: true);
		}
	}
}
