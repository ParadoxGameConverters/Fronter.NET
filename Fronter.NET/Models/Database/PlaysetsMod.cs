using System;
using System.Collections.Generic;

namespace Fronter.Models.Database;

public partial class PlaysetsMod
{
    public string PlaysetId { get; set; } = null!;

    public string ModId { get; set; } = null!;

    public byte[]? Enabled { get; set; }

    public long? Position { get; set; }

    public virtual Mod Mod { get; set; } = null!;

    public virtual Playset Playset { get; set; } = null!;
}
