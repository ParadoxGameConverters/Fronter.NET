using Fronter.Models;
using System.Text.RegularExpressions;
using LogLevel = commonItems.LogLevel;

namespace Fronter.Services;

public static class MessageSlicer {
	public enum MessageSource {
		UNINITIALIZED = 0,
		UI = 1,
		CONVERTER = 2
	}

	private static Regex dateTimeRegex = new(@"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})$");

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

	private static LogLevel? GetLogLevel(string levelStr) {
		return levelStr switch {
			"DEBUG" => LogLevel.Debug,
			"INFO" => LogLevel.Info,
			"WARNING" or "WARN" => LogLevel.Warn,
			"ERROR" => LogLevel.Error,
			"PROGRESS" => LogLevel.Progress,
			"NOTICE" => LogLevel.Notice,
			_ => null
		};
	}
}