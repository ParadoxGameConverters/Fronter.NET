using commonItems;
using System.IO;

namespace Fronter.Services;

public class LogWatcher {
	void Entry() {
		using var reader = new StreamReader(tailSource);

		var oneLastRun = false;
		while (!terminate || oneLastRun) {
			while (!reader.EndOfStream) {
				var line = reader.ReadLine();
				if (line is not null) {
					var logMessage = MessageSlicer.SliceMessage(line);
					if (transcriberMode) {
						Logger.Debug(logMessage.Message); // TODO: MAKE LEVEL MATCH logMessage.LogLevel
					}
				}
			}
		}
	}

	private string tailSource = string.Empty;
	private bool terminate = false;
	private bool transcriberMode = false;
	private bool emitterMode = false;
}