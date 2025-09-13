using log4net.Core;
using ReactiveUI;
using System;

namespace Fronter.Models;

internal sealed class LogLine : ReactiveObject {
	public DateTime Timestamp { get; set; }
	public Level? Level { get; set; }
	public string LevelName => Level?.Name ?? string.Empty;

	public string Message {
		get;
		set => this.RaiseAndSetIfChanged(ref field, value);
	} = string.Empty;

	internal LogLine(DateTime timestamp, Level? level, string message) {
		Timestamp = timestamp;
		Level = level;
		Message = message;
	}
}