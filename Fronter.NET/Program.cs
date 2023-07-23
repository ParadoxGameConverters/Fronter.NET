﻿using Avalonia;
using Avalonia.ReactiveUI;
using Sentry;
using System;

namespace Fronter;

internal static class Program {
	// Initialization code. Don't use any Avalonia, third-party APIs or any
	// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
	// yet and stuff might break.
	[STAThread]
	public static void Main(string[] args) {
		SentrySdk.Init(options => {
			// A Sentry Data Source Name (DSN) is required.
			// See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
			// You can set it in the SENTRY_DSN environment variable, or you can set it in code here.
			options.Dsn = "https://1663c8c97b29484c933826e33b81d4a9@o4505568167460864.ingest.sentry.io/4505568179257344";

			// This option is recommended. It enables Sentry's "Release Health" feature.
			options.AutoSessionTracking = true;

			// This option is recommended for client applications only. It ensures all threads use the same global scope.
			// If you're writing a background service of any kind, you should remove this.
			options.IsGlobalModeEnabled = true;

			// This option will enable Sentry's tracing features. You still need to start transactions and spans.
			options.EnableTracing = true;

			options.MaxBreadcrumbs = 10000;
			options.MaxAttachmentSize = long.MaxValue;
			
#if DEBUG
			options.Environment = "Debug";
#else
			options.Environment = "Release"; 
#endif
		});
		SentrySdk.ConfigureScope(scope => scope.AddAttachment("log.txt"));
		
        var app = BuildAvaloniaApp();
        app.StartWithClassicDesktopLifetime(args);
    }

	// Avalonia configuration, don't remove; also used by visual designer.
	public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.LogToTrace()
			.UseReactiveUI();
}