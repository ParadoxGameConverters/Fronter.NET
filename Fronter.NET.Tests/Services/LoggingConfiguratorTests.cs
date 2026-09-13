using log4net;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace Fronter.Tests.Services; 

[Collection("Sequential")]
public class LoggingConfiguratorTests {
	[Fact]
	public void MessagesAreLoggedToLogTxtFile() {
		LoggingConfigurator.ConfigureLogging(useConsole: false);
		
		var logger = LogManager.GetLogger(typeof(LoggingConfiguratorTests));
		logger.Debug("Test debug message");
		logger.Info("Test message");
		logger.Warn("Test warning");
		logger.Error("Test error");

		using var fs = new FileStream("log.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		using var sr = new StreamReader(fs, Encoding.Default);
		
		var logFileContent = sr.ReadToEnd();
		Assert.Contains("Test debug message", logFileContent);
		Assert.Contains("Test message", logFileContent);
		Assert.Contains("Test warning", logFileContent);
		Assert.Contains("Test error", logFileContent);
	}

	[Fact]
	public void LogsAreWrittenNextToExecutableNotCurrentWorkingDirectory() {
		// Reproduce the macOS launch scenario: CWD is not the app's directory.
		var decoyDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		var previousWorkingDirectory = Directory.GetCurrentDirectory();
		try {
			Directory.CreateDirectory(decoyDirectory);
			Directory.SetCurrentDirectory(decoyDirectory);

			LoggingConfigurator.ConfigureLogging(useConsole: false);

			var logger = LogManager.GetLogger(typeof(LoggingConfiguratorTests));
			logger.Info("Path resolution test message");

			Assert.True(File.Exists(FronterPaths.LogFilePath), $"Expected log file at {FronterPaths.LogFilePath}");

			using var fs = new FileStream(FronterPaths.LogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			using var sr = new StreamReader(fs, Encoding.Default);
			var logFileContent = sr.ReadToEnd();
			Assert.Contains("Path resolution test message", logFileContent);

			// Nothing may be written into the working directory.
			Assert.False(File.Exists(Path.Combine(decoyDirectory, "log.txt")));
		}
		finally {
			Directory.SetCurrentDirectory(previousWorkingDirectory);
			if (Directory.Exists(decoyDirectory)) {
				Directory.Delete(decoyDirectory, recursive: true);
			}
		}
	}
}