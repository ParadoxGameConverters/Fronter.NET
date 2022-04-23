using commonItems;
using Fronter.Services;
using log4net.Core;
using ReactiveUI;
using System.ComponentModel;

namespace Fronter.Models; 

public class LogLine : ReactiveObject {
	public enum MessageSource {
		Uninitialized = 0,
		UI = 1,
		Converter = 2
	}
	public string Timestamp { get; set; } = string.Empty;
	public Level? Level { get; set; }
	public MessageSource Source { get; set; } = MessageSource.Uninitialized;

	private string message = string.Empty;
	public string Message {
		get => message;
		set => this.RaiseAndSetIfChanged(ref message, value);
	}
}