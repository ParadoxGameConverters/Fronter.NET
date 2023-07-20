using Fronter.Models.Database;
using Microsoft.EntityFrameworkCore;

namespace Fronter;

public partial class LauncherDbContext : DbContext {
	private readonly string connectionString;
	public LauncherDbContext(string connectionString) {
		this.connectionString = connectionString;
	}

    public virtual DbSet<Dlc> Dlcs { get; set; }

    public virtual DbSet<KeyValuePair> KeyValuePairs { get; set; }

    public virtual DbSet<KnexMigration> KnexMigrations { get; set; }

    public virtual DbSet<KnexMigrationsLock> KnexMigrationsLocks { get; set; }

    public virtual DbSet<MetadataRelationship> MetadataRelationships { get; set; }

    public virtual DbSet<Mod> Mods { get; set; }

    public virtual DbSet<ModsDependency> ModsDependencies { get; set; }

    public virtual DbSet<Playset> Playsets { get; set; }

    public virtual DbSet<PlaysetsMod> PlaysetsMods { get; set; }

    public virtual DbSet<Ugc> Ugcs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite(connectionString);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Dlc>(entity =>
        {
            entity.ToTable("dlc");

            entity.Property(e => e.Id)
                .HasColumnType("char(36)")
                .HasColumnName("id");
            entity.Property(e => e.DirPath)
                .HasColumnType("varchar(255)")
                .HasColumnName("dirPath");
            entity.Property(e => e.Name)
                .HasColumnType("varchar(255)")
                .HasColumnName("name");
        });

        modelBuilder.Entity<KeyValuePair>(entity =>
        {
            entity.HasKey(e => e.Name);

            entity.ToTable("key_value_pairs");

            entity.Property(e => e.Name)
                .HasColumnType("varchar(255)")
                .HasColumnName("name");
            entity.Property(e => e.Value)
                .HasColumnType("varchar(255)")
                .HasColumnName("value");
        });

        modelBuilder.Entity<KnexMigration>(entity =>
        {
            entity.ToTable("knex_migrations");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Batch).HasColumnName("batch");
            entity.Property(e => e.MigrationTime)
                .HasColumnType("datetime")
                .HasColumnName("migration_time");
            entity.Property(e => e.Name)
                .HasColumnType("varchar(255)")
                .HasColumnName("name");
        });

        modelBuilder.Entity<KnexMigrationsLock>(entity =>
        {
            entity.HasKey(e => e.Index);

            entity.ToTable("knex_migrations_lock");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.IsLocked).HasColumnName("is_locked");
        });

        modelBuilder.Entity<MetadataRelationship>(entity =>
        {
            entity.ToTable("metadata_relationships");

            entity.Property(e => e.Id)
                .HasColumnType("char(36)")
                .HasColumnName("id");
            entity.Property(e => e.DisplayName)
                .HasColumnType("varchar(255)")
                .HasColumnName("displayName");
            entity.Property(e => e.ModMetadataId)
                .HasColumnType("varchar(255)")
                .HasColumnName("modMetadataId");
            entity.Property(e => e.RelationshipId)
                .HasColumnType("varchar(255)")
                .HasColumnName("relationshipId");
            entity.Property(e => e.RelationshipType).HasColumnName("relationshipType");
            entity.Property(e => e.RequiredVersion)
                .HasColumnType("varchar(255)")
                .HasColumnName("requiredVersion");
            entity.Property(e => e.ResourceType).HasColumnName("resourceType");
        });

        modelBuilder.Entity<Mod>(entity =>
        {
            entity.ToTable("mods");

            entity.Property(e => e.Id)
                .HasColumnType("char(36)")
                .HasColumnName("id");
            entity.Property(e => e.Arch).HasColumnName("arch");
            entity.Property(e => e.ArchivePath).HasColumnName("archivePath");
            entity.Property(e => e.Cause).HasColumnName("cause");
            entity.Property(e => e.CreatedDate)
                .HasColumnType("datetime")
                .HasColumnName("createdDate");
            entity.Property(e => e.DescriptionDeprecated).HasColumnName("descriptionDeprecated");
            entity.Property(e => e.DescriptionPdx)
                .HasColumnType("varchar(255)")
                .HasColumnName("descriptionPdx");
            entity.Property(e => e.DescriptionSteam)
                .HasColumnType("varchar(255)")
                .HasColumnName("descriptionSteam");
            entity.Property(e => e.DirPath).HasColumnName("dirPath");
            entity.Property(e => e.DisplayName)
                .HasColumnType("varchar(255)")
                .HasColumnName("displayName");
            entity.Property(e => e.GameRegistryId).HasColumnName("gameRegistryId");
            entity.Property(e => e.IsNew)
                .HasColumnType("boolean")
                .HasColumnName("isNew");
            entity.Property(e => e.MetadataGameId)
                .HasColumnType("varchar(255)")
                .HasColumnName("metadataGameId");
            entity.Property(e => e.MetadataId)
                .HasColumnType("varchar(255)")
                .HasColumnName("metadataId");
            entity.Property(e => e.MetadataStatus)
                .HasDefaultValueSql("'not_applied'")
                .HasColumnName("metadataStatus");
            entity.Property(e => e.MetadataVersion)
                .HasColumnType("varchar(255)")
                .HasColumnName("metadataVersion");
            entity.Property(e => e.Name)
                .HasColumnType("varchar(255)")
                .HasColumnName("name");
            entity.Property(e => e.Os).HasColumnName("os");
            entity.Property(e => e.PdxId)
                .HasColumnType("varchar(255)")
                .HasColumnName("pdxId");
            entity.Property(e => e.RemotePdxId)
                .HasColumnType("varchar(255)")
                .HasColumnName("remotePdxId");
            entity.Property(e => e.RemoteSteamId)
                .HasColumnType("varchar(255)")
                .HasColumnName("remoteSteamId");
            entity.Property(e => e.RepositoryPath).HasColumnName("repositoryPath");
            entity.Property(e => e.RequiredVersion)
                .HasColumnType("varchar(255)")
                .HasColumnName("requiredVersion");
            entity.Property(e => e.ShortDescriptionPdx)
                .HasColumnType("varchar(255)")
                .HasColumnName("shortDescriptionPdx");
            entity.Property(e => e.Size).HasColumnName("size");
            entity.Property(e => e.Source).HasColumnName("source");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.SteamId)
                .HasColumnType("varchar(255)")
                .HasColumnName("steamId");
            entity.Property(e => e.SubscribedDate)
                .HasColumnType("datetime")
                .HasColumnName("subscribedDate");
            entity.Property(e => e.Tags)
                .HasDefaultValueSql("'[]'")
                .HasColumnType("json")
                .HasColumnName("tags");
            entity.Property(e => e.ThumbnailPath).HasColumnName("thumbnailPath");
            entity.Property(e => e.ThumbnailUrl).HasColumnName("thumbnailUrl");
            entity.Property(e => e.TimeUpdated).HasColumnName("timeUpdated");
            entity.Property(e => e.Version)
                .HasColumnType("varchar(255)")
                .HasColumnName("version");
        });

        modelBuilder.Entity<ModsDependency>(entity =>
        {
            entity.HasKey(e => new { e.ModId, e.ModVersion, e.DependencyId });

            entity.ToTable("mods_dependencies");

            entity.Property(e => e.ModId)
                .HasColumnType("char(36)")
                .HasColumnName("modId");
            entity.Property(e => e.ModVersion)
                .HasColumnType("varchar(255)")
                .HasColumnName("modVersion");
            entity.Property(e => e.DependencyId)
                .HasColumnType("char(36)")
                .HasColumnName("dependencyId");
            entity.Property(e => e.DependencyName)
                .HasColumnType("varchar(255)")
                .HasColumnName("dependencyName");
            entity.Property(e => e.DependencyType).HasColumnName("dependencyType");
            entity.Property(e => e.DependencyVersion)
                .HasColumnType("varchar(255)")
                .HasColumnName("dependencyVersion");
        });

        modelBuilder.Entity<Playset>(entity =>
        {
            entity.ToTable("playsets");

            entity.HasIndex(e => e.PdxId, "IX_playsets_pdxId").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("char(36)")
                .HasColumnName("id");
            entity.Property(e => e.CreatedOn)
                .HasColumnType("datetime")
                .HasColumnName("createdOn");
            entity.Property(e => e.HasNotApprovedChanges)
                .HasDefaultValueSql("'0'")
                .HasColumnType("boolean")
                .HasColumnName("hasNotApprovedChanges");
            entity.Property(e => e.IsActive)
                .HasColumnType("boolean")
                .HasColumnName("isActive");
            entity.Property(e => e.IsRemoved)
                .HasDefaultValueSql("false")
                .HasColumnType("boolean")
                .HasColumnName("isRemoved");
            entity.Property(e => e.LastServerChecksum).HasColumnName("lastServerChecksum");
            entity.Property(e => e.LoadOrder)
                .HasColumnType("varchar(255)")
                .HasColumnName("loadOrder");
            entity.Property(e => e.Name)
                .HasColumnType("varchar(255)")
                .HasColumnName("name");
            entity.Property(e => e.PdxId)
                .HasColumnType("INT")
                .HasColumnName("pdxId");
            entity.Property(e => e.PdxUserId)
                .HasColumnType("char(36)")
                .HasColumnName("pdxUserId");
            entity.Property(e => e.SyncState)
                .HasColumnType("varchar(255)")
                .HasColumnName("syncState");
            entity.Property(e => e.SyncedOn)
                .HasColumnType("datetime")
                .HasColumnName("syncedOn");
            entity.Property(e => e.UpdatedOn)
                .HasColumnType("datetime")
                .HasColumnName("updatedOn");
        });

        modelBuilder.Entity<PlaysetsMod>(entity =>
        {
	        entity.ToTable("playsets_mods");
	        entity.HasKey(m => new {m.PlaysetId, m.ModId});

            entity.Property(e => e.Enabled)
                .HasDefaultValueSql("'1'")
                .HasColumnType("boolean")
                .HasColumnName("enabled");
            entity.Property(e => e.ModId)
                .HasColumnType("char(36)")
                .HasColumnName("modId");
            entity.Property(e => e.PlaysetId)
                .HasColumnType("char(36)")
                .HasColumnName("playsetId");
            entity.Property(e => e.Position).HasColumnName("position");

            entity.HasOne(d => d.Mod).WithMany().HasForeignKey(d => d.ModId);

            entity.HasOne(d => d.Playset).WithMany().HasForeignKey(d => d.PlaysetId);
        });

        modelBuilder.Entity<Ugc>(entity =>
        {
            entity.HasKey(e => e.Name);

            entity.ToTable("ugc");

            entity.Property(e => e.Name)
                .HasColumnType("varchar(255)")
                .HasColumnName("name");
            entity.Property(e => e.DisplayName)
                .HasColumnType("varchar(255)")
                .HasColumnName("displayName");
            entity.Property(e => e.ShortDescription)
                .HasColumnType("varchar(255)")
                .HasColumnName("shortDescription");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
