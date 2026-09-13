using Avalonia.Media;
using Avalonia.Notification;
using Fronter.Models.Configuration;
using Fronter.Services;

namespace Fronter.Extensions;

internal static class NotificationMessageBuilderExtensions {
	public static NotificationMessageBuilder CreateError(this INotificationMessageManager manager) {
		return manager
			.CreateMessage()
			.Accent(Brushes.Red)
			.Animates(animates: true)
			.Background("#333")
			.HasBadge("Error");
	}
	public static NotificationMessageBuilder SuggestManualUpdate(this NotificationMessageBuilder builder, Config config) {
		if (!string.IsNullOrWhiteSpace(config.LatestGitHubConverterReleaseUrl)) {
			builder = builder
				.Dismiss().WithButton("See latest release", button => {
					BrowserLauncher.Open(config.LatestGitHubConverterReleaseUrl);
				});
		}
		if (!string.IsNullOrWhiteSpace(config.LatestGitHubConverterReleaseUrl)) {
			builder = builder
				.Dismiss().WithButton("See forum thread", button => {
					BrowserLauncher.Open(config.ConverterReleaseForumThread);
				});
		}
		return builder;
	}
}