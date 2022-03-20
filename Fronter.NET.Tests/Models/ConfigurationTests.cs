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
}