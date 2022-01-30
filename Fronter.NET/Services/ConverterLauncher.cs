using commonItems;
using Fronter.Models.Configuration;
using System.IO;

namespace Fronter.Services;

internal class ConverterLauncher {
	public ConverterLauncher() {
		var converterFolder = Path.Combine(configuration.ConverterFolder);
		var backendExePath = Path.Combine(configuration.BackendExePath);
		var backendExePathRelativeToFrontend = Path.Combine(converterFolder, backendExePath);

		var extension = CommonFunctions.GetExtension(backendExePathRelativeToFrontend);
		if (extension == "jar") {
			backendExePathRelativeToFrontend = $"java.exe -jar {backendExePathRelativeToFrontend}";
		} else if (string.IsNullOrEmpty(extension)) {
			backendExePathRelativeToFrontend += ".exe";
		}

		if (string.IsNullOrEmpty(backendExePath)) {
			Logger.Error("Converter location has not been set!");
		}
		if (!File.Exists(backendExePathRelativeToFrontend)) {
			Logger.Error("Could not find converter executable!");
		}
	}

	private Configuration configuration;
}