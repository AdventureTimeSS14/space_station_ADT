using System.Threading.Tasks;
using Content.Server.ADT.Drawing.Database;
using Content.Shared.ADT.Drawing;

namespace Content.Server.Database;

public partial interface IServerDbManager
{
    Task<int?> SubmitPaintingAsync(Guid paintingUid, string title, string? description, DateTime now);
    Task SavePaintingPyramidAsync(Guid paintingUid, int revisionId, List<PaintingLevelTileRecord> tiles, Dictionary<CanvasHash, byte[]> blobs, Dictionary<CanvasHash, string>? locations, CanvasHash thumb, long bytesTotal, DateTime now);
    Task<bool> WithdrawPaintingAsync(Guid paintingUid, Guid ownerUserId, DateTime now);
    Task<bool> RejectPaintingBuildAsync(Guid paintingUid, DateTime now);
    Task<int> CountOwnerPaintingsAsync(Guid ownerUserId, AdtPaintingStatus status);
    Task<DateTime?> GetLastPaintingSubmitAsync(Guid ownerUserId);
    Task<List<PaintingDraftRecord>> GetOwnerPaintingsAsync(Guid ownerUserId, AdtPaintingStatus status);
    Task<List<PaintingSummary>> GetPaintingsByStatusAsync(AdtPaintingStatus status, int limit);
    Task<PaintingSummary?> GetPaintingSummaryAsync(Guid paintingUid);
    Task<Dictionary<Guid, PaintingOwnerStats>> GetPaintingOwnerStatsAsync(List<Guid> owners, DateTime now);
    Task<bool> ReviewPaintingAsync(Guid paintingUid, bool approve, Guid reviewer, string? comment, DateTime now);
    Task<bool> SetPaintingHiddenAsync(Guid paintingUid, bool hidden, DateTime now);
    Task<bool> SetPaintingPinnedAsync(Guid paintingUid, bool pinned);
    Task<bool> DeletePaintingAsync(Guid paintingUid, DateTime now);
    Task<long> GetApprovedPaintingBytesAsync();
    Task<PaintingViewRecord?> GetPaintingViewAsync(Guid paintingUid);
    Task<PaintingBlobSet> GetPaintingBlobsAsync(List<CanvasHash> hashes);
    Task<PaintingSummary?> GetRandomApprovedPaintingAsync(List<Guid> exclude, bool pinnedOnly, int seed);
    Task<List<PaintingDecisionRecord>> GetUnnotifiedPaintingDecisionsAsync(Guid ownerUserId);
    Task MarkPaintingsNotifiedAsync(List<Guid> paintings);
    Task<PaintingBanRecord?> GetPaintingBanAsync(Guid userId, DateTime now);
    Task AddPaintingBanAsync(Guid userId, Guid bannedBy, string reason, DateTime now, DateTime? expiresAt);
    Task<bool> RemovePaintingBanAsync(Guid userId);
    Task<bool> IsPaintingWhitelistedAsync(Guid userId);
    Task<bool> AddPaintingWhitelistAsync(Guid userId, Guid addedBy, DateTime now);
    Task<bool> RemovePaintingWhitelistAsync(Guid userId);
    Task<List<string>> GetPaintingWhitelistNamesAsync();
    Task<PaintingCleanupResult> CleanupPaintingsAsync(DateTime now, TimeSpan draftTtl, TimeSpan rejectTtl);
}

public sealed partial class ServerDbManager
{
    public Task<int?> SubmitPaintingAsync(Guid paintingUid, string title, string? description, DateTime now)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.SubmitPainting(paintingUid, title, description, now));
    }

    public Task SavePaintingPyramidAsync(Guid paintingUid, int revisionId, List<PaintingLevelTileRecord> tiles, Dictionary<CanvasHash, byte[]> blobs, Dictionary<CanvasHash, string>? locations, CanvasHash thumb, long bytesTotal, DateTime now)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.SavePaintingPyramid(paintingUid, revisionId, tiles, blobs, locations, thumb, bytesTotal, now));
    }

    public Task<bool> WithdrawPaintingAsync(Guid paintingUid, Guid ownerUserId, DateTime now)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.WithdrawPainting(paintingUid, ownerUserId, now));
    }

    public Task<bool> RejectPaintingBuildAsync(Guid paintingUid, DateTime now)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.RejectPaintingBuild(paintingUid, now));
    }

    public Task<int> CountOwnerPaintingsAsync(Guid ownerUserId, AdtPaintingStatus status)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.CountOwnerPaintings(ownerUserId, status));
    }

    public Task<DateTime?> GetLastPaintingSubmitAsync(Guid ownerUserId)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetLastPaintingSubmit(ownerUserId));
    }

    public Task<List<PaintingDraftRecord>> GetOwnerPaintingsAsync(Guid ownerUserId, AdtPaintingStatus status)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetOwnerPaintings(ownerUserId, status));
    }

    public Task<List<PaintingSummary>> GetPaintingsByStatusAsync(AdtPaintingStatus status, int limit)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetPaintingsByStatus(status, limit));
    }

    public Task<PaintingSummary?> GetPaintingSummaryAsync(Guid paintingUid)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetPaintingSummary(paintingUid));
    }

    public Task<Dictionary<Guid, PaintingOwnerStats>> GetPaintingOwnerStatsAsync(List<Guid> owners, DateTime now)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetPaintingOwnerStats(owners, now));
    }

    public Task<bool> ReviewPaintingAsync(Guid paintingUid, bool approve, Guid reviewer, string? comment, DateTime now)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.ReviewPainting(paintingUid, approve, reviewer, comment, now));
    }

    public Task<bool> SetPaintingHiddenAsync(Guid paintingUid, bool hidden, DateTime now)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.SetPaintingHidden(paintingUid, hidden, now));
    }

    public Task<bool> SetPaintingPinnedAsync(Guid paintingUid, bool pinned)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.SetPaintingPinned(paintingUid, pinned));
    }

    public Task<bool> DeletePaintingAsync(Guid paintingUid, DateTime now)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.DeletePainting(paintingUid, now));
    }

    public Task<long> GetApprovedPaintingBytesAsync()
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetApprovedPaintingBytes());
    }

    public Task<PaintingViewRecord?> GetPaintingViewAsync(Guid paintingUid)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetPaintingView(paintingUid));
    }

    public Task<PaintingBlobSet> GetPaintingBlobsAsync(List<CanvasHash> hashes)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetPaintingBlobs(hashes));
    }

    public Task<PaintingSummary?> GetRandomApprovedPaintingAsync(List<Guid> exclude, bool pinnedOnly, int seed)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetRandomApprovedPainting(exclude, pinnedOnly, seed));
    }

    public Task<List<PaintingDecisionRecord>> GetUnnotifiedPaintingDecisionsAsync(Guid ownerUserId)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetUnnotifiedPaintingDecisions(ownerUserId));
    }

    public Task MarkPaintingsNotifiedAsync(List<Guid> paintings)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.MarkPaintingsNotified(paintings));
    }

    public Task<PaintingBanRecord?> GetPaintingBanAsync(Guid userId, DateTime now)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetPaintingBan(userId, now));
    }

    public Task AddPaintingBanAsync(Guid userId, Guid bannedBy, string reason, DateTime now, DateTime? expiresAt)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.AddPaintingBan(userId, bannedBy, reason, now, expiresAt));
    }

    public Task<bool> RemovePaintingBanAsync(Guid userId)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.RemovePaintingBan(userId));
    }

    public Task<bool> IsPaintingWhitelistedAsync(Guid userId)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.IsPaintingWhitelisted(userId));
    }

    public Task<bool> AddPaintingWhitelistAsync(Guid userId, Guid addedBy, DateTime now)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.AddPaintingWhitelist(userId, addedBy, now));
    }

    public Task<bool> RemovePaintingWhitelistAsync(Guid userId)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.RemovePaintingWhitelist(userId));
    }

    public Task<List<string>> GetPaintingWhitelistNamesAsync()
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetPaintingWhitelistNames());
    }

    public Task<PaintingCleanupResult> CleanupPaintingsAsync(DateTime now, TimeSpan draftTtl, TimeSpan rejectTtl)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.CleanupPaintings(now, draftTtl, rejectTtl));
    }
}
