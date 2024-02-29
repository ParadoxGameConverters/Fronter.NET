using Fronter.LogAppenders;
using Fronter.Models;
using log4net;
using log4net.Core;
using Sentry;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fronter.Services;

public static class SentryHelper {
	public static void SendMessageToSentry(int processExitCode) {
		LogLine? error = GetFirstErrorLogLineFromGrid();
		if (error is not null) {
			var sentryMessageLevel = error.Level == Level.Fatal ? SentryLevel.Fatal : SentryLevel.Error;
			SendMessageToSentry(error.Message, sentryMessageLevel);
		} else {
			var message = $"Converter exited with code {processExitCode}";
			SendMessageToSentry(message, SentryLevel.Error);
		}
	}
	
	public static async void SendMessageToSentry(string message, SentryLevel level) {
		// Identify user by IP address and machine/user name.
		string name = Environment.MachineName;
		if (string.IsNullOrWhiteSpace(name)) {
			name = Environment.UserName;
		}
		SentrySdk.ConfigureScope(scope => {
			scope.User = new() {Username = name, IpAddress = "{{auto}}"};
		});

		SentrySdk.CaptureMessage(message, level);
	}

	private static LogLine? GetFirstErrorLogLineFromGrid() {
		var gridAppender = LogManager.GetRepository().GetAppenders().First(a => a.Name == "grid");
		if (gridAppender is LogGridAppender logGridAppender) {
			return logGridAppender.LogLines
				.FirstOrDefault(l => l.Level is not null && l.Level >= Level.Error);
		}
		return null;
	}
}