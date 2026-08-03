using System;

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

    public string? DeprecatedLastServerChecksum { get; set; }

    public bool? IsRemoved { get; set; } = false;

    public bool? HasNotApprovedChanges { get; set; } = false;

    public string? SyncState { get; set; }

    public string State { get; set; } = null!;

    public bool? Owned { get; set; }

    public string Author { get; set; } = null!;

    public int SubscribersCount { get; set; }

    public int RatingsCount { get; set; }

    public string? CoverImagePath { get; set; }

    public string? Description { get; set; }

    public bool? OffDisk { get; set; }

    public string? Version { get; set; }

    public string? LastSyncAttemptAt { get; set; }

    public DateTime? CoverImageUpdatedOn { get; set; }
}
