using Fronter.Models.Configuration;
using Fronter.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Fronter.Tests.Services;

public class ModCopierTests {
	[Fact]
	public void LoadPlaysetInfo_UnquotesModNamesContainingQuotes() {
		var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);

		try {
			var config = new Config();
			SetConverterFolder(config, tempDir);
			File.WriteAllText(Path.Combine(tempDir, "playset_info.txt"), "\"More \\\"This is you\\\" Flavor\"=\"mod/more_this_is_you_flavor.mod\"");

			var playsetInfo = InvokeLoadPlaysetInfo(new ModCopier(config));
			var entry = playsetInfo.Single();

			Assert.Equal("More \"This is you\" Flavor", entry.Key);
			Assert.Equal("mod/more_this_is_you_flavor.mod", entry.Value);
		} finally {
			Cleanup(tempDir);
		}
	}

	[Fact]
	public void LoadPlaysetInfo_ParsesQuotedAndRelativeModEntries() {
		var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);

		try {
			var config = new Config();
			SetConverterFolder(config, tempDir);
			var playsetInfoContent = string.Join(Environment.NewLine,
				"\"More \\\"This is you\\\" Flavor\"=\"C:/Program Files (x86)/Steam/steamapps/workshop/content/1158310/3338373660\"",
				"\"Converted - test_save\"=\"test_save\"");
			File.WriteAllText(Path.Combine(tempDir, "playset_info.txt"), playsetInfoContent);

			var playsetInfo = InvokeLoadPlaysetInfo(new ModCopier(config));

			Assert.Equal(2, playsetInfo.Count);
			Assert.Equal("C:/Program Files (x86)/Steam/steamapps/workshop/content/1158310/3338373660", playsetInfo["More \"This is you\" Flavor"]);
			Assert.Equal("test_save", playsetInfo["Converted - test_save"]);
		} finally {
			Cleanup(tempDir);
		}
	}

	private static OrderedDictionary<string, string> InvokeLoadPlaysetInfo(ModCopier modCopier) {
		var method = typeof(ModCopier).GetMethod("LoadPlaysetInfo", BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.NotNull(method);

		return Assert.IsType<OrderedDictionary<string, string>>(method.Invoke(modCopier, null));
	}

	private static void SetConverterFolder(Config config, string converterFolder) {
		var field = typeof(Config).GetField("<ConverterFolder>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.NotNull(field);
		field.SetValue(config, converterFolder);
	}

	private static void Cleanup(string path) {
		if (!Directory.Exists(path)) {
			return;
		}

		Directory.Delete(path, recursive: true);
	}
}