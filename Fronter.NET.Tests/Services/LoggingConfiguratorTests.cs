using log4net;
using System.IO;
using System.Text;
using Xunit;

namespace Fronter.Tests.Services; 

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
}