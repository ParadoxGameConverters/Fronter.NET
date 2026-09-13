namespace Fronter.Models.Database;

public partial class KnexMigrationsLock
{
    public long Index { get; set; }

    public long? IsLocked { get; set; }
}
