using System;
using System.Collections.Generic;

namespace Fronter.Models.Database;

public partial class Mod
{
    public string Id { get; set; } = null!;

    public string? PdxId { get; set; }

    public string? SteamId { get; set; }

    public string? GameRegistryId { get; set; }

    public string? Name { get; set; }

    public string? DisplayName { get; set; }

    public string? DescriptionDeprecated { get; set; }

    public string? ThumbnailUrl { get; set; }

    public string? ThumbnailPath { get; set; }

    public string? Version { get; set; }

    public byte[]? Tags { get; set; }

    public string? RequiredVersion { get; set; }

    public string? Arch { get; set; }

    public string? Os { get; set; }

    public string? RepositoryPath { get; set; }

    public string? DirPath { get; set; }

    public string? ArchivePath { get; set; }

    public string Status { get; set; } = null!;

    public string Source { get; set; } = null!;

    public string? Cause { get; set; }

    public long? TimeUpdated { get; set; }

    public byte[]? IsNew { get; set; }

    public byte[]? CreatedDate { get; set; }

    public byte[]? SubscribedDate { get; set; }

    public long? Size { get; set; }

    public string? MetadataId { get; set; }

    public string? RemotePdxId { get; set; }

    public string? RemoteSteamId { get; set; }

    public string? MetadataVersion { get; set; }

    public string MetadataStatus { get; set; } = null!;

    public string? MetadataGameId { get; set; }

    public string? DescriptionPdx { get; set; }

    public string? DescriptionSteam { get; set; }

    public string? ShortDescriptionPdx { get; set; }
}
