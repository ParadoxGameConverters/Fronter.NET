using commonItems;

namespace Fronter.Models;

public sealed class UpdateInfoModel {
	public string? Version { get; set; }
	public string? Description { get; set; }
	public string? AssetUrl { get; set; }
	public bool UseInstaller => CommonFunctions.GetExtension(AssetUrl ?? string.Empty).Equals("exe", System.StringComparison.OrdinalIgnoreCase);
}