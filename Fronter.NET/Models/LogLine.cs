using commonItems;
using Fronter.Services;
using ReactiveUI;
using System.ComponentModel;

namespace Fronter.Models; 

public class LogLine : ReactiveObject {
	public string Timestamp { get; set; } = string.Empty;
	public Logger.LogLevel? LogLevel { get; set; }
	public MessageSlicer.MessageSource Source { get; set; } = MessageSlicer.MessageSource.UNINITIALIZED;

	private string message = string.Empty;
	public string Message {
		get => message;
		set => this.RaiseAndSetIfChanged(ref message, value);
	}
}