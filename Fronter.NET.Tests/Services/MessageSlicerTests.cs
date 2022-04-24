using Fronter.Services;
using log4net.Core;
using Xunit;

namespace Fronter.Tests.Services; 

public class MessageSlicerTests {
	[Fact]
	public void MessageIsCorrectlySliced() {
		const string message = "2000-06-06 15:23:33 [ALERT] test message";
		var logLine = MessageSlicer.SliceMessage(message);
		
		Assert.Equal("2000-06-06 15:23:33", logLine.Timestamp);
		Assert.Equal(Level.Alert, logLine.Level);
		Assert.Equal("test message", logLine.Message);
	}
}