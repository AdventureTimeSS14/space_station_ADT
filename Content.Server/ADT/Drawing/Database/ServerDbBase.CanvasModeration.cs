using System.Linq;
using System.Threading.Tasks;
using Content.Server.ADT.Drawing.Database;
using Content.Shared.ADT.Drawing;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    private static readonly TimeSpan OrphanBlobAge = TimeSpan.FromHours(1);

    #region Отправка

    public async Task<int?> SubmitPainting(Guid paintingUid, string title, string? description, DateTime now)
    {
        await using var db = await GetDb();

        var painting = await db.DbContext.Set<AdtPainting>()
            .SingleOrDefaultAsync(p => p.PaintingUid == paintingUid);

        if (painting == null || painting.Status != AdtPaintingStatus.Draft || painting.HeadRevisionId is not { } revisionId)
            return null;

        var revision = await db.DbContext.Set<AdtPaintingRevision>()
            .SingleOrDefaultAsync(r => r.RevisionId == revisionId);

        if (revision == null)
            return null;

        revision.Kind = AdtPaintingRevisionKind.Submitted;
        painting.Status = AdtPaintingStatus.Submitted;
        painting.Title = title;
        painting.Description = description;
        painting.SubmittedAt = now;
        painting.UpdatedAt = now;
        painting.ReviewedAt = null;
        painting.ReviewerUserId = null;
        painting.ReviewComment = null;
        painting.AuthorNotified = false;

        await db.DbContext.SaveChangesAsync();
        return revisionId;
    }

    public async Task SavePaintingPyramid(Guid paintingUid, int revisionId, List<PaintingLevelTileRecord> tiles,
        Dictionary<CanvasHash, byte[]> blobs, Dictionary<CanvasHash, string>? locations, CanvasHash thumb,
        long bytesTotal, DateTime now)
    {
        await using var db = await GetDb();
        await using var transaction = await db.DbContext.Database.BeginTransactionAsync();

        var painting = await db.DbContext.Set<AdtPainting>()
            .SingleOrDefaultAsync(p => p.PaintingUid == paintingUid);

        if (painting == null || painting.HeadRevisionId != revisionId)
            return;

        await AddPaintingBlobs(db.DbContext, blobs, locations, now);

        await db.DbContext.Set<AdtPaintingRevisionTile>()
            .Where(t => t.RevisionId == revisionId && t.Lod > 0)
            .ExecuteDeleteAsync();

        foreach (var tile in tiles)
        {
            if (tile.Lod == 0 || tile.Content.Kind == CanvasTileKind.Empty)
                continue;

            db.DbContext.Set<AdtPaintingRevisionTile>().Add(ToTileRow(revisionId, tile.Lod, tile.Key, tile.Content));
        }

        painting.ThumbHash = HashBytes(thumb);
        painting.BytesTotal = bytesTotal;

        await db.DbContext.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task<bool> WithdrawPainting(Guid paintingUid, Guid ownerUserId, DateTime now)
    {
        await using var db = await GetDb();

        var painting = await db.DbContext.Set<AdtPainting>()
            .SingleOrDefaultAsync(p => p.PaintingUid == paintingUid);

        if (painting == null || painting.OwnerUserId != ownerUserId || painting.Status != AdtPaintingStatus.Submitted)
            return false;

        await RevertToDraft(db.DbContext, painting, now);
        await db.DbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectPaintingBuild(Guid paintingUid, DateTime now)
    {
        await using var db = await GetDb();

        var painting = await db.DbContext.Set<AdtPainting>()
            .SingleOrDefaultAsync(p => p.PaintingUid == paintingUid);

        if (painting == null || painting.Status != AdtPaintingStatus.Submitted)
            return false;

        await RevertToDraft(db.DbContext, painting, now);
        await db.DbContext.SaveChangesAsync();
        return true;
    }

    private static async Task RevertToDraft(ServerDbContext context, AdtPainting painting, DateTime now)
    {
        if (painting.HeadRevisionId is { } revisionId)
        {
            var revision = await context.Set<AdtPaintingRevision>()
                .SingleOrDefaultAsync(r => r.RevisionId == revisionId);

            if (revision != null)
                revision.Kind = AdtPaintingRevisionKind.Autosave;

            await context.Set<AdtPaintingRevisionTile>()
                .Where(t => t.RevisionId == revisionId && t.Lod > 0)
                .ExecuteDeleteAsync();
        }

        painting.Status = AdtPaintingStatus.Draft;
        painting.SubmittedAt = null;
        painting.ThumbHash = null;
        painting.UpdatedAt = now;
    }

    public async Task<int> CountOwnerPaintings(Guid ownerUserId, AdtPaintingStatus status)
    {
        await using var db = await GetDb();

        return await db.DbContext.Set<AdtPainting>()
            .CountAsync(p => p.OwnerUserId == ownerUserId && p.Status == status);
    }

    public async Task<DateTime?> GetLastPaintingSubmit(Guid ownerUserId)
    {
        await using var db = await GetDb();

        return await db.DbContext.Set<AdtPainting>()
            .Where(p => p.OwnerUserId == ownerUserId && p.SubmittedAt != null)
            .MaxAsync(p => p.SubmittedAt);
    }

    public async Task<List<PaintingDraftRecord>> GetOwnerPaintings(Guid ownerUserId, AdtPaintingStatus status)
    {
        await using var db = await GetDb();

        return await db.DbContext.Set<AdtPainting>()
            .AsNoTracking()
            .Where(p => p.OwnerUserId == ownerUserId && p.Status == status)
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => new PaintingDraftRecord
            {
                PaintingUid = p.PaintingUid,
                Title = p.Title,
                Width = p.Width,
                Height = p.Height,
                UpdatedAt = p.UpdatedAt,
            })
            .ToListAsync();
    }

    #endregion

    #region Модерация

    public async Task<List<PaintingSummary>> GetPaintingsByStatus(AdtPaintingStatus status, int limit)
    {
        await using var db = await GetDb();

        var query = db.DbContext.Set<AdtPainting>()
            .AsNoTracking()
            .Include(p => p.Contributors)
            .Where(p => p.Status == status);

        query = status == AdtPaintingStatus.Submitted
            ? query.OrderBy(p => p.SubmittedAt)
            : query.OrderByDescending(p => p.ReviewedAt);

        var rows = await query.Take(limit).ToListAsync();
        return rows.Select(ToSummary).ToList();
    }

    public async Task<PaintingSummary?> GetPaintingSummary(Guid paintingUid)
    {
        await using var db = await GetDb();

        var row = await db.DbContext.Set<AdtPainting>()
            .AsNoTracking()
            .Include(p => p.Contributors)
            .SingleOrDefaultAsync(p => p.PaintingUid == paintingUid);

        return row == null ? null : ToSummary(row);
    }

    public async Task<Dictionary<Guid, PaintingOwnerStats>> GetPaintingOwnerStats(List<Guid> owners, DateTime now)
    {
        await using var db = await GetDb();

        var result = new Dictionary<Guid, PaintingOwnerStats>();
        foreach (var owner in owners)
        {
            result[owner] = new PaintingOwnerStats();
        }

        var counts = await db.DbContext.Set<AdtPainting>()
            .Where(p => owners.Contains(p.OwnerUserId) &&
                        (p.Status == AdtPaintingStatus.Approved ||
                         p.Status == AdtPaintingStatus.Hidden ||
                         p.Status == AdtPaintingStatus.Rejected))
            .GroupBy(p => new { p.OwnerUserId, p.Status })
            .Select(g => new { g.Key.OwnerUserId, g.Key.Status, Count = g.Count() })
            .ToListAsync();

        foreach (var count in counts)
        {
            var stats = result[count.OwnerUserId];
            if (count.Status == AdtPaintingStatus.Rejected)
                stats.Rejected += count.Count;
            else
                stats.Approved += count.Count;
        }

        var banned = await db.DbContext.Set<AdtPaintingBan>()
            .Where(b => owners.Contains(b.UserId) && (b.ExpiresAt == null || b.ExpiresAt > now))
            .Select(b => b.UserId)
            .ToListAsync();

        foreach (var user in banned)
        {
            result[user].Banned = true;
        }

        return result;
    }

    public async Task<bool> ReviewPainting(Guid paintingUid, bool approve, Guid reviewer, string? comment, DateTime now)
    {
        await using var db = await GetDb();

        var painting = await db.DbContext.Set<AdtPainting>()
            .SingleOrDefaultAsync(p => p.PaintingUid == paintingUid);

        if (painting == null || painting.Status != AdtPaintingStatus.Submitted)
            return false;

        painting.Status = approve ? AdtPaintingStatus.Approved : AdtPaintingStatus.Rejected;
        painting.ReviewedAt = now;
        painting.ReviewerUserId = reviewer;
        painting.ReviewComment = comment;
        painting.AuthorNotified = false;
        painting.UpdatedAt = now;

        await db.DbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetPaintingHidden(Guid paintingUid, bool hidden, DateTime now)
    {
        await using var db = await GetDb();

        var painting = await db.DbContext.Set<AdtPainting>()
            .SingleOrDefaultAsync(p => p.PaintingUid == paintingUid);

        var from = hidden ? AdtPaintingStatus.Approved : AdtPaintingStatus.Hidden;
        if (painting == null || painting.Status != from)
            return false;

        painting.Status = hidden ? AdtPaintingStatus.Hidden : AdtPaintingStatus.Approved;
        painting.UpdatedAt = now;

        await db.DbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetPaintingPinned(Guid paintingUid, bool pinned)
    {
        await using var db = await GetDb();

        var painting = await db.DbContext.Set<AdtPainting>()
            .SingleOrDefaultAsync(p => p.PaintingUid == paintingUid);

        if (painting == null)
            return false;

        painting.Pinned = pinned;
        await db.DbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeletePainting(Guid paintingUid, DateTime now)
    {
        await using var db = await GetDb();

        var painting = await db.DbContext.Set<AdtPainting>()
            .SingleOrDefaultAsync(p => p.PaintingUid == paintingUid);

        if (painting == null || painting.Status == AdtPaintingStatus.Deleted)
            return false;

        await db.DbContext.Set<AdtPaintingRevision>()
            .Where(r => r.PaintingId == painting.PaintingId)
            .ExecuteDeleteAsync();

        painting.Status = AdtPaintingStatus.Deleted;
        painting.HeadRevisionId = null;
        painting.ThumbHash = null;
        painting.UpdatedAt = now;

        await db.DbContext.SaveChangesAsync();
        return true;
    }

    public async Task<long> GetApprovedPaintingBytes()
    {
        await using var db = await GetDb();

        return await db.DbContext.Set<AdtPainting>()
            .Where(p => p.Status == AdtPaintingStatus.Approved || p.Status == AdtPaintingStatus.Hidden)
            .SumAsync(p => p.BytesTotal);
    }

    #endregion

    #region Просмотр и галерея

    public async Task<PaintingViewRecord?> GetPaintingView(Guid paintingUid)
    {
        await using var db = await GetDb();

        var row = await db.DbContext.Set<AdtPainting>()
            .AsNoTracking()
            .Include(p => p.Contributors)
            .SingleOrDefaultAsync(p => p.PaintingUid == paintingUid);

        if (row == null)
            return null;

        var view = new PaintingViewRecord
        {
            Summary = ToSummary(row),
        };

        if (row.HeadRevisionId is not { } revisionId)
            return view;

        var tiles = await db.DbContext.Set<AdtPaintingRevisionTile>()
            .AsNoTracking()
            .Where(t => t.RevisionId == revisionId)
            .ToListAsync();

        foreach (var tile in tiles)
        {
            view.Tiles.Add(new PaintingLevelTileRecord(
                (byte) tile.Lod,
                new CanvasTileKey((byte) tile.Layer, (ushort) tile.TileX, (ushort) tile.TileY),
                ToContent(tile)));
        }

        return view;
    }

    public async Task<PaintingBlobSet> GetPaintingBlobs(List<CanvasHash> hashes)
    {
        await using var db = await GetDb();

        var result = new PaintingBlobSet();
        foreach (var hash in hashes)
        {
            var key = HashBytes(hash);
            var row = await db.DbContext.Set<AdtPaintingBlob>()
                .AsNoTracking()
                .Where(b => b.Hash == key)
                .Select(b => new { b.Data, b.Location })
                .SingleOrDefaultAsync();

            if (row?.Data != null)
                result.Data[hash] = row.Data;
            else if (row?.Location != null)
                result.External[hash] = row.Location;
        }

        return result;
    }

    public async Task<PaintingSummary?> GetRandomApprovedPainting(List<Guid> exclude, bool pinnedOnly, int seed)
    {
        await using var db = await GetDb();

        var query = db.DbContext.Set<AdtPainting>()
            .AsNoTracking()
            .Where(p => p.Status == AdtPaintingStatus.Approved && p.ThumbHash != null && !exclude.Contains(p.PaintingUid));

        if (pinnedOnly)
            query = query.Where(p => p.Pinned);

        var count = await query.CountAsync();
        if (count == 0)
            return null;

        var index = (int) ((uint) seed % (uint) count);
        var row = await query
            .OrderBy(p => p.PaintingId)
            .Skip(index)
            .Include(p => p.Contributors)
            .FirstOrDefaultAsync();

        return row == null ? null : ToSummary(row);
    }

    #endregion

    #region Уведомления автора

    public async Task<List<PaintingDecisionRecord>> GetUnnotifiedPaintingDecisions(Guid ownerUserId)
    {
        await using var db = await GetDb();

        return await db.DbContext.Set<AdtPainting>()
            .AsNoTracking()
            .Where(p => p.OwnerUserId == ownerUserId &&
                        !p.AuthorNotified &&
                        p.ReviewedAt != null &&
                        (p.Status == AdtPaintingStatus.Approved || p.Status == AdtPaintingStatus.Rejected))
            .Select(p => new PaintingDecisionRecord
            {
                PaintingUid = p.PaintingUid,
                Title = p.Title,
                Status = p.Status,
                Comment = p.ReviewComment,
            })
            .ToListAsync();
    }

    public async Task MarkPaintingsNotified(List<Guid> paintings)
    {
        await using var db = await GetDb();

        await db.DbContext.Set<AdtPainting>()
            .Where(p => paintings.Contains(p.PaintingUid))
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.AuthorNotified, true));
    }

    #endregion

    #region Баны

    public async Task<PaintingBanRecord?> GetPaintingBan(Guid userId, DateTime now)
    {
        await using var db = await GetDb();

        return await db.DbContext.Set<AdtPaintingBan>()
            .AsNoTracking()
            .Where(b => b.UserId == userId && (b.ExpiresAt == null || b.ExpiresAt > now))
            .Select(b => new PaintingBanRecord
            {
                UserId = b.UserId,
                BannedBy = b.BannedBy,
                Reason = b.Reason,
                CreatedAt = b.CreatedAt,
                ExpiresAt = b.ExpiresAt,
            })
            .SingleOrDefaultAsync();
    }

    public async Task AddPaintingBan(Guid userId, Guid bannedBy, string reason, DateTime now, DateTime? expiresAt)
    {
        await using var db = await GetDb();

        var ban = await db.DbContext.Set<AdtPaintingBan>()
            .SingleOrDefaultAsync(b => b.UserId == userId);

        if (ban == null)
        {
            ban = new AdtPaintingBan
            {
                UserId = userId,
            };
            db.DbContext.Set<AdtPaintingBan>().Add(ban);
        }

        ban.BannedBy = bannedBy;
        ban.Reason = reason;
        ban.CreatedAt = now;
        ban.ExpiresAt = expiresAt;

        await db.DbContext.SaveChangesAsync();
    }

    public async Task<bool> RemovePaintingBan(Guid userId)
    {
        await using var db = await GetDb();

        var removed = await db.DbContext.Set<AdtPaintingBan>()
            .Where(b => b.UserId == userId)
            .ExecuteDeleteAsync();

        return removed > 0;
    }

    #endregion

    #region Вайтлист

    public async Task<bool> IsPaintingWhitelisted(Guid userId)
    {
        await using var db = await GetDb();

        return await db.DbContext.Set<AdtPaintingWhitelist>()
            .AnyAsync(w => w.UserId == userId);
    }

    public async Task<bool> AddPaintingWhitelist(Guid userId, Guid addedBy, DateTime now)
    {
        await using var db = await GetDb();

        if (await db.DbContext.Set<AdtPaintingWhitelist>().AnyAsync(w => w.UserId == userId))
            return false;

        db.DbContext.Set<AdtPaintingWhitelist>().Add(new AdtPaintingWhitelist
        {
            UserId = userId,
            AddedBy = addedBy,
            CreatedAt = now,
        });

        await db.DbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemovePaintingWhitelist(Guid userId)
    {
        await using var db = await GetDb();

        var removed = await db.DbContext.Set<AdtPaintingWhitelist>()
            .Where(w => w.UserId == userId)
            .ExecuteDeleteAsync();

        return removed > 0;
    }

    public async Task<List<string>> GetPaintingWhitelistNames()
    {
        await using var db = await GetDb();

        var users = db.DbContext.Set<AdtPaintingWhitelist>().Select(w => w.UserId);
        return await db.DbContext.Player
            .Where(p => users.Contains(p.UserId))
            .Select(p => p.LastSeenUserName)
            .ToListAsync();
    }

    #endregion

    #region Сборщик мусора

    public async Task<PaintingCleanupResult> CleanupPaintings(DateTime now, TimeSpan draftTtl, TimeSpan rejectTtl)
    {
        await using var db = await GetDb();

        var draftCutoff = now - draftTtl;
        var expired = await db.DbContext.Set<AdtPainting>()
            .Where(p => p.Status == AdtPaintingStatus.Draft && p.UpdatedAt < draftCutoff)
            .ToListAsync();

        var rejectCutoff = now - rejectTtl;
        var rejected = await db.DbContext.Set<AdtPainting>()
            .Where(p => p.Status == AdtPaintingStatus.Rejected && p.ReviewedAt != null && p.ReviewedAt < rejectCutoff)
            .ToListAsync();

        var ids = expired.Select(p => p.PaintingId).Concat(rejected.Select(p => p.PaintingId)).ToList();
        if (ids.Count > 0)
        {
            await db.DbContext.Set<AdtPaintingRevision>()
                .Where(r => ids.Contains(r.PaintingId))
                .ExecuteDeleteAsync();
        }

        foreach (var painting in expired)
        {
            painting.Status = AdtPaintingStatus.Expired;
            painting.HeadRevisionId = null;
            painting.ThumbHash = null;
        }

        foreach (var painting in rejected)
        {
            painting.Status = AdtPaintingStatus.Deleted;
            painting.HeadRevisionId = null;
            painting.ThumbHash = null;
        }

        await db.DbContext.SaveChangesAsync();

        var blobCutoff = now - OrphanBlobAge;
        var orphans = db.DbContext.Set<AdtPaintingBlob>()
            .Where(b => b.CreatedAt < blobCutoff &&
                        !db.DbContext.Set<AdtPaintingRevisionTile>().Any(t => t.BlobHash == b.Hash) &&
                        !db.DbContext.Set<AdtPainting>().Any(p => p.ThumbHash == b.Hash));

        var locations = await orphans
            .Where(b => b.Location != null)
            .Select(b => b.Location!)
            .ToListAsync();

        var deletedBlobs = await orphans.ExecuteDeleteAsync();

        return new PaintingCleanupResult(expired.Count, rejected.Count, deletedBlobs, locations);
    }

    #endregion

    private static AdtPaintingRevisionTile ToTileRow(int revisionId, byte lod, CanvasTileKey key, CanvasTileContent content)
    {
        return new AdtPaintingRevisionTile
        {
            RevisionId = revisionId,
            Layer = key.Layer,
            Lod = lod,
            TileX = (short) key.X,
            TileY = (short) key.Y,
            Kind = content.Kind == CanvasTileKind.Solid ? AdtPaintingTileKind.Solid : AdtPaintingTileKind.Blob,
            SolidRgba = content.Kind == CanvasTileKind.Solid ? unchecked((int) content.Rgba) : null,
            BlobHash = content.Kind == CanvasTileKind.Blob ? HashBytes(content.Hash) : null,
        };
    }

    private static PaintingSummary ToSummary(AdtPainting row)
    {
        var summary = new PaintingSummary
        {
            PaintingId = row.PaintingId,
            PaintingUid = row.PaintingUid,
            OwnerUserId = row.OwnerUserId,
            Title = row.Title,
            Description = row.Description,
            Width = row.Width,
            Height = row.Height,
            Status = row.Status,
            SubmittedAt = row.SubmittedAt,
            ReviewedAt = row.ReviewedAt,
            ReviewerUserId = row.ReviewerUserId,
            ReviewComment = row.ReviewComment,
            RoundId = row.RoundId,
            BytesTotal = row.BytesTotal,
            Pinned = row.Pinned,
            ThumbHash = row.ThumbHash is { Length: CanvasConstants.HashLength } thumb ? CanvasHash.FromBytes(thumb) : null,
        };

        foreach (var contributor in row.Contributors)
        {
            summary.Contributors.Add((contributor.UserId, contributor.TilesCommitted));
        }

        return summary;
    }
}
