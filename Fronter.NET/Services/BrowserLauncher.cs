using System.Diagnostics;

namespace Fronter.Services;

public static class BrowserLauncher {
	public static void Open(string url) {
		Process.Start(new ProcessStartInfo {
			FileName = url,
			UseShellExecute = true
		});
	}
}