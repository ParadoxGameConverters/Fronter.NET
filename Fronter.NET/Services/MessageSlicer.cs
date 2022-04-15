using commonItems;
using Fronter.Models;
using Splat;
using System.Text.RegularExpressions;

namespace Fronter.Services;

public class MessageSlicer {
	public enum MessageSource {
		UNINITIALIZED = 0,
		UI = 1,
		CONVERTER = 2
	}

	private static Regex dateTimeRegex = new Regex(@"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})$");

	public static LogLine SliceMessage(string message) {
		var logMessage = new LogLine();
		
		var posOpen = message.IndexOf('[');
		var posClose = message.IndexOf(']');

		if (posOpen < 0 || posOpen > posClose) {
			logMessage.Message = message;
			return logMessage;
		}

		var timestampPart = message[..posOpen].Trim();
		if (dateTimeRegex.IsMatch(timestampPart)) {
		} else {
			logMessage.Message = message;
			return logMessage;
		}
		
		logMessage.Timestamp = timestampPart;
		var logLevelStr = message.Substring(posOpen + 1, posClose - posOpen - 1);
		logMessage.LogLevel = GetLogLevel(logLevelStr);
		if (message.Length >= posClose + 2) {
			logMessage.Message = message[(posClose + 2)..];
		}
		
		return logMessage;
	}

	private static Logger.LogLevel? GetLogLevel(string levelStr) {
		return levelStr switch {
			"DEBUG" => Logger.LogLevel.Debug,
			"INFO" => Logger.LogLevel.Info,
			"WARNING" or "WARN" => Logger.LogLevel.Warn,
			"ERROR" => Logger.LogLevel.Error,
			"PROGRESS" => Logger.LogLevel.Progress,
			"NOTICE" => Logger.LogLevel.Notice,
			_ => null
		};
	}
}