using System.Threading.Tasks;
using Content.Server.ADT.Drawing.Database;
using Content.Shared.ADT.Drawing;

namespace Content.Server.Database;

public partial interface IServerDbManager
{
    Task<int> CreatePaintingAsync(Guid paintingUid, Guid ownerUserId, int width, int height, int? roundId, DateTime now);
    Task<bool> CreatePaintingFromParentAsync(Guid parentUid, Guid paintingUid, Guid ownerUserId, int width, int height,
        int? roundId, DateTime now);
    Task<List<PaintingDraftRecord>> GetPaintingDraftsAsync(Guid ownerUserId, DateTime notBefore);
    Task<int> ExpirePaintingDraftsAsync(Guid ownerUserId, int keep, DateTime notBefore, Guid? except);
    Task<PaintingSnapshot?> LoadPaintingAsync(Guid paintingUid);
    Task SavePaintingAutosaveAsync(Guid paintingUid, List<PaintingTileRecord> tiles,
        Dictionary<CanvasHash, byte[]> newBlobs, Dictionary<CanvasHash, string>? locations, long bytesTotal, DateTime now,
        Guid ownerUserId, Dictionary<Guid, int> contributions);
    Task<List<(CanvasHash Hash, byte[] Data)>> GetInlinePaintingBlobsAsync(int limit, int skip);
    Task SetPaintingBlobLocationsAsync(Dictionary<CanvasHash, string> locations);
    Task<HashSet<string>> GetUsedPaintingBlobLocationsAsync(List<string> locations);
    Task<Dictionary<CanvasHash, string>> GetExternalPaintingBlobsAsync(int limit, int skip);
    Task SetPaintingBlobDataAsync(Dictionary<CanvasHash, byte[]> blobs);
}

public sealed partial class ServerDbManager
{
    public Task<int> CreatePaintingAsync(Guid paintingUid, Guid ownerUserId, int width, int height, int? roundId, DateTime now)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.CreatePainting(paintingUid, ownerUserId, width, height, roundId, now));
    }

    public Task<bool> CreatePaintingFromParentAsync(Guid parentUid, Guid paintingUid, Guid ownerUserId, int width, int height,
        int? roundId, DateTime now)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.CreatePaintingFromParent(parentUid, paintingUid, ownerUserId, width, height, roundId, now));
    }

    public Task<List<PaintingDraftRecord>> GetPaintingDraftsAsync(Guid ownerUserId, DateTime notBefore)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetPaintingDrafts(ownerUserId, notBefore));
    }

    public Task<int> ExpirePaintingDraftsAsync(Guid ownerUserId, int keep, DateTime notBefore, Guid? except)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.ExpirePaintingDrafts(ownerUserId, keep, notBefore, except));
    }

    public Task<PaintingSnapshot?> LoadPaintingAsync(Guid paintingUid)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.LoadPainting(paintingUid));
    }

    public Task SavePaintingAutosaveAsync(Guid paintingUid, List<PaintingTileRecord> tiles,
        Dictionary<CanvasHash, byte[]> newBlobs, Dictionary<CanvasHash, string>? locations, long bytesTotal, DateTime now,
        Guid ownerUserId, Dictionary<Guid, int> contributions)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.SavePaintingAutosave(paintingUid, tiles, newBlobs, locations, bytesTotal, now,
            ownerUserId, contributions));
    }

    public Task<List<(CanvasHash Hash, byte[] Data)>> GetInlinePaintingBlobsAsync(int limit, int skip)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetInlinePaintingBlobs(limit, skip));
    }

    public Task SetPaintingBlobLocationsAsync(Dictionary<CanvasHash, string> locations)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.SetPaintingBlobLocations(locations));
    }

    public Task<HashSet<string>> GetUsedPaintingBlobLocationsAsync(List<string> locations)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetUsedPaintingBlobLocations(locations));
    }

    public Task<Dictionary<CanvasHash, string>> GetExternalPaintingBlobsAsync(int limit, int skip)
    {
        DbReadOpsMetric.Inc();
        return RunDbCommand(() => _db.GetExternalPaintingBlobs(limit, skip));
    }

    public Task SetPaintingBlobDataAsync(Dictionary<CanvasHash, byte[]> blobs)
    {
        DbWriteOpsMetric.Inc();
        return RunDbCommand(() => _db.SetPaintingBlobData(blobs));
    }
}
