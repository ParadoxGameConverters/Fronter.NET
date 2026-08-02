namespace Fronter.Models.Database;

public partial class PlaysetsDlc
{
    public string PlaysetId { get; set; } = null!;

    public string DlcId { get; set; } = null!;

    public bool? Enabled { get; set; }
}
