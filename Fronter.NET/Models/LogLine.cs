using log4net.Core;
using ReactiveUI;
using System;
using System.Globalization;

namespace Fronter.Models;

public sealed class LogLine : ReactiveObject {
	public string Timestamp { get; set; } = string.Empty;
	public DateTime TimestampAsDateTime => 
		string.IsNullOrWhiteSpace(Timestamp) ? DateTime.Now : Convert.ToDateTime(Timestamp, CultureInfo.InvariantCulture);
	public Level? Level { get; set; }
	public string LevelName => Level?.Name ?? string.Empty;

	private string message = string.Empty;
	public string Message {
		get => message;
		set => this.RaiseAndSetIfChanged(ref message, value);
	}
}