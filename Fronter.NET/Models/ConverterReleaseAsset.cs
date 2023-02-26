﻿using System.Text.Json.Serialization;

namespace Fronter.Models;

public class ConverterReleaseAsset {
	[JsonPropertyName("name")] public string? Name { get; set; }
	[JsonPropertyName("browser_download_url")] public string? BrowserDownloadUrl { get; set; }
}