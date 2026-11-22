using Content.Client.ADT.Drawing.Canvas;
using Content.Client.ADT.Drawing.Sync;
using Content.Shared.ADT.Drawing;
using Robust.Client.Graphics;
using Robust.Shared.Timing;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.ADT.Drawing.View;

public sealed class PaintingViewInfo
{
    public readonly Guid Painting;
    public readonly bool Allowed;
    public readonly PaintingStatus Status;
    public readonly int Width;
    public readonly int Height;
    public readonly string Title;
    public readonly CanvasTileContent Thumb;
    public readonly List<CanvasLevelTile> Tiles;
    public readonly int Levels;

    public PaintingViewInfo(MsgCanvasViewInfo msg)
    {
        Painting = msg.Painting;
        Allowed = msg.Allowed;
        Status = msg.Status;
        Width = msg.Width;
        Height = msg.Height;
        Title = msg.Title;
        Thumb = msg.Thumb;
        Tiles = msg.Tiles;
        Levels = Allowed && CanvasConstants.IsValidSize(new Vector2i(Width, Height))
            ? CanvasConstants.LevelCount(Width, Height)
            : 0;
    }
}

public sealed class CanvasViewSystem : EntitySystem
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    [Dependency] private readonly CanvasNetManager _net = default!;
    [Dependency] private readonly CanvasBlobSystem _blobs = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<Guid, PaintingViewInfo> _infos = new();
    private readonly Dictionary<Guid, TimeSpan> _pending = new();
    private readonly Dictionary<Guid, OwnedTexture> _thumbs = new();
    private readonly HashSet<Guid> _thumbWanted = new();
    private readonly Dictionary<CanvasHash, HashSet<Guid>> _thumbWaiting = new();

    public event Action<Guid>? ViewUpdated;
    public event Action<Guid>? ThumbnailChanged;

    public override void Initialize()
    {
        base.Initialize();

        _net.ViewInfoReceived += OnViewInfo;
        _net.InvalidateReceived += OnInvalidate;
        _blobs.BlobArrived += OnBlobArrived;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _net.ViewInfoReceived -= OnViewInfo;
        _net.InvalidateReceived -= OnInvalidate;
        _blobs.BlobArrived -= OnBlobArrived;

        foreach (var texture in _thumbs.Values)
        {
            texture.Dispose();
        }

        _thumbs.Clear();
    }

    public void RequestView(Guid painting, bool force = false)
    {
        var now = _timing.RealTime;
        if (!force && _pending.TryGetValue(painting, out var requested) && now - requested < RequestTimeout)
            return;

        _pending[painting] = now;
        _net.Send(new MsgCanvasViewRequest
        {
            Painting = painting,
        });
    }

    public bool TryGetView(Guid painting, out PaintingViewInfo info)
    {
        return _infos.TryGetValue(painting, out info!);
    }

    public bool TryGetThumbnail(Guid painting, out Texture texture)
    {
        if (_thumbs.TryGetValue(painting, out var owned))
        {
            texture = owned;
            return true;
        }

        texture = default!;
        _thumbWanted.Add(painting);

        if (_infos.TryGetValue(painting, out var info))
            LoadThumb(info);
        else
            RequestView(painting);

        return false;
    }

    private void OnViewInfo(MsgCanvasViewInfo msg)
    {
        if (msg.Malformed)
            return;

        var info = new PaintingViewInfo(msg);
        _infos[msg.Painting] = info;
        _pending.Remove(msg.Painting);
        ViewUpdated?.Invoke(msg.Painting);

        if (_thumbWanted.Contains(msg.Painting))
            LoadThumb(info);
    }

    private void LoadThumb(PaintingViewInfo info)
    {
        if (!info.Allowed || info.Thumb.Kind != CanvasTileKind.Blob || _thumbs.ContainsKey(info.Painting))
            return;

        var hash = info.Thumb.Hash;
        if (_blobs.TryGetCached(hash, out var data))
        {
            CreateThumb(info, data);
            return;
        }

        if (!_thumbWaiting.TryGetValue(hash, out var waiting))
        {
            waiting = new HashSet<Guid>();
            _thumbWaiting[hash] = waiting;
        }

        waiting.Add(info.Painting);
        _blobs.Request(info.Painting, new[] { hash });
    }

    private void OnBlobArrived(CanvasHash hash, byte[] data)
    {
        if (!_thumbWaiting.Remove(hash, out var paintings))
            return;

        foreach (var painting in paintings)
        {
            if (_infos.TryGetValue(painting, out var info) && info.Thumb.Kind == CanvasTileKind.Blob && info.Thumb.Hash == hash)
                CreateThumb(info, data);
        }
    }

    private void CreateThumb(PaintingViewInfo info, byte[] data)
    {
        var size = CanvasConstants.ThumbSize(info.Width, info.Height);
        var pixels = new Rgba32[size.X * size.Y];
        if (!CanvasTileCodec.TryDecodePng(data, size.X, size.Y, pixels))
        {
            Log.Warning($"Thumbnail of painting {info.Painting} failed to decode");
            return;
        }

        var texture = _clyde.CreateBlankTexture<Rgba32>(size, "adt-painting-thumb");
        texture.SetSubImage(Vector2i.Zero, size, new ReadOnlySpan<Rgba32>(pixels));

        if (_thumbs.Remove(info.Painting, out var old))
            old.Dispose();

        _thumbs[info.Painting] = texture;
        ThumbnailChanged?.Invoke(info.Painting);
    }

    private void OnInvalidate(MsgCanvasInvalidate msg)
    {
        if (msg.Malformed)
            return;

        _infos.Remove(msg.Painting);
        _pending.Remove(msg.Painting);

        if (msg.Hashes.Count > 0)
            _blobs.Invalidate(msg.Hashes);

        if (_thumbs.Remove(msg.Painting, out var texture))
            texture.Dispose();

        ViewUpdated?.Invoke(msg.Painting);
        ThumbnailChanged?.Invoke(msg.Painting);
    }
}
