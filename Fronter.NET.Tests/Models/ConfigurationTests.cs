using Fronter.Models.Configuration;
using Xunit;
namespace Fronter.Tests.Models;

public class ConfigurationTests {
	[Fact]
	public void SimpleValuesAreLoaded() {
		var config = new Config();
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
		var config = new Config();
		Assert.Collection(config.RequiredFolders,
			folder => {
				Assert.Equal("ImperatorDirectory", folder.Name);
				Assert.Equal("IMPFOLDER", folder.DisplayName);
				Assert.Equal("storeFolder", folder.SearchPathType);
				Assert.Equal("859580", folder.SteamGameId);
				Assert.Equal("2131232214", folder.GOGGameId);
			},
			folder => {
				Assert.Equal("ImperatorDocDirectory", folder.Name);
				Assert.Equal("IMPDOCTIP", folder.Tooltip);
			},
			folder => {
				Assert.Equal("CK3directory", folder.Name);
				Assert.True(folder.Mandatory);
			},
			folder => {
				Assert.Equal("targetGameModPath", folder.Name);
				Assert.Equal(@"Paradox Interactive\Crusader Kings III\mod", folder.SearchPath);
			}
		);

		Assert.Collection(config.RequiredFiles,
			file => {
				Assert.Equal("SaveGame", file.Name);
				Assert.Equal("IMPSAVE", file.DisplayName);
				Assert.Equal("IMPSAVETIP", file.Tooltip);
				Assert.True(file.Mandatory);
				Assert.True(file.Outputtable);
				Assert.Equal("windowsUsersFolder", file.SearchPathType);
				Assert.Equal(@"Paradox Interactive\Imperator\save games", file.SearchPath );
				Assert.Equal("*.rome", file.AllowedExtension);
			});
	}
}