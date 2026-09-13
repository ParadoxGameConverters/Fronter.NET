namespace Fronter.Models.Database;

public partial class MetadataRelationship
{
    public string Id { get; set; } = null!;

    public string ModMetadataId { get; set; } = null!;

    public string RelationshipId { get; set; } = null!;

    public string RelationshipType { get; set; } = null!;

    public string ResourceType { get; set; } = null!;

    public string? RequiredVersion { get; set; }

    public string? DisplayName { get; set; }
}
