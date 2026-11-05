using System.Linq;
using System.Threading.Tasks;
using Content.Server.ADT.Drawing.Database;
using Content.Shared.ADT.Drawing;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    public async Task<int> CreatePainting(Guid paintingUid, Guid ownerUserId, int width, int height, int? roundId, DateTime now)
    {
        await using var db = await GetDb();

        var painting = new AdtPainting
        {
            PaintingUid = paintingUid,
            OwnerUserId = ownerUserId,
            Width = width,
            Height = height,
            Status = AdtPaintingStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
            RoundId = roundId,
        };

        painting.Contributors.Add(new AdtPaintingContributor
        {
            UserId = ownerUserId,
            Role = AdtPaintingRole.Host,
        });

        db.DbContext.Set<AdtPainting>().Add(painting);
        await db.DbContext.SaveChangesAsync();
        return painting.PaintingId;
    }

    public async Task<bool> CreatePaintingFromParent(Guid parentUid, Guid paintingUid, Guid ownerUserId, int width, int height,
        int? roundId, DateTime now)
    {
        await using var db = await GetDb();
        await using var transaction = await db.DbContext.Database.BeginTransactionAsync();

        var parent = await db.DbContext.Set<AdtPainting>()
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.PaintingUid == parentUid);

        if (parent == null ||
            parent.OwnerUserId != ownerUserId ||
            parent.Status != AdtPaintingStatus.Approved ||
            parent.Width != width ||
            parent.Height != height ||
            parent.HeadRevisionId is not { } parentRevision)
        {
            return false;
        }

        var painting = new AdtPainting
        {
            PaintingUid = paintingUid,
            ParentPaintingId = parent.PaintingId,
            OwnerUserId = ownerUserId,
            Title = parent.Title,
            Width = width,
            Height = height,
            Status = AdtPaintingStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
            RoundId = roundId,
            BytesTotal = parent.BytesTotal,
        };

        painting.Contributors.Add(new AdtPaintingContributor
        {
            UserId = ownerUserId,
            Role = AdtPaintingRole.Host,
        });

        db.DbContext.Set<AdtPainting>().Add(painting);
        await db.DbContext.SaveChangesAsync();

        var tiles = await db.DbContext.Set<AdtPaintingRevisionTile>()
            .AsNoTracking()
            .Where(t => t.RevisionId == parentRevision && t.Lod == 0)
            .ToListAsync();

        var revision = new AdtPaintingRevision
        {
            PaintingId = painting.PaintingId,
            Kind = AdtPaintingRevisionKind.Autosave,
            CreatedAt = now,
            BytesTotal = parent.BytesTotal,
        };

        foreach (var tile in tiles)
        {
            revision.Tiles.Add(new AdtPaintingRevisionTile
            {
                Layer = tile.Layer,
                Lod = 0,
                TileX = tile.TileX,
                TileY = tile.TileY,
                Kind = tile.Kind,
                SolidRgba = tile.SolidRgba,
                BlobHash = tile.BlobHash,
            });
        }

        db.DbContext.Set<AdtPaintingRevision>().Add(revision);
        await db.DbContext.SaveChangesAsync();

        painting.HeadRevisionId = revision.RevisionId;
        await db.DbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }

    public async Task<List<PaintingDraftRecord>> GetPaintingDrafts(Guid ownerUserId, DateTime notBefore)
    {
        await using var db = await GetDb();

        return await db.DbContext.Set<AdtPainting>()
            .AsNoTracking()
            .Where(p => p.OwnerUserId == ownerUserId &&
                        p.Status == AdtPaintingStatus.Draft &&
                        p.UpdatedAt >= notBefore)
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

    public async Task<int> ExpirePaintingDrafts(Guid ownerUserId, int keep, DateTime notBefore, Guid? except)
    {
        await using var db = await GetDb();

        var drafts = await db.DbContext.Set<AdtPainting>()
            .Where(p => p.OwnerUserId == ownerUserId && p.Status == AdtPaintingStatus.Draft)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync();

        var kept = 0;
        var expired = new List<AdtPainting>();
        foreach (var draft in drafts)
        {
            if (draft.PaintingUid == except)
                continue;

            if (draft.UpdatedAt >= notBefore && kept < keep)
            {
                kept++;
                continue;
            }

            expired.Add(draft);
        }

        if (expired.Count == 0)
            return 0;

        var ids = expired.Select(p => p.PaintingId).ToList();
        var revisions = await db.DbContext.Set<AdtPaintingRevision>()
            .Where(r => ids.Contains(r.PaintingId))
            .ToListAsync();

        db.DbContext.Set<AdtPaintingRevision>().RemoveRange(revisions);
        foreach (var draft in expired)
        {
            draft.Status = AdtPaintingStatus.Expired;
            draft.HeadRevisionId = null;
        }

        await db.DbContext.SaveChangesAsync();
        return expired.Count;
    }

    public async Task<PaintingSnapshot?> LoadPainting(Guid paintingUid)
    {
        await using var db = await GetDb();

        var painting = await db.DbContext.Set<AdtPainting>()
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.PaintingUid == paintingUid);

        if (painting == null)
            return null;

        var snapshot = new PaintingSnapshot
        {
            PaintingId = painting.PaintingId,
            PaintingUid = painting.PaintingUid,
            OwnerUserId = painting.OwnerUserId,
            Width = painting.Width,
            Height = painting.Height,
            Status = painting.Status,
            Title = painting.Title,
            UpdatedAt = painting.UpdatedAt,
        };

        if (painting.HeadRevisionId is not { } revisionId)
            return snapshot;

        var tiles = await db.DbContext.Set<AdtPaintingRevisionTile>()
            .AsNoTracking()
            .Where(t => t.RevisionId == revisionId && t.Lod == 0)
            .ToListAsync();

        var hasBlobs = false;
        foreach (var tile in tiles)
        {
            if (tile.Kind == AdtPaintingTileKind.Blob && tile.BlobHash != null)
                hasBlobs = true;

            snapshot.Tiles.Add(new PaintingTileRecord(
                new CanvasTileKey((byte) tile.Layer, (ushort) tile.TileX, (ushort) tile.TileY),
                ToContent(tile)));
        }

        if (!hasBlobs)
            return snapshot;

        var blobs = await db.DbContext.Set<AdtPaintingRevisionTile>()
            .AsNoTracking()
            .Where(t => t.RevisionId == revisionId && t.Lod == 0 && t.BlobHash != null)
            .Join(db.DbContext.Set<AdtPaintingBlob>(),
                t => t.BlobHash!,
                b => b.Hash,
                (t, b) => new { b.Hash, b.Data, b.Location })
            .ToListAsync();

        foreach (var blob in blobs)
        {
            var hash = CanvasHash.FromBytes(blob.Hash);
            if (blob.Data != null)
                snapshot.Blobs[hash] = blob.Data;
            else if (blob.Location != null)
                snapshot.ExternalBlobs[hash] = blob.Location;
        }

        return snapshot;
    }

    private static async Task AddPaintingBlobs(ServerDbContext context, Dictionary<CanvasHash, byte[]> blobs,
        Dictionary<CanvasHash, string>? locations, DateTime now)
    {
        foreach (var (hash, data) in blobs)
        {
            var key = HashBytes(hash);
            if (await context.Set<AdtPaintingBlob>().AnyAsync(b => b.Hash == key))
                continue;

            string? location = null;
            locations?.TryGetValue(hash, out location);

            context.Set<AdtPaintingBlob>().Add(new AdtPaintingBlob
            {
                Hash = key,
                Size = data.Length,
                Data = location == null ? data : null,
                Location = location,
                CreatedAt = now,
            });
        }
    }

    public async Task<List<(CanvasHash Hash, byte[] Data)>> GetInlinePaintingBlobs(int limit, int skip)
    {
        await using var db = await GetDb();

        var rows = await db.DbContext.Set<AdtPaintingBlob>()
            .AsNoTracking()
            .Where(b => b.Data != null)
            .OrderBy(b => b.CreatedAt)
            .Skip(skip)
            .Take(limit)
            .Select(b => new { b.Hash, b.Data })
            .ToListAsync();

        return rows.Select(r => (CanvasHash.FromBytes(r.Hash), r.Data!)).ToList();
    }

    public async Task<Dictionary<CanvasHash, string>> GetExternalPaintingBlobs(int limit, int skip)
    {
        await using var db = await GetDb();

        var rows = await db.DbContext.Set<AdtPaintingBlob>()
            .AsNoTracking()
            .Where(b => b.Data == null && b.Location != null)
            .OrderBy(b => b.CreatedAt)
            .Skip(skip)
            .Take(limit)
            .Select(b => new { b.Hash, b.Location })
            .ToListAsync();

        var result = new Dictionary<CanvasHash, string>();
        foreach (var row in rows)
        {
            result[CanvasHash.FromBytes(row.Hash)] = row.Location!;
        }

        return result;
    }

    public async Task SetPaintingBlobData(Dictionary<CanvasHash, byte[]> blobs)
    {
        await using var db = await GetDb();

        foreach (var (hash, data) in blobs)
        {
            var key = HashBytes(hash);
            await db.DbContext.Set<AdtPaintingBlob>()
                .Where(b => b.Hash == key)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.Data, data)
                    .SetProperty(b => b.Location, (string?) null));
        }
    }

    public async Task<HashSet<string>> GetUsedPaintingBlobLocations(List<string> locations)
    {
        await using var db = await GetDb();

        var used = await db.DbContext.Set<AdtPaintingBlob>()
            .AsNoTracking()
            .Where(b => b.Location != null && locations.Contains(b.Location))
            .Select(b => b.Location!)
            .ToListAsync();

        return used.ToHashSet();
    }

    public async Task SetPaintingBlobLocations(Dictionary<CanvasHash, string> locations)
    {
        await using var db = await GetDb();

        foreach (var (hash, location) in locations)
        {
            var key = HashBytes(hash);
            await db.DbContext.Set<AdtPaintingBlob>()
                .Where(b => b.Hash == key)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.Location, location)
                    .SetProperty(b => b.Data, (byte[]?) null));
        }
    }

    public async Task SavePaintingAutosave(Guid paintingUid, List<PaintingTileRecord> tiles,
        Dictionary<CanvasHash, byte[]> newBlobs, Dictionary<CanvasHash, string>? locations, long bytesTotal, DateTime now,
        Guid ownerUserId, Dictionary<Guid, int> contributions)
    {
        await using var db = await GetDb();
        await using var transaction = await db.DbContext.Database.BeginTransactionAsync();

        var painting = await db.DbContext.Set<AdtPainting>()
            .Include(p => p.Contributors)
            .SingleOrDefaultAsync(p => p.PaintingUid == paintingUid);

        if (painting == null)
            return;

        painting.OwnerUserId = ownerUserId;
        foreach (var (user, tilesCommitted) in contributions)
        {
            var contributor = painting.Contributors.Find(c => c.UserId == user);
            if (contributor == null)
            {
                contributor = new AdtPaintingContributor
                {
                    UserId = user,
                    Role = AdtPaintingRole.Editor,
                };
                painting.Contributors.Add(contributor);
            }

            contributor.TilesCommitted += tilesCommitted;
        }

        foreach (var contributor in painting.Contributors)
        {
            contributor.Role = contributor.UserId == ownerUserId ? AdtPaintingRole.Host : AdtPaintingRole.Editor;
        }

        if (painting.Contributors.All(c => c.UserId != ownerUserId))
        {
            painting.Contributors.Add(new AdtPaintingContributor
            {
                UserId = ownerUserId,
                Role = AdtPaintingRole.Host,
            });
        }

        await AddPaintingBlobs(db.DbContext, newBlobs, locations, now);

        var revision = new AdtPaintingRevision
        {
            PaintingId = painting.PaintingId,
            Kind = AdtPaintingRevisionKind.Autosave,
            CreatedAt = now,
            BytesTotal = bytesTotal,
        };

        foreach (var tile in tiles)
        {
            if (tile.Content.Kind == CanvasTileKind.Empty)
                continue;

            revision.Tiles.Add(new AdtPaintingRevisionTile
            {
                Layer = tile.Key.Layer,
                Lod = 0,
                TileX = (short) tile.Key.X,
                TileY = (short) tile.Key.Y,
                Kind = tile.Content.Kind == CanvasTileKind.Solid ? AdtPaintingTileKind.Solid : AdtPaintingTileKind.Blob,
                SolidRgba = tile.Content.Kind == CanvasTileKind.Solid ? unchecked((int) tile.Content.Rgba) : null,
                BlobHash = tile.Content.Kind == CanvasTileKind.Blob ? HashBytes(tile.Content.Hash) : null,
            });
        }

        db.DbContext.Set<AdtPaintingRevision>().Add(revision);
        await db.DbContext.SaveChangesAsync();

        var old = await db.DbContext.Set<AdtPaintingRevision>()
            .Where(r => r.PaintingId == painting.PaintingId &&
                        r.Kind == AdtPaintingRevisionKind.Autosave &&
                        r.RevisionId != revision.RevisionId)
            .ToListAsync();

        db.DbContext.Set<AdtPaintingRevision>().RemoveRange(old);
        painting.HeadRevisionId = revision.RevisionId;
        painting.UpdatedAt = now;
        painting.BytesTotal = bytesTotal;

        await db.DbContext.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static CanvasTileContent ToContent(AdtPaintingRevisionTile tile)
    {
        switch (tile.Kind)
        {
            case AdtPaintingTileKind.Solid:
                return CanvasTileContent.Solid(unchecked((uint) (tile.SolidRgba ?? 0)));

            case AdtPaintingTileKind.Blob when tile.BlobHash is { Length: CanvasConstants.HashLength }:
                return CanvasTileContent.Blob(CanvasHash.FromBytes(tile.BlobHash));

            default:
                return CanvasTileContent.Empty;
        }
    }

    private static byte[] HashBytes(CanvasHash hash)
    {
        var bytes = new byte[CanvasConstants.HashLength];
        hash.WriteTo(bytes);
        return bytes;
    }
}
