namespace Fronter.Models.Database;

public partial class ModsDependency
{
    public string ModId { get; set; } = null!;

    public string ModVersion { get; set; } = null!;

    public string DependencyType { get; set; } = null!;

    public string DependencyId { get; set; } = null!;

    public string? DependencyVersion { get; set; }

    public string? DependencyName { get; set; }
}
