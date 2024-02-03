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
		// Identify user by username or IP address.
		string? ip = (await GetExternalIpAddress())?.ToString();
		SentrySdk.ConfigureScope(scope => {
			scope.User = ip is null ? new() {Username = Environment.UserName} : new() {IpAddress = ip};
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

	private static async Task<IPAddress?> GetExternalIpAddress() {
		try {
			var externalIpString = (await new HttpClient().GetStringAsync("https://icanhazip.com/"))
				.Replace(@"\r", "")
				.Replace(@"\n", "")
				.Trim();
			return !IPAddress.TryParse(externalIpString, out var ipAddress) ? null : ipAddress;
		} catch (Exception e) {
			SentrySdk.AddBreadcrumb($"Failed to get IP address: {e.Message}");
			return null;
		}
	}
}