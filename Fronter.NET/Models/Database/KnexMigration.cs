namespace Fronter.Models.Database;

public partial class KnexMigration
{
    public long Id { get; set; }

    public string? Name { get; set; }

    public long? Batch { get; set; }

    public byte[]? MigrationTime { get; set; }
}
