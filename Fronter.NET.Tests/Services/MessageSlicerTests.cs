using Fronter.Services;
using log4net.Core;
using System;
using Xunit;

namespace Fronter.Tests.Services;

public class MessageSlicerTests {
	[Fact]
	public void MessageIsCorrectlySliced() {
		const string message = "2000-06-06 15:23:33 [ALERT] test message";
		var logLine = MessageSlicer.SliceMessage(message);

		Assert.Equal("2000-06-06 15:23:33", actual: logLine.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
		Assert.Equal(Level.Alert, logLine.Level);
		Assert.Equal("test message", logLine.Message);
	}

	[Theory]
	[InlineData("2000-06-06 15:23:33 [DEBUG] debug message", "DEBUG", "debug message")]
	[InlineData("2000-06-06 15:23:33 [INFO] info message", "INFO", "info message")]
	[InlineData("2000-06-06 15:23:33 [WARN] warning message", "WARN", "warning message")]
	[InlineData("2000-06-06 15:23:33 [NOTICE] notice message", "NOTICE", "notice message")]
	[InlineData("2000-06-06 15:23:33 [ERROR] error message", "ERROR", "error message")]
	[InlineData("2000-06-06 15:23:33 [FATAL] fatal message", "FATAL", "fatal message")]
	[InlineData("2000-06-06 15:23:33 [PROGRESS] 25%", "PROGRESS", "25%")]
	public void SliceMessage_ParsesKnownLevels(string message, string expectedLevelName, string expectedText) {
		var logLine = MessageSlicer.SliceMessage(message);

		Assert.NotNull(logLine.Level);
		Assert.Equal(expectedLevelName, logLine.Level.Name);
		Assert.Equal(expectedText, logLine.Message);
		Assert.Equal("2000-06-06 15:23:33", logLine.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
	}

	[Fact]
	public void SliceMessage_WithoutTimestamp_StillParsesLevelAndMessage() {
		var logLine = MessageSlicer.SliceMessage("[WARNING] keep going");

		Assert.Equal(Level.Warn, logLine.Level);
		Assert.Equal("keep going", logLine.Message);
		Assert.True(logLine.Timestamp <= DateTime.Now);
	}

	[Fact]
	public void SliceMessage_WithoutBracketedLevel_ReturnsOriginalText() {
		var message = "plain log line without a level prefix";
		var logLine = MessageSlicer.SliceMessage(message);

		Assert.Null(logLine.Level);
		Assert.Equal(message, logLine.Message);
	}
}