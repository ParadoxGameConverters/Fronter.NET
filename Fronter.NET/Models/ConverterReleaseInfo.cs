using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fronter.Models;

public sealed class ConverterReleaseInfo {
	[JsonPropertyName("body")] public string? Body { get; set; }
	[JsonPropertyName("name")] public string? Name { get; set; }
	[JsonPropertyName("assets")] public List<ConverterReleaseAsset> Assets { get; set; } = new();
}