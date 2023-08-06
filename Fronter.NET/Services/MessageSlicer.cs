using Fronter.Models;
using log4net;
using log4net.Core;
using System.Text.RegularExpressions;

namespace Fronter.Services;

public static partial class MessageSlicer {
	private static readonly Regex DateTimeRegex = GetDateTimeRegex();

	public static LogLine SliceMessage(string message) {
		var logMessage = new LogLine();

		var posOpen = message.IndexOf('[');
		var posClose = message.IndexOf(']');

		if (posOpen < 0 || posOpen > posClose) {
			logMessage.Message = message;
			return logMessage;
		}

		var timestampPart = message[..posOpen].Trim();
		if (DateTimeRegex.IsMatch(timestampPart)) {
			logMessage.Timestamp = timestampPart;
		} else if (!message.TrimStart().StartsWith('[')){
			logMessage.Message = message;
			logMessage.Level = null;
			return logMessage;
		}

		var logLevelStr = message.Substring(posOpen + 1, posClose - posOpen - 1);
		logMessage.Level = GetLogLevel(logLevelStr);
		if (message.Length >= posClose + 2) {
			logMessage.Message = message[(posClose + 2)..];
		}

		return logMessage;
	}

	private static Level GetLogLevel(string levelStr) {
		if (levelStr == "WARNING") {
			levelStr = "WARN";
		}
		var level = LogManager.GetRepository().LevelMap[levelStr];
		return level;
	}

	[GeneratedRegex(@"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})$")]
	private static partial Regex GetDateTimeRegex();
}