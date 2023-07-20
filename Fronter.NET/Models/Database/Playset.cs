using System;
using System.Collections.Generic;

namespace Fronter.Models.Database;

public partial class Playset
{
    public string Id { get; set; } = null!;

    public string Name { get; set; } = null!;

    public bool? IsActive { get; set; }

    public string? LoadOrder { get; set; }

    public long? PdxId { get; set; }

    public string? PdxUserId { get; set; }

    public string CreatedOn { get; set; } = null!;

    public string? UpdatedOn { get; set; }

    public byte[]? SyncedOn { get; set; }

    public string? LastServerChecksum { get; set; }

    public bool? IsRemoved { get; set; } = false;

    public bool? HasNotApprovedChanges { get; set; } = false;

    public string? SyncState { get; set; }
}
