using Fronter.LogAppenders;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using System.Collections.Generic;
using Logger = commonItems.Logger;

namespace Fronter;

public static class LoggingConfigurator {
	public static void ConfigureLogging(bool useConsole = false) {
		var appenders = new List<IAppender>();

        var layout = new PatternLayout {
            ConversionPattern = "%date{yyyy'-'MM'-'dd HH':'mm':'ss} [%level] %message%newline",
        };
        if (useConsole) {
            var consoleAppender = new ConsoleAppender {
                Threshold = Level.All,
                Target = "Console.Out",
                Layout = layout,
            };
            consoleAppender.ActivateOptions();
            appenders.Add(consoleAppender);
        } else {
            layout.ActivateOptions();
            var fileAppender = new FileAppender {
                Name = "file",
                File = "log.txt",
                AppendToFile = false,
                Threshold = Level.All,
                Layout = layout,
            };
            fileAppender.ActivateOptions();
			appenders.Add(fileAppender);

            var gridAppender = new LogGridAppender {
                Name = "grid",
                Threshold = Level.All,
            };
            gridAppender.ActivateOptions();
			appenders.Add(gridAppender);
        }

		Logger.Configure(appenders);
	}
}