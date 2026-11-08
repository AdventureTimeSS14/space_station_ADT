using Content.Server.Database;
using Content.Shared.ADT.Drawing;

namespace Content.Server.ADT.Drawing.Database;

public readonly record struct PaintingTileRecord(CanvasTileKey Key, CanvasTileContent Content);

public sealed class PaintingSnapshot
{
    public int PaintingId;
    public Guid PaintingUid;
    public Guid OwnerUserId;
    public int Width;
    public int Height;
    public AdtPaintingStatus Status;
    public string Title = string.Empty;
    public DateTime UpdatedAt;
    public List<PaintingTileRecord> Tiles = new();
    public Dictionary<CanvasHash, byte[]> Blobs = new();
    public Dictionary<CanvasHash, string> ExternalBlobs = new();
}

public sealed class PaintingBlobSet
{
    public Dictionary<CanvasHash, byte[]> Data = new();
    public Dictionary<CanvasHash, string> External = new();
}

public sealed class PaintingDraftRecord
{
    public Guid PaintingUid;
    public string Title = string.Empty;
    public int Width;
    public int Height;
    public DateTime UpdatedAt;
}

public readonly record struct PaintingLevelTileRecord(byte Lod, CanvasTileKey Key, CanvasTileContent Content);

public sealed class PaintingSummary
{
    public int PaintingId;
    public Guid PaintingUid;
    public Guid OwnerUserId;
    public string Title = string.Empty;
    public string? Description;
    public int Width;
    public int Height;
    public AdtPaintingStatus Status;
    public DateTime? SubmittedAt;
    public DateTime? ReviewedAt;
    public Guid? ReviewerUserId;
    public string? ReviewComment;
    public int? RoundId;
    public long BytesTotal;
    public bool Pinned;
    public CanvasHash? ThumbHash;
    public List<(Guid UserId, int Tiles)> Contributors = new();
}

public sealed class PaintingViewRecord
{
    public PaintingSummary Summary = new();
    public List<PaintingLevelTileRecord> Tiles = new();
}

public sealed class PaintingBanRecord
{
    public Guid UserId;
    public Guid BannedBy;
    public string Reason = string.Empty;
    public DateTime CreatedAt;
    public DateTime? ExpiresAt;
}

public sealed class PaintingReportRecord
{
    public int ReportId;
    public Guid PaintingUid;
    public Guid ReporterUserId;
    public string Reason = string.Empty;
    public DateTime CreatedAt;
    public int? RoundId;
    public bool Resolved;
    public Guid? ResolvedBy;
    public DateTime? ResolvedAt;
    public string? Title;
    public AdtPaintingStatus? Status;
    public Guid? OwnerUserId;
    public int OpenReports;
}

public sealed class PaintingDecisionRecord
{
    public Guid PaintingUid;
    public string Title = string.Empty;
    public AdtPaintingStatus Status;
    public string? Comment;
}

public sealed class PaintingOwnerStats
{
    public int Approved;
    public int Rejected;
    public bool Banned;
}

public readonly record struct PaintingCleanupResult(int ExpiredDrafts, int DeletedRejected, int DeletedBlobs,
    List<string> DeletedLocations);
