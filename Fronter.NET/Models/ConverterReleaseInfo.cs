using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fronter.Models;

internal sealed class ConverterReleaseInfo {
	[JsonPropertyName("body")] public string? Body { get; set; }
	[JsonPropertyName("name")] public string? Name { get; set; }
	[JsonPropertyName("tag_name")] public string? TagName { get; set; }
	[JsonPropertyName("prerelease")] public bool Prerelease { get; set; }
	[JsonPropertyName("draft")] public bool Draft { get; set; }
	[JsonPropertyName("assets")] public List<ConverterReleaseAsset> Assets { get; set; } = [];
}