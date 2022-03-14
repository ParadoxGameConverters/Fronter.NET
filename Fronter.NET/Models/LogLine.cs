using Fronter.Services;

namespace Fronter.Models; 

public class LogLine {
	public string Timestamp { get; set; }
	public MessageSlicer.LogLevel LogLevel { get; set; } = MessageSlicer.LogLevel.Info;
	public MessageSlicer.MessageSource Source { get; set; } = MessageSlicer.MessageSource.UNINITIALIZED;
	public string Message { get; set; }
}