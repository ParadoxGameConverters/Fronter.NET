using commonItems.Collections;

namespace Fronter.Models;

public sealed class FrontendTheme : IIdentifiable<string> {
	public required string Id { get; init; }
	public required string LocKey { get; init; }
}