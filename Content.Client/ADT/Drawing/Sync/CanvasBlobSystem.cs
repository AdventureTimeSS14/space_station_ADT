using Content.Client.ADT.Drawing.Cache;
using Content.Shared.ADT.Drawing;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Drawing.Sync;

public sealed class CanvasBlobSystem : EntitySystem
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);

    [Dependency] private readonly CanvasNetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly CanvasTileCacheSystem _cache = default!;

    private readonly Dictionary<CanvasHash, BlobRequest> _requested = new();
    private readonly Dictionary<uint, CanvasClientDownload> _downloads = new();

    public event Action<CanvasHash, byte[]>? BlobArrived;

    private readonly record struct BlobRequest(Guid Painting, TimeSpan RequestedAt);

    public override void Initialize()
    {
        base.Initialize();

        _net.ChunkReceived += OnChunk;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _net.ChunkReceived -= OnChunk;
    }

    public bool TryGetCached(CanvasHash hash, out byte[] data)
    {
        return _cache.TryGet(hash, out data);
    }

    public void Invalidate(IEnumerable<CanvasHash> hashes)
    {
        foreach (var hash in hashes)
        {
            _requested.Remove(hash);
        }

        _cache.Invalidate(hashes);
    }

    public void Request(Guid painting, IEnumerable<CanvasHash> hashes, bool force = false)
    {
        var now = _timing.RealTime;
        var batch = new List<CanvasHash>();

        foreach (var hash in hashes)
        {
            if (!force &&
                _requested.TryGetValue(hash, out var existing) &&
                existing.Painting == painting &&
                now - existing.RequestedAt < RequestTimeout)
            {
                continue;
            }

            _requested[hash] = new BlobRequest(painting, now);
            batch.Add(hash);

            if (batch.Count < CanvasConstants.MaxBlobRequest)
                continue;

            Send(painting, batch);
            batch = new List<CanvasHash>();
        }

        if (batch.Count > 0)
            Send(painting, batch);
    }

    private void Send(Guid painting, List<CanvasHash> hashes)
    {
        _net.Send(new MsgCanvasBlobRequest
        {
            Painting = painting,
            Hashes = hashes,
        });
    }

    private void OnChunk(MsgCanvasChunk msg)
    {
        if (msg.Malformed || !_requested.ContainsKey(msg.Hash))
            return;

        if (!_downloads.TryGetValue(msg.TransferId, out var download))
        {
            var count = (msg.TotalLength + CanvasConstants.ChunkSize - 1) / CanvasConstants.ChunkSize;
            if (msg.TotalLength <= 0 || msg.TotalLength > CanvasConstants.MaxTileBytes || msg.Count != count)
                return;

            download = new CanvasClientDownload(msg.Hash, msg.TotalLength, count);
            _downloads[msg.TransferId] = download;
        }

        if (download.Hash != msg.Hash ||
            msg.TotalLength != download.Buffer.Length ||
            msg.Index < 0 ||
            msg.Index >= download.Received.Length ||
            download.Received[msg.Index])
        {
            return;
        }

        var offset = msg.Index * CanvasConstants.ChunkSize;
        var expected = Math.Min(CanvasConstants.ChunkSize, download.Buffer.Length - offset);
        if (msg.Data.Length != expected)
            return;

        Array.Copy(msg.Data, 0, download.Buffer, offset, expected);
        download.Received[msg.Index] = true;
        download.ReceivedCount++;

        if (!download.Complete)
            return;

        _downloads.Remove(msg.TransferId);
        _requested.Remove(download.Hash);

        var valid = CanvasHash.Compute(download.Buffer) == download.Hash;
        _net.Send(new MsgCanvasTransferAck
        {
            TransferId = msg.TransferId,
            Ok = valid,
        });

        if (!valid)
        {
            Log.Warning($"Canvas blob {download.Hash} arrived with wrong hash");
            return;
        }

        _cache.Put(download.Hash, download.Buffer);
        BlobArrived?.Invoke(download.Hash, download.Buffer);
    }
}
