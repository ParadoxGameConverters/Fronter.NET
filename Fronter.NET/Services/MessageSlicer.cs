using commonItems;
using Fronter.Models;
using Splat;

namespace Fronter.Services;

public class MessageSlicer {
	public enum MessageSource {
		UNINITIALIZED = 0,
		UI = 1,
		CONVERTER = 2
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
		
		var logLevelStr = message.Substring(posOpen + 1, posClose - posOpen - 1);
		logMessage.LogLevel = GetLogLevel(logLevelStr);

		logMessage.Timestamp = message.Substring(0, 19);
		logMessage.Message = message.Substring(posClose + 2);
		
		return logMessage;
	}

	private static Logger.LogLevel GetLogLevel(string levelStr) {
		return levelStr switch {
			"DEBUG" => Logger.LogLevel.Debug,
			"INFO" => Logger.LogLevel.Info,
			"WARNING" or "WARN" => Logger.LogLevel.Warn,
			"ERROR" => Logger.LogLevel.Error,
			"PROGRESS" => Logger.LogLevel.Progress,
			"NOTICE" => Logger.LogLevel.Notice,
			_ => Logger.LogLevel.Debug
		};
	}
}