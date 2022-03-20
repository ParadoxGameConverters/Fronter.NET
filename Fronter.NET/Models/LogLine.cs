using commonItems;
using Fronter.Services;

namespace Fronter.Models; 

public class LogLine {
	public string Timestamp { get; set; }
	public Logger.LogLevel? LogLevel { get; set; }
	public MessageSlicer.MessageSource Source { get; set; } = MessageSlicer.MessageSource.UNINITIALIZED;
	public string Message { get; set; }
}