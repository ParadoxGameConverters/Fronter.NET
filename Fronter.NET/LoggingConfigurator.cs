using commonItems;
using Fronter.LogAppenders;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using System;

namespace Fronter;

public static class LoggingConfigurator {
	public static void ConfigureLogging(bool useConsole = false) {
		var repository = LogManager.GetRepository();
		var hierarchy = (Hierarchy)repository;
        hierarchy.Root.RemoveAllAppenders();

		// Add custom "PROGRESS" level.
		repository.LevelMap.Add(LogExtensions.ProgressLevel);

        var layout = new PatternLayout {
            ConversionPattern = "%date{yyyy'-'MM'-'dd HH':'mm':'ss} [%level] %message%newline",
        };
        if (useConsole){
            var consoleAppender = new ConsoleAppender {
                Threshold = Level.All,
                Target = "Console.Out",
                Layout = layout,
            };
            consoleAppender.ActivateOptions();
            hierarchy.Root.AddAppender(consoleAppender);
        } else {
            layout.ActivateOptions();
            var fileAppender = new FileAppender {
                Name = "file",
                File = "log.txt",
                AppendToFile = false,
                Threshold = Level.All,
            };
            fileAppender.ActivateOptions();
            hierarchy.Root.AddAppender(fileAppender);

            var gridAppender = new LogGridAppender {
                Name = "grid",
                Threshold = Level.All,
            };
            gridAppender.ActivateOptions();
            hierarchy.Root.AddAppender(gridAppender);
        }

		hierarchy.Root.Level = Level.All;
		hierarchy.Configured = true;
	}
}