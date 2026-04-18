using commonItems;
using Fronter.LogAppenders;
using Fronter.Models;
using log4net.Core;
using System.Threading.Tasks;
using Xunit;

namespace Fronter.Tests.LogAppenders;

[Collection("Sequential")]
public class LogGridAppenderTests {
	[Fact]
	public void ToggleLogFilterLevel_DoesNotSurfaceStandaloneContinuationRows() {
		var appender = new LogGridAppender();

		appender.LogLines.Add(new LogLine(System.DateTime.Now, Level.Warn, "visible warning"));
		appender.LogLines.Add(new LogLine(System.DateTime.Now, null, "continuation"));

		Assert.Single(appender.FilteredLogLines);

		appender.LogFilterLevel = Level.Info;
		appender.ToggleLogFilterLevel();

		Assert.Single(appender.FilteredLogLines);
	}

	[Theory]
	[InlineData("42%", (ushort)42)]
	[InlineData(" 7 ", (ushort)7)]
	public void TryGetProgressValue_ReturnsParsedProgressForProgressRows(string message, ushort expectedValue) {
		var logLine = new LogLine(System.DateTime.Now, LogExtensions.ProgressLevel, message);

		var success = LogGridAppender.TryGetProgressValue(logLine, out var progressValue);

		Assert.True(success);
		Assert.Equal(expectedValue, progressValue);
	}

	[Fact]
	public void TryGetProgressValue_ReturnsFalseForNonProgressRows() {
		var logLine = new LogLine(System.DateTime.Now, Level.Info, "42%");

		var success = LogGridAppender.TryGetProgressValue(logLine, out _);

		Assert.False(success);
	}

	[Fact]
	public async Task ClearDisplayedLogLines_ClearsRows_WhenAwaited() {
		var appender = new LogGridAppender();
		appender.LogLines.Add(new LogLine(System.DateTime.Now, Level.Warn, "warning"));
		appender.LogLines.Add(new LogLine(System.DateTime.Now, Level.Error, "error"));

		await appender.ClearDisplayedLogLines();

		Assert.Empty(appender.LogLines);
		Assert.Empty(appender.FilteredLogLines);
	}
}