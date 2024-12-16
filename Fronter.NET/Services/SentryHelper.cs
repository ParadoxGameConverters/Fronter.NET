using commonItems;
using Fronter.LogAppenders;
using Fronter.Models;
using Fronter.Models.Configuration;
using log4net;
using log4net.Core;
using Sentry;
using System;
using System.IO;
using System.Linq;

namespace Fronter.Services;

internal sealed class SentryHelper {
	public SentryHelper(Config config) {
		this.config = config;
		InitSentry();
	}

	private readonly Config config;

	private void InitSentry() {
		string? release = null;
		// Try to get version from converter's version.txt
		var versionFilePath = Path.Combine(config.ConverterFolder, "configurables/version.txt");
		if (File.Exists(versionFilePath)) {
			var version = new ConverterVersion();
			version.LoadVersion(versionFilePath);
			if (!string.IsNullOrWhiteSpace(version.Version) && !string.IsNullOrWhiteSpace(config.Name)) {
				release = $"{config.Name}@{version.Version}";
			}
		}

		if (release is null) {
			Logger.Debug("Skipping Sentry initialization because converter version could not be determined.");
			return;
		}

		SentrySdk.Init(options => {
			// A Sentry Data Source Name (DSN) is required.
			// See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
			options.Dsn = config.SentryDsn;

			// This option enables Sentry's "Release Health" feature.
			options.AutoSessionTracking = false;

			// This option is recommended for client applications only. It ensures all threads use the same global scope.
			// If you're writing a background service of any kind, you should remove this.
			options.IsGlobalModeEnabled = true;

			options.AttachStacktrace = false;

			options.MaxBreadcrumbs = int.MaxValue;
			options.MaxAttachmentSize = long.MaxValue;

			options.Release = release;
#if DEBUG
			options.Environment = "Debug";
#else
			options.Environment = "Release";
#endif
		});
		Logger.Debug("Sentry initialized.");
	}

	public void AddBreadcrumb(string text) => SentrySdk.AddBreadcrumb(text);

	public void AddAttachment(string filePath) => SentrySdk.ConfigureScope(scope => scope.AddAttachment(filePath));

	public void SendMessageToSentry(int processExitCode) {
		LogLine? error = GetFirstErrorLogLineFromGrid();
		if (error is not null) {
			var sentryMessageLevel = error.Level == Level.Fatal ? SentryLevel.Fatal : SentryLevel.Error;
			SendMessageToSentry(error.Message, sentryMessageLevel);
		} else {
			var message = $"Converter exited with code {processExitCode}";
			SendMessageToSentry(message, SentryLevel.Error);
		}
	}

	public void SendMessageToSentry(string message, SentryLevel level) {
		// Identify user by IP address and machine/user name.
		string name = Environment.MachineName;
		if (string.IsNullOrWhiteSpace(name)) {
			name = Environment.UserName;
		}
		SentrySdk.ConfigureScope(scope => scope.User = new() { Username = name, IpAddress = "{{auto}}" });

		SentrySdk.CaptureMessage(message, level);
	}

	private static LogLine? GetFirstErrorLogLineFromGrid() {
		var gridAppender = LogManager.GetRepository().GetAppenders()
			.First(a => string.Equals(a.Name, "grid", StringComparison.OrdinalIgnoreCase));
		if (gridAppender is LogGridAppender logGridAppender) {
			return logGridAppender.LogLines
				.FirstOrDefault(l => l.Level is not null && l.Level >= Level.Error);
		}
		return null;
	}
}