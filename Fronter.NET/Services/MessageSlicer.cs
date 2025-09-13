using commonItems;
using Fronter.Models;
using log4net;
using log4net.Core;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Fronter.Services;

internal static partial class MessageSlicer {
    private static readonly ILog logger = LogManager.GetLogger("Frontend");

    public static LogLine SliceMessageV2(string message) {
        ReadOnlySpan<char> span = message.AsSpan();

        int posOpen = span.IndexOf('[');
        int posClose = span.IndexOf(']');

        if (posOpen < 0 || posOpen > posClose) {
			return new LogLine(DateTime.Now, level: null, message);
		}

        ReadOnlySpan<char> timestampSpan = span[..posOpen].Trim();
		DateTime timestamp;
		if (IsTimestamp(timestampSpan)) {
			timestamp = DateTime.ParseExact(timestampSpan, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
		} else {
			timestamp = DateTime.Now;
			var trimmedStart = span.TrimStart();
			if (trimmedStart.IsEmpty || trimmedStart[0] != '[') {
				return new LogLine(timestamp, level: null, message);
			}
		}

		var levelSpan = span.Slice(posOpen + 1, posClose - posOpen - 1);
		Level? level = GetLogLevel(levelSpan);

		string msg = string.Empty;
		if (message.Length >= posClose + 2) {
			msg = message[(posClose + 2)..];
		}

        return new LogLine(timestamp, level, msg);
	}

    private static Level GetLogLevel(ReadOnlySpan<char> levelSpan) {
		// Map common levels without allocations.
		// For optimal performance, order by expected frequency.
		if (levelSpan.Equals("DEBUG".AsSpan(), StringComparison.OrdinalIgnoreCase)) {
			return Level.Debug;
		}
		if (levelSpan.Equals("INFO".AsSpan(), StringComparison.OrdinalIgnoreCase)) {
			return Level.Info;
		}
		if (levelSpan.Equals("PROGRESS".AsSpan(), StringComparison.OrdinalIgnoreCase)) {
			return LogExtensions.ProgressLevel;
		}
		if (levelSpan.Equals("WARN".AsSpan(), StringComparison.OrdinalIgnoreCase)) {
			return Level.Warn;
		}
		if (levelSpan.Equals("NOTICE".AsSpan(), StringComparison.OrdinalIgnoreCase)) {
			return Level.Notice;
		}
		if (levelSpan.Equals("ERROR".AsSpan(), StringComparison.OrdinalIgnoreCase)) {
			return Level.Error;
		}
		if (levelSpan.Equals("FATAL".AsSpan(), StringComparison.OrdinalIgnoreCase)) {
			return Level.Fatal;
		}
		if (levelSpan.Equals("WARNING".AsSpan(), StringComparison.OrdinalIgnoreCase)) {
			return Level.Warn;
		}

		string levelKey = new(levelSpan);
		var level = LogManager.GetRepository().LevelMap[levelKey];
        if (level == null) {
            logger.Warn($"Unknown log level: {levelKey}");
            level = Level.Debug;
        }

        return level;
    }

    private static bool IsTimestamp(ReadOnlySpan<char> s) {
        // Fast check for format: yyyy-MM-dd HH:mm:ss (length 19)
        if (s.Length != 19) return false;

        static bool IsDigit(char c) => (uint)(c - '0') <= 9u;

        // yyyy
        if (!IsDigit(s[0]) || !IsDigit(s[1]) || !IsDigit(s[2]) || !IsDigit(s[3])) return false;
        // -
        if (s[4] != '-') return false;
        // MM
        if (!IsDigit(s[5]) || !IsDigit(s[6])) return false;
        // -
        if (s[7] != '-') return false;
        // dd
        if (!IsDigit(s[8]) || !IsDigit(s[9])) return false;
        // space
        if (s[10] != ' ') return false;
        // HH
        if (!IsDigit(s[11]) || !IsDigit(s[12])) return false;
        // :
        if (s[13] != ':') return false;
        // mm
        if (!IsDigit(s[14]) || !IsDigit(s[15])) return false;
        // :
        if (s[16] != ':') return false;
        // ss
        if (!IsDigit(s[17]) || !IsDigit(s[18])) return false;

        return true;
    }


	public static LogLine SliceMessage(string message) => SliceMessageV2(message);

	public static LogLine SliceMessageV1(string message) {
		var posOpen = message.IndexOf('[');
		var posClose = message.IndexOf(']');

		if (posOpen < 0 || posOpen > posClose) {
			return new LogLine(DateTime.Now, level: null, message);
		}

		var timestampPart = message[..posOpen].Trim();
		string msg = string.Empty;
		Level? level;
		DateTime timestamp;
		if (dateTimeRegex.IsMatch(timestampPart)) {
			timestamp = Convert.ToDateTime(timestampPart);
		} else if (!message.TrimStart().StartsWith('[')) {
			timestamp = DateTime.Now;
			msg = message;
			level = null;
			return new LogLine(timestamp, level, msg);
		}

		timestamp = DateTime.Now;
		var logLevelStr = message.Substring(posOpen + 1, posClose - posOpen - 1);
		level = GetLogLevelV1(logLevelStr);
		if (message.Length >= posClose + 2) {
			msg = message[(posClose + 2)..];
		}

		return new LogLine(timestamp, level, msg);
	}



	private static Level GetLogLevelV1(string levelStr) { // TODO: remove this after switching to V2
		if (levelStr.Equals("WARNING", StringComparison.OrdinalIgnoreCase)) {
			levelStr = "WARN";
		}
		var level = LogManager.GetRepository().LevelMap[levelStr];
		if (level == null) {
			logger.Warn($"Unknown log level: {levelStr}");
			level = Level.Debug;
		}

		return level;
	}

	private static readonly Regex dateTimeRegex = GetDateTimeRegex(); // TODO: remove this after switching to V2
	[GeneratedRegex(@"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})$")]
	private static partial Regex GetDateTimeRegex(); // TODO: remove this after switching to V2
}