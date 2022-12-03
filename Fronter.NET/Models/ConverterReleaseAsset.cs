using System.Text.Json.Serialization;

namespace Fronter.Models; 

public class ConverterReleaseAsset {
	[JsonPropertyName("name")] public string Name { get; }
	[JsonPropertyName("name")] public string BrowserDownloadUrl { get; }
}