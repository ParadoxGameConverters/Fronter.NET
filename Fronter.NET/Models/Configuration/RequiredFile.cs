﻿using Avalonia.Data;
using commonItems;
using Fronter.Extensions;
using log4net;
using System.IO;

namespace Fronter.Models.Configuration;

public class RequiredFile : RequiredPath {
	private static readonly ILog logger = LogManager.GetLogger("Required file");
	public RequiredFile(BufferedReader reader) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}

	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("name", reader => Name = reader.GetString());
		parser.RegisterKeyword("tooltip", reader => Tooltip = reader.GetString());
		parser.RegisterKeyword("displayName", reader => DisplayName = reader.GetString());
		parser.RegisterKeyword("mandatory", reader => Mandatory = reader.GetString() == "true");
		parser.RegisterKeyword("outputtable", reader => Outputtable = reader.GetString() == "true");

		parser.RegisterKeyword("searchPathType", reader => SearchPathType = reader.GetString());
		parser.RegisterKeyword("searchPath", reader => SearchPath = reader.GetString());
		parser.RegisterKeyword("fileName", reader => FileName = reader.GetString());
		parser.RegisterKeyword("allowedExtension", reader => AllowedExtension = reader.GetString());
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}

	public string FileName { get; private set; } = string.Empty;
	public string AllowedExtension { get; private set; } = string.Empty;
	public string? InitialDirectory { get; set; }
	
	public override string Value {
		get => base.Value;
		set {
			if (!string.IsNullOrEmpty(value) && !File.Exists(value)) {
				throw new DataValidationException("File does not exist!");
			}

			base.Value = value;
			logger.Info($"{TranslationSource.Instance[DisplayName]} set to: {value}");
		}
	}
}