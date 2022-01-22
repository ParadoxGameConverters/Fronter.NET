using System.Collections.Generic;

namespace Fronter.Models;

public class Configuration {
	public string Name { get; private set; }
	public string ConverterFolder { get; private set; }
	public string BackendExePath { get; private set; } // relative to ConverterFolder
	public string DisplayName { get; private set; }
	public string SourceGame { get; private set; }
	public string TargetGame { get; private set; }
	public string AutoGenerateModsFrom { get; private set; }
	public bool EnableUpdateChecker { get; private set; } = false;
	public bool CheckForUpdatesOnStartup { get; private set; } = false;
	public string ConverterReleaseForumThread { get; private set; }
	public string LatestGitHubCoverterReleaseThread { get; private set; }
	public string PagesCommitIdUrl { get; private set; }
	public Dictionary<string, RequiredFile> RequiredFiles { get; } = new();
	public Dictionary<string, RequiredFolder> RequiredFolders { get; } = new();
	public List<Option> Options { get; } = new();
	public List<Mod> AutoLocatedMods { get; } = new();
	public HashSet<string> PreloadedModFileNames { get; } = new();

}