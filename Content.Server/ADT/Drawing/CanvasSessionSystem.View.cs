using System.Linq;
using System.Threading.Tasks;
using Content.Server.ADT.Drawing.Database;
using Content.Server.Database;
using Content.Shared.ADT.Drawing;
using Content.Shared.Administration;
using Robust.Shared.Player;

namespace Content.Server.ADT.Drawing;

public sealed partial class CanvasSessionSystem
{
    private const int MaxCachedViews = 64;
    private const long MaxBlobCacheBytes = 64L * 1024 * 1024;
    private const int MaxViewLoadsPerPeer = 4;

    private readonly Dictionary<Guid, CachedPaintingView> _views = new();
    private readonly Dictionary<Guid, Task<PaintingViewRecord?>> _viewLoads = new();
    private readonly Dictionary<CanvasHash, HashSet<CanvasPeer>> _blobFetches = new();
    private readonly Dictionary<CanvasHash, byte[]> _blobCache = new();
    private readonly Queue<CanvasHash> _blobCacheOrder = new();
    private long _blobCacheBytes;

    private sealed class CachedPaintingView
    {
        public readonly PaintingViewRecord Record;
        public readonly HashSet<CanvasHash> Hashes = new();
        public TimeSpan LastUsed;

        public CachedPaintingView(PaintingViewRecord record)
        {
            Record = record;
            foreach (var tile in record.Tiles)
            {
                if (tile.Content.Kind == CanvasTileKind.Blob)
                    Hashes.Add(tile.Content.Hash);
            }

            if (record.Summary.ThumbHash is { } thumb)
                Hashes.Add(thumb);
        }
    }

    private void InitializeView()
    {
        _net.RegisterNetMessage<MsgCanvasViewRequest>(OnViewRequest);
        _net.RegisterNetMessage<MsgCanvasViewInfo>();
        _net.RegisterNetMessage<MsgCanvasInvalidate>();
    }

    public void InvalidatePainting(Guid paintingUid, bool dropClientCache)
    {
        var hashes = new List<CanvasHash>();
        if (_views.Remove(paintingUid, out var view) && dropClientCache)
            hashes.AddRange(view.Hashes);

        foreach (var peer in _peers.Values)
        {
            peer.Views.Remove(paintingUid);
        }

        var msg = new MsgCanvasInvalidate
        {
            Painting = paintingUid,
            Hashes = hashes,
        };

        _net.ServerSendToAll(msg);
    }

    private void OnViewRequest(MsgCanvasViewRequest msg)
    {
        if (!_players.TryGetSessionByChannel(msg.MsgChannel, out var player))
            return;

        ViewAsync(player, msg.Painting);
    }

    private async void ViewAsync(ICommonSession player, Guid paintingUid)
    {
        try
        {
            if (!_views.TryGetValue(paintingUid, out var view))
            {
                var peer = GetPeer(player);
                if (peer.ViewLoads >= MaxViewLoadsPerPeer)
                    return;

                PaintingViewRecord? record;
                peer.ViewLoads++;
                try
                {
                    record = await LoadView(paintingUid);
                }
                finally
                {
                    peer.ViewLoads--;
                }

                if (record == null)
                {
                    SendViewDenied(player, paintingUid);
                    return;
                }

                if (!_views.TryGetValue(paintingUid, out view))
                {
                    view = new CachedPaintingView(record);
                    _views[paintingUid] = view;
                    TrimViews();
                }
            }

            view.LastUsed = _timing.RealTime;

            if (player.Status == Robust.Shared.Enums.SessionStatus.Disconnected)
                return;

            if (!CanView(player, view.Record.Summary))
            {
                SendViewDenied(player, paintingUid);
                return;
            }

            GetPeer(player).Views.Add(paintingUid);

            var summary = view.Record.Summary;
            var info = new MsgCanvasViewInfo
            {
                Painting = paintingUid,
                Allowed = true,
                Status = (PaintingStatus) summary.Status,
                Width = summary.Width,
                Height = summary.Height,
                Title = summary.Title,
                Thumb = summary.ThumbHash is { } thumb ? CanvasTileContent.Blob(thumb) : CanvasTileContent.Empty,
            };

            foreach (var tile in view.Record.Tiles)
            {
                info.Tiles.Add(new CanvasLevelTile(tile.Lod, tile.Key, tile.Content));
            }

            _net.ServerSendMessage(info, player.Channel);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load painting view {paintingUid}: {e}");
        }
    }

    private async Task<PaintingViewRecord?> LoadView(Guid paintingUid)
    {
        if (!_viewLoads.TryGetValue(paintingUid, out var task))
        {
            task = _db.GetPaintingViewAsync(paintingUid);
            _viewLoads[paintingUid] = task;
        }

        try
        {
            return await task;
        }
        finally
        {
            if (_viewLoads.TryGetValue(paintingUid, out var current) && current == task)
                _viewLoads.Remove(paintingUid);
        }
    }

    private void SendViewDenied(ICommonSession player, Guid paintingUid)
    {
        if (player.Status == Robust.Shared.Enums.SessionStatus.Disconnected)
            return;

        _net.ServerSendMessage(new MsgCanvasViewInfo
        {
            Painting = paintingUid,
            Allowed = false,
        }, player.Channel);
    }

    private bool CanView(ICommonSession player, PaintingSummary summary)
    {
        var owner = summary.OwnerUserId == player.UserId.UserId;
        var admin = IsPaintingAdmin(player);

        switch (summary.Status)
        {
            case AdtPaintingStatus.Approved:
                return true;

            case AdtPaintingStatus.Submitted:
                return owner || admin || IsInWorld(summary.PaintingUid);

            case AdtPaintingStatus.Draft:
            case AdtPaintingStatus.Hidden:
            case AdtPaintingStatus.Rejected:
                return owner || admin;

            default:
                return false;
        }
    }

    public bool IsPaintingAdmin(ICommonSession player)
    {
        return _admins.HasAdminFlag(player, AdminFlags.Admin);
    }

    private bool IsInWorld(Guid paintingUid)
    {
        var query = EntityQueryEnumerator<PaintingComponent>();
        while (query.MoveNext(out var painting))
        {
            if (painting.PaintingUid == paintingUid && !painting.Hidden)
                return true;
        }

        return false;
    }

    private void TrimViews()
    {
        while (_views.Count > MaxCachedViews)
        {
            var oldest = _views.MinBy(v => v.Value.LastUsed).Key;
            _views.Remove(oldest);
        }
    }

    private bool TryServeViewBlobs(CanvasPeer peer, MsgCanvasBlobRequest msg)
    {
        if (!peer.Views.Contains(msg.Painting) || !_views.TryGetValue(msg.Painting, out var view))
            return false;

        view.LastUsed = _timing.RealTime;

        var missing = new List<CanvasHash>();
        foreach (var hash in msg.Hashes)
        {
            if (!view.Hashes.Contains(hash) || peer.Queued.Contains(hash))
                continue;

            if (_blobCache.TryGetValue(hash, out var data))
            {
                QueueBlob(peer, hash, data);
                continue;
            }

            peer.Queued.Add(hash);
            if (_blobFetches.TryGetValue(hash, out var waiters))
            {
                waiters.Add(peer);
                continue;
            }

            _blobFetches[hash] = new HashSet<CanvasPeer>
            {
                peer,
            };
            missing.Add(hash);
        }

        if (missing.Count > 0)
            FetchViewBlobsAsync(missing);

        return true;
    }

    private async void FetchViewBlobsAsync(List<CanvasHash> hashes)
    {
        var loaded = new Dictionary<CanvasHash, byte[]>();
        try
        {
            var blobs = await _db.GetPaintingBlobsAsync(hashes);
            await _blobStore.ResolveAsync(blobs.External, blobs.Data);
            loaded = blobs.Data;
            foreach (var (hash, data) in loaded)
            {
                CacheBlob(hash, data);
            }
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load painting blobs: {e}");
        }

        foreach (var hash in hashes)
        {
            if (!_blobFetches.Remove(hash, out var waiters))
                continue;

            loaded.TryGetValue(hash, out var data);
            foreach (var waiter in waiters)
            {
                waiter.Queued.Remove(hash);
                if (data != null && _peers.TryGetValue(waiter.Session.UserId, out var current) && current == waiter)
                    QueueBlob(waiter, hash, data);
            }
        }
    }

    private void QueueBlob(CanvasPeer peer, CanvasHash hash, byte[] data)
    {
        peer.Queued.Add(hash);
        peer.Outgoing.Enqueue(new CanvasOutgoingTransfer(peer.NextTransferId++, hash, data));
    }

    private void CacheBlob(CanvasHash hash, byte[] data)
    {
        if (_blobCache.ContainsKey(hash))
            return;

        _blobCache[hash] = data;
        _blobCacheOrder.Enqueue(hash);
        _blobCacheBytes += data.Length;

        while (_blobCacheBytes > MaxBlobCacheBytes && _blobCacheOrder.TryDequeue(out var oldest))
        {
            if (_blobCache.Remove(oldest, out var removed))
                _blobCacheBytes -= removed.Length;
        }
    }
}
