namespace Fronter.NET.Services;

internal class MessageSlicer {
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

	public class LogMessage {
		public string Timestamp { get; set; }
		public LogLevel LogLevel { get; set; } = LogLevel.Info;
		public MessageSource Source { get; set; } = MessageSource.UNINITIALIZED;
		public string Message { get; set; }
	}

	public LogMessage SliceMessage(string message) {
		var logMessage = new LogMessage();
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
		if (logLevel == "INFO")
			logMessage.LogLevel = LogLevel.Info;
		else if (logLevel == "WARNING" || logLevel == "WARN")
			logMessage.LogLevel = LogLevel.Warn;
		else if (logLevel == "ERROR")
			logMessage.LogLevel = LogLevel.Error;
		else if (logLevel == "PROGRESS")
			logMessage.LogLevel = LogLevel.Progress;
		else if (logLevel == "NOTICE")
			logMessage.LogLevel = LogLevel.Notice;
		else
			logMessage.LogLevel = LogLevel.Debug; // Debug or Unknown log level.

		logMessage.Timestamp = message.Substring(0, 19);
		logMessage.Message = message.Substring(posClose + 2, message.Length);
		return logMessage;
	}
}