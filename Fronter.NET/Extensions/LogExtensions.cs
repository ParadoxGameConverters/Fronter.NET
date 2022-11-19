using log4net;
using log4net.Core;
using System;

namespace Fronter.Extensions; 

public static class LogExtensions {
	public static void LogWithCustomTimestamp(this ILog log, DateTime timestamp, Level level, string message) {
		var loggingEventDate = new LoggingEventData {
#pragma warning disable CS0618
			TimeStamp = timestamp, Level = level, Message = message
#pragma warning restore CS0618
		};
		var loggingEvent = new LoggingEvent(loggingEventDate);
		log.Logger.Log(loggingEvent);
	}
}