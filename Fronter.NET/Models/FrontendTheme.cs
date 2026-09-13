using commonItems.Collections;

namespace Fronter.Models;

internal sealed class FrontendTheme : IIdentifiable<string> {
	public required string Id { get; init; }
	public required string LocKey { get; init; }
}