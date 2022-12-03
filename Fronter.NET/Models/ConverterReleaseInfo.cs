using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fronter.Models; 

public class ConverterReleaseInfo {
	[JsonPropertyName("body")] public string? Body { get; }
	[JsonPropertyName("name")] public string? Name { get; }
	[JsonPropertyName("assets")] public List<ConverterReleaseAsset>? Assets { get; } = new();
}