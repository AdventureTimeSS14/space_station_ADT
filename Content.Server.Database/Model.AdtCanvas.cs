using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

internal static class ModelAdtCanvas
{
    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdtPainting>()
            .HasIndex(p => p.PaintingUid)
            .IsUnique();

        modelBuilder.Entity<AdtPainting>()
            .HasIndex(p => new { p.Status, p.SubmittedAt });

        modelBuilder.Entity<AdtPainting>()
            .HasIndex(p => new { p.OwnerUserId, p.Status });

        modelBuilder.Entity<AdtPainting>()
            .HasIndex(p => new { p.Status, p.ReviewedAt });

        modelBuilder.Entity<AdtPainting>()
            .HasOne(p => p.ParentPainting)
            .WithMany()
            .HasForeignKey(p => p.ParentPaintingId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AdtPainting>()
            .HasOne<Round>()
            .WithMany()
            .HasForeignKey(p => p.RoundId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AdtPainting>()
            .HasOne<Server>()
            .WithMany()
            .HasForeignKey(p => p.ServerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AdtPaintingRevision>()
            .HasOne(r => r.Painting)
            .WithMany(p => p.Revisions)
            .HasForeignKey(r => r.PaintingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AdtPaintingRevision>()
            .HasIndex(r => new { r.PaintingId, r.Kind });

        modelBuilder.Entity<AdtPaintingRevisionTile>()
            .HasKey(t => new { t.RevisionId, t.Layer, t.Lod, t.TileX, t.TileY });

        modelBuilder.Entity<AdtPaintingRevisionTile>()
            .HasOne(t => t.Revision)
            .WithMany(r => r.Tiles)
            .HasForeignKey(t => t.RevisionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AdtPaintingRevisionTile>()
            .HasOne<AdtPaintingBlob>()
            .WithMany()
            .HasForeignKey(t => t.BlobHash)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AdtPaintingRevisionTile>()
            .HasIndex(t => t.BlobHash);

        modelBuilder.Entity<AdtPaintingBlob>();
        modelBuilder.Entity<AdtPaintingBan>();
        modelBuilder.Entity<AdtPaintingWhitelist>();

        modelBuilder.Entity<AdtPaintingReport>()
            .HasIndex(r => new { r.Resolved, r.CreatedAt });

        modelBuilder.Entity<AdtPaintingReport>()
            .HasIndex(r => new { r.PaintingUid, r.ReporterUserId });

        modelBuilder.Entity<AdtPaintingReport>()
            .HasOne<Round>()
            .WithMany()
            .HasForeignKey(r => r.RoundId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AdtPaintingReport>()
            .HasOne<Server>()
            .WithMany()
            .HasForeignKey(r => r.ServerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AdtPaintingContributor>()
            .HasKey(c => new { c.PaintingId, c.UserId });

        modelBuilder.Entity<AdtPaintingContributor>()
            .HasOne(c => c.Painting)
            .WithMany(p => p.Contributors)
            .HasForeignKey(c => c.PaintingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public enum AdtPaintingStatus : short
{
    Draft,
    Submitted,
    Approved,
    Rejected,
    Hidden,
    Deleted,
    Expired,
}

public enum AdtPaintingRevisionKind : short
{
    Autosave,
    Submitted,
}

public enum AdtPaintingTileKind : short
{
    Solid = 1,
    Blob = 2,
}

public enum AdtPaintingRole : short
{
    Host,
    Editor,
}

public class AdtPainting
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int PaintingId { get; set; }

    public Guid PaintingUid { get; set; }

    public int? ParentPaintingId { get; set; }

    public AdtPainting? ParentPainting { get; set; }

    public Guid OwnerUserId { get; set; }

    [MaxLength(64)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? Description { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public AdtPaintingStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public Guid? ReviewerUserId { get; set; }

    [MaxLength(512)]
    public string? ReviewComment { get; set; }

    public bool AuthorNotified { get; set; }

    public int? RoundId { get; set; }

    public int? ServerId { get; set; }

    public int? HeadRevisionId { get; set; }

    public byte[]? ThumbHash { get; set; }

    public long BytesTotal { get; set; }

    public bool Pinned { get; set; }

    public List<AdtPaintingRevision> Revisions { get; set; } = new();

    public List<AdtPaintingContributor> Contributors { get; set; } = new();
}

public class AdtPaintingRevision
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int RevisionId { get; set; }

    public int PaintingId { get; set; }

    public AdtPainting Painting { get; set; } = default!;

    public AdtPaintingRevisionKind Kind { get; set; }

    public DateTime CreatedAt { get; set; }

    public long BytesTotal { get; set; }

    public List<AdtPaintingRevisionTile> Tiles { get; set; } = new();
}

public class AdtPaintingRevisionTile
{
    public int RevisionId { get; set; }

    public AdtPaintingRevision Revision { get; set; } = default!;

    public short Layer { get; set; }

    public short Lod { get; set; }

    public short TileX { get; set; }

    public short TileY { get; set; }

    public AdtPaintingTileKind Kind { get; set; }

    public int? SolidRgba { get; set; }

    public byte[]? BlobHash { get; set; }
}

public class AdtPaintingBlob
{
    [Key]
    public byte[] Hash { get; set; } = default!;

    public int Size { get; set; }

    public byte[]? Data { get; set; }

    [MaxLength(256)]
    public string? Location { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class AdtPaintingContributor
{
    public int PaintingId { get; set; }

    public AdtPainting Painting { get; set; } = default!;

    public Guid UserId { get; set; }

    public AdtPaintingRole Role { get; set; }

    public int TilesCommitted { get; set; }
}

public class AdtPaintingBan
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid UserId { get; set; }

    public Guid BannedBy { get; set; }

    [MaxLength(512)]
    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }
}

public class AdtPaintingWhitelist
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid UserId { get; set; }

    public Guid AddedBy { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class AdtPaintingReport
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ReportId { get; set; }

    public Guid PaintingUid { get; set; }

    public Guid ReporterUserId { get; set; }

    [MaxLength(256)]
    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public int? RoundId { get; set; }

    public int? ServerId { get; set; }

    public bool Resolved { get; set; }

    public Guid? ResolvedBy { get; set; }

    public DateTime? ResolvedAt { get; set; }
}
