using Fronter.LogAppenders;
using Fronter.Models;
using log4net.Core;
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
}