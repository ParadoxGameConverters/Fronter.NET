using log4net;
using log4net.Core;
using System;

namespace Fronter.Extensions;

public static class LogExtensions {
	public static void LogWithCustomTimestamp(this ILog log, DateTime timestamp, Level level, string message) {
		var loggingEventDate = new LoggingEventData {
			// The incoming timeStamp is in local timezone, but Log4Net expects it to be in UTC.
			// So we need to convert it to UTC.
			TimeStampUtc = timestamp.ToUniversalTime(), Level = level, Message = message,
		};
		var loggingEvent = new LoggingEvent(loggingEventDate);
		log.Logger.Log(loggingEvent);
	}
}