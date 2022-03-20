using Fronter.Models.Configuration;
using Xunit;
namespace Fronter.Tests.Models;

public class ConfigurationTests {
	[Fact]
	public void SimpleValuesAreLoaded() {
		var config = new Configuration();
		Assert.Equal("ImperatorToCK3", config.Name);
		Assert.Equal("ImperatorToCK3", config.ConverterFolder);
		Assert.Equal("ImperatorToCK3Converter", config.BackendExePath);
		Assert.Equal("IMPDISPLAYNAME", config.DisplayName);
		Assert.Equal("IMPGAME", config.SourceGame);
		Assert.Equal("CK3GAME", config.TargetGame);
		Assert.True(config.UpdateCheckerEnabled);
		Assert.True(config.CheckForUpdatesOnStartup );
		Assert.Equal("https://github.com/ParadoxGameConverters/ImperatorToCK3/releases/latest", config.LatestGitHubConverterReleaseUrl);
		Assert.Equal("https://forum.paradoxplaza.com/forum/threads/imperator-to-ck3-release-thread.1415172", config.ConverterReleaseForumThread);
		Assert.Equal("https://paradoxgameconverters.com/commit_ids/ImperatorToCK3.txt", config.PagesCommitIdUrl);
	}

	[Fact]
	public void RequiredFoldersAndFilesAreLoaded() {
		var config = new Configuration();
		Assert.Collection(config.RequiredFolders,
			kvPair => {
				(string key, RequiredFolder folder) = kvPair;
				Assert.Equal("ImperatorDirectory",key);
				Assert.Equal("IMPFOLDER", folder.DisplayName);
			},
			kvPair => {
				(string key, RequiredFolder folder) = kvPair;
				Assert.Equal("ImperatorDocDirectory",key);
				Assert.Equal("IMPDOCTIP", folder.Tooltip);
			},
			kvPair => {
				(string key, RequiredFolder folder) = kvPair;
				Assert.Equal("CK3directory",key);
				Assert.True(folder.Mandatory);
			},
			kvPair => {
				(string key, RequiredFolder folder) = kvPair;
				Assert.Equal("targetGameModPath",key);
				Assert.Equal("Paradox Interactive\\Crusader Kings III\\mod", folder.SearchPath);
			}
		);
		
		Assert.Collection(config.RequiredFiles,
			kvPair => {
				(string key, RequiredFile file) = kvPair;
				Assert.Equal("SaveGame", file.Name);
				Assert.Equal("IMPSAVE", file.DisplayName);
				Assert.Equal("IMPSAVETIP", file.Tooltip);
				Assert.True(file.Mandatory);
				Assert.True(file.Outputtable);
				Assert.Equal("windowsUsersFolder", file.SearchPathType);
				Assert.Equal("Paradox Interactive\\Imperator\\save games", file.SearchPath );
				Assert.Equal("*.rome", file.AllowedExtension);
			});
	}
}