using log4net;
using log4net.Core;
using System;

namespace Fronter.Extensions; 

public static class LogExtensions {
	public static void LogWithCustomTimestamp(this ILog log, DateTime timestamp, Level level, string message) {
		var loggingEventDate = new LoggingEventData {
			TimeStamp = timestamp, Level = level, Message = message
		};
		var loggingEvent = new LoggingEvent(loggingEventDate);
		log.Logger.Log(loggingEvent);
	}
}