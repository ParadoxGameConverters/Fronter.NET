using commonItems;
using Fronter.Models.Configuration;
using System.Diagnostics;
using System.IO;

namespace Fronter.Services;

internal class ConverterLauncher {
	public void LaunchConverter() {
		//var converterFolder = Path.Combine();
		var backendExePath = Path.Combine();
		var backendExePathRelativeToFrontend = Path.Combine(configuration.ConverterFolder, configuration.BackendExePath);

		var extension = CommonFunctions.GetExtension(backendExePathRelativeToFrontend);
		if (string.IsNullOrEmpty(extension)) {
			backendExePathRelativeToFrontend += ".exe";
		}

		if (string.IsNullOrEmpty(backendExePath)) {
			Logger.Error("Converter location has not been set!");
		}

		if (!File.Exists(backendExePathRelativeToFrontend)) {
			Logger.Error("Could not find converter executable!");
		}



		if (extension == "jar") {
			backendExePathRelativeToFrontend = $"java.exe -jar {backendExePathRelativeToFrontend}";
		}




		using Process process = new();
		string currentDir = Directory.GetCurrentDirectory();
		string executablePath = backendExePathRelativeToFrontend;

		process.StartInfo.WorkingDirectory = CommonFunctions.GetPath(backendExePathRelativeToFrontend);
		process.StartInfo.UseShellExecute = false;
		process.StartInfo.FileName = executablePath;
		process.StartInfo.CreateNoWindow = true;

		var timer = new Stopwatch();
		timer.Start();
		process.Start();
		process.WaitForExit();
		timer.Stop();

		Logger.Info($"Converter exited at {timer.Elapsed.TotalSeconds} seconds.");

		if (process.ExitCode != 0) {
			Logger.Error("Converter Error! See log.txt for details.");
			Logger.Error("If you require assistance please upload log.txt to forums for a detailed post-mortem.");
		}
	}

	public void LoadConfiguration(Configuration configuration) {
		this.configuration = configuration;
	}

	private Configuration configuration;
}