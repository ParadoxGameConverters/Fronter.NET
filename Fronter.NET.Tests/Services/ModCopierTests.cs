using Fronter.Models.Configuration;
using Fronter.Services;
using Microsoft.Data.Sqlite;
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

	[Fact]
	public void SetUpPlayset_WorksWhenCoverImageColumnsAreMissing() {
		var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		var gameDocsDir = Path.Combine(tempDir, "docs");
		var targetGameModsDir = Path.Combine(gameDocsDir, "mod");
		var converterDir = Path.Combine(tempDir, "converter");
		Directory.CreateDirectory(targetGameModsDir);
		Directory.CreateDirectory(converterDir);

		try {
			var config = new Config();
			SetConverterFolder(config, converterDir);
			var targetGameFolder = config.RequiredFolders.First(f =>
				string.Equals(f.Name, "targetGameModPath", StringComparison.OrdinalIgnoreCase));
			targetGameFolder.Value = targetGameModsDir;

			var dbPath = Path.Combine(gameDocsDir, "launcher-v2.sqlite");
			CreateLauncherDbWithoutCoverColumns(dbPath);

			var modCopier = new ModCopier(config);
			var targetModName = "testmod";
			var destModFolder = Path.Combine(targetGameModsDir, targetModName);

			InvokeSetUpPlayset(modCopier, targetGameModsDir, targetModName, destModFolder);
			AssertPlaysetCreated(dbPath, $"{config.Name}: {targetModName}");

			// Running SetUpPlayset again should update the existing playset instead of failing.
			InvokeSetUpPlayset(modCopier, targetGameModsDir, targetModName, destModFolder);
			AssertPlaysetCreated(dbPath, $"{config.Name}: {targetModName}");
		} finally {
			// Release pooled SQLite connections (from LauncherDbContext) that may still hold the database file open.
			SqliteConnection.ClearAllPools();
			Cleanup(tempDir);
		}
	}

	private static void InvokeSetUpPlayset(ModCopier modCopier, string targetModsDirectory, string targetModName, string destModFolder) {
		var method = typeof(ModCopier).GetMethod("SetUpPlayset", BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.NotNull(method);
		method.Invoke(modCopier, new object[] { targetModsDirectory, targetModName, destModFolder });
	}

	private static void AssertPlaysetCreated(string dbPath, string expectedPlaysetName) {
		using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=False");
		connection.Open();

		var countCommand = connection.CreateCommand();
		countCommand.CommandText = "SELECT COUNT(*) FROM playsets;";
		Assert.Equal(1L, countCommand.ExecuteScalar());

		var nameCommand = connection.CreateCommand();
		nameCommand.CommandText = "SELECT name, isActive FROM playsets;";
		using var reader = nameCommand.ExecuteReader();
		Assert.True(reader.Read());
		Assert.Equal(expectedPlaysetName, reader.GetString(0));
		Assert.Equal(1L, reader.GetInt64(1));
	}

	private static void CreateLauncherDbWithoutCoverColumns(string dbPath) {
		using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=False");
		connection.Open();

		var createTableCommand = connection.CreateCommand();
		createTableCommand.CommandText = """
			CREATE TABLE "playsets" (
				"id" char(36) NOT NULL,
				"name" varchar(255) NOT NULL,
				"isActive" boolean,
				"loadOrder" varchar(255),
				"pdxId" INT,
				"pdxUserId" char(36),
				"createdOn" datetime NOT NULL,
				"updatedOn" datetime,
				"syncedOn" datetime,
				"deprecatedLastServerChecksum" varchar(255),
				"isRemoved" boolean DEFAULT false,
				"hasNotApprovedChanges" boolean DEFAULT '0',
				"syncState" varchar(255),
				"state" varchar(255) DEFAULT 'private' NOT NULL,
				"owned" boolean DEFAULT '1' NOT NULL,
				"author" varchar(255) DEFAULT '' NOT NULL,
				"subscribersCount" integer DEFAULT '0' NOT NULL,
				"ratingsCount" integer DEFAULT '0' NOT NULL,
				"description" varchar(255) DEFAULT '',
				"offDisk" boolean DEFAULT '0' NOT NULL,
				"version" varchar(255),
				"lastSyncAttemptAt" datetime
			);
			CREATE TABLE "mods" (
				"id" char(36) NOT NULL,
				"pdxId" varchar(255),
				"steamId" varchar(255),
				"gameRegistryId" varchar(255),
				"name" varchar(255),
				"displayName" varchar(255),
				"descriptionDeprecated" varchar(255),
				"thumbnailUrl" varchar(255),
				"thumbnailPath" varchar(255),
				"version" varchar(255),
				"tags" json,
				"requiredVersion" varchar(255),
				"arch" varchar(255),
				"os" varchar(255),
				"repositoryPath" varchar(255),
				"dirPath" varchar(255),
				"archivePath" varchar(255),
				"status" varchar(255) NOT NULL,
				"source" varchar(255) NOT NULL,
				"cause" varchar(255),
				"timeUpdated" integer,
				"isNew" boolean,
				"createdDate" datetime,
				"subscribedDate" datetime,
				"size" integer,
				"metadataId" varchar(255),
				"remotePdxId" varchar(255),
				"remoteSteamId" varchar(255),
				"metadataVersion" varchar(255),
				"metadataStatus" varchar(255) DEFAULT 'not_applied',
				"metadataGameId" varchar(255),
				"descriptionPdx" varchar(255),
				"descriptionSteam" varchar(255),
				"shortDescriptionPdx" varchar(255),
				"keepLatest" boolean DEFAULT '1',
				"userVersion" boolean,
				"remotePdxUserId" varchar(255),
				"remoteSteamUserId" varchar(255)
			);
			CREATE TABLE "playsets_mods" (
				"playsetId" char(36) NOT NULL,
				"modId" char(36) NOT NULL,
				"enabled" boolean DEFAULT '1',
				"position" integer
			);
			""";
		createTableCommand.ExecuteNonQuery();
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