using Fronter.Models;

namespace Fronter.Services;

public class MessageSlicer {
	public enum MessageSource {
		UNINITIALIZED = 0,
		UI = 1,
		CONVERTER = 2
	}

	public enum LogLevel {
		Debug,
		Info,
		Warn,
		Error,
		Notice,
		Progress
	}

	public static LogLine SliceMessage(string message) {
		var logMessage = new LogLine();
		// Is this a version dump?
		if (message.StartsWith('*')) {
			// This is not a standard message. File as info ad verbatim.
			logMessage.Message = message;
			return logMessage;
		}
		var posOpen = message.IndexOf('[');
		if (posOpen is not (>= 20 and <= 24)) {
			// This is not a standard message. File as info ad verbatim.
			logMessage.Message = message;
			return logMessage;
		}
		var posClose = message.IndexOf(']');
		if (posClose == -1) {
			// Something's very wrong with this message.
			logMessage.Message = message;
			return logMessage;
		}
		var logLevel = message.Substring(posOpen + 1, posClose - posOpen - 1);
		logMessage.LogLevel = logLevel switch {
			"INFO" => LogLevel.Info,
			"WARNING" or "WARN" => LogLevel.Warn,
			"ERROR" => LogLevel.Error,
			"PROGRESS" => LogLevel.Progress,
			"NOTICE" => LogLevel.Notice,
			_ => LogLevel.Debug
		};

		logMessage.Timestamp = message.Substring(0, 19);
		logMessage.Message = message.Substring(posClose + 2, message.Length);
		return logMessage;
	}
}