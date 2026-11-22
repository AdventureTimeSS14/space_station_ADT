using System.Numerics;
using Content.Client.ADT.Drawing.Canvas;
using Content.Client.ADT.Drawing.Sync;
using Content.Shared.ADT.Drawing;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.ADT.Drawing.View;

public sealed class PaintingViewControl : Control
{
    private const float MinZoom = 0.02f;
    private const float MaxZoom = 16f;

    private static readonly Color BackdropColor = Color.FromHex("#1e1e22");

    private readonly IClyde _clyde;
    private readonly CanvasBlobSystem _blobs;

    private readonly Dictionary<(byte Lod, CanvasTileKey Key), OwnedTexture> _textures = new();
    private readonly Dictionary<(byte Lod, CanvasTileKey Key), Color> _solids = new();
    private readonly Dictionary<CanvasHash, List<(byte Lod, CanvasTileKey Key)>> _waiting = new();
    private readonly Dictionary<byte, List<CanvasLevelTile>> _levels = new();

    private PaintingViewInfo? _info;
    private float _zoom = 1f;
    private Vector2 _offset;
    private bool _fitted;
    private bool _panning;
    private Vector2 _panStart;
    private Vector2 _panOrigin;

    public PaintingViewControl(IClyde clyde, CanvasBlobSystem blobs)
    {
        _clyde = clyde;
        _blobs = blobs;
        _blobs.BlobArrived += OnBlobArrived;

        RectClipContent = true;
        MouseFilter = MouseFilterMode.Stop;
        HorizontalExpand = true;
        VerticalExpand = true;
    }

    public void SetInfo(PaintingViewInfo info)
    {
        ClearTextures();
        _info = info;
        _levels.Clear();
        _fitted = false;

        foreach (var tile in info.Tiles)
        {
            if (!_levels.TryGetValue(tile.Lod, out var list))
            {
                list = new List<CanvasLevelTile>();
                _levels[tile.Lod] = list;
            }

            list.Add(tile);
        }

        FitToView();
    }

    public void FitToView()
    {
        if (_info == null || PixelSize.X <= 0 || PixelSize.Y <= 0)
            return;

        var zx = (PixelSize.X - 16f) / _info.Width;
        var zy = (PixelSize.Y - 16f) / _info.Height;
        _zoom = Math.Clamp(MathF.Min(zx, zy), MinZoom, MaxZoom);
        _offset = ((Vector2) PixelSize - new Vector2(_info.Width, _info.Height) * _zoom) / 2f;
        _fitted = true;
        RequestVisible();
    }

    protected override void Resized()
    {
        base.Resized();

        if (!_fitted)
            FitToView();
        else
            RequestVisible();
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function != EngineKeyFunctions.UIClick && args.Function != EngineKeyFunctions.UIRightClick)
            return;

        _panning = true;
        _panStart = args.RelativePixelPosition;
        _panOrigin = _offset;
        args.Handle();
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function != EngineKeyFunctions.UIClick && args.Function != EngineKeyFunctions.UIRightClick)
            return;

        _panning = false;
        RequestVisible();
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        if (!_panning)
            return;

        _offset = _panOrigin + (args.RelativePixelPosition - _panStart);
    }

    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        base.MouseWheel(args);

        if (args.Delta.Y == 0f)
            return;

        var anchor = args.RelativePixelPosition;
        var canvasAnchor = (anchor - _offset) / _zoom;
        _zoom = Math.Clamp(_zoom * MathF.Pow(1.2f, args.Delta.Y), MinZoom, MaxZoom);
        _offset = anchor - canvasAnchor * _zoom;
        args.Handle();
        RequestVisible();
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        handle.DrawRect(new UIBox2(Vector2.Zero, PixelSize), BackdropColor);

        if (_info == null || _info.Levels == 0)
            return;

        var target = TargetLevel();
        for (var lod = _info.Levels - 1; lod >= target; lod--)
        {
            if (!_levels.TryGetValue((byte) lod, out var tiles))
                continue;

            var level = CanvasConstants.LevelSize(_info.Width, _info.Height, lod);
            var scale = new Vector2((float) _info.Width / level.X, (float) _info.Height / level.Y);

            foreach (var tile in tiles)
            {
                var rect = TileRect(tile, level, scale);
                if (!IsVisible(rect))
                    continue;

                var key = (tile.Lod, tile.Key);
                if (_textures.TryGetValue(key, out var texture))
                    handle.DrawTextureRect(texture, rect);
                else if (_solids.TryGetValue(key, out var color))
                    handle.DrawRect(rect, color);
            }
        }
    }

    private UIBox2 TileRect(CanvasLevelTile tile, Vector2i level, Vector2 scale)
    {
        var x = tile.Key.X * CanvasConstants.TileSize;
        var y = tile.Key.Y * CanvasConstants.TileSize;
        var w = CanvasConstants.TileExtent(level.X, tile.Key.X);
        var h = CanvasConstants.TileExtent(level.Y, tile.Key.Y);

        var topLeft = _offset + new Vector2(x, y) * scale * _zoom;
        var size = new Vector2(w, h) * scale * _zoom;
        return new UIBox2(topLeft, topLeft + size);
    }

    private bool IsVisible(UIBox2 rect)
    {
        return rect.Right >= 0 && rect.Bottom >= 0 && rect.Left <= PixelSize.X && rect.Top <= PixelSize.Y;
    }

    private int TargetLevel()
    {
        if (_info == null || _info.Levels == 0)
            return 0;

        var lod = (int) MathF.Floor(MathF.Log2(1f / MathF.Max(_zoom, 0.0001f)));
        return Math.Clamp(lod, 0, _info.Levels - 1);
    }

    private void RequestVisible()
    {
        if (_info == null || _info.Levels == 0)
            return;

        var coarsest = _info.Levels - 1;
        LoadLevel(coarsest, false);

        var target = TargetLevel();
        if (target != coarsest)
            LoadLevel(target, true);
    }

    private void LoadLevel(int lod, bool visibleOnly)
    {
        if (_info == null || !_levels.TryGetValue((byte) lod, out var tiles))
            return;

        var level = CanvasConstants.LevelSize(_info.Width, _info.Height, lod);
        var scale = new Vector2((float) _info.Width / level.X, (float) _info.Height / level.Y);
        var request = new List<CanvasHash>();

        foreach (var tile in tiles)
        {
            var key = (tile.Lod, tile.Key);
            if (_textures.ContainsKey(key) || _solids.ContainsKey(key))
                continue;

            if (visibleOnly && !IsVisible(TileRect(tile, level, scale)))
                continue;

            switch (tile.Content.Kind)
            {
                case CanvasTileKind.Solid:
                    var rgba = CanvasTileCodec.UnpackRgba(tile.Content.Rgba);
                    _solids[key] = new Color(rgba.R, rgba.G, rgba.B, rgba.A);
                    continue;

                case CanvasTileKind.Empty:
                    _solids[key] = Color.Transparent;
                    continue;
            }

            var hash = tile.Content.Hash;
            if (_blobs.TryGetCached(hash, out var data))
            {
                CreateTexture(tile.Lod, tile.Key, level, data);
                continue;
            }

            if (!_waiting.TryGetValue(hash, out var waiting))
            {
                waiting = new List<(byte, CanvasTileKey)>();
                _waiting[hash] = waiting;
                request.Add(hash);
            }

            if (!waiting.Contains(key))
                waiting.Add(key);
        }

        if (request.Count > 0)
            _blobs.Request(_info.Painting, request);
    }

    private void OnBlobArrived(CanvasHash hash, byte[] data)
    {
        if (_info == null || !_waiting.Remove(hash, out var keys))
            return;

        foreach (var (lod, key) in keys)
        {
            var level = CanvasConstants.LevelSize(_info.Width, _info.Height, lod);
            CreateTexture(lod, key, level, data);
        }
    }

    private void CreateTexture(byte lod, CanvasTileKey key, Vector2i level, byte[] data)
    {
        var w = CanvasConstants.TileExtent(level.X, key.X);
        var h = CanvasConstants.TileExtent(level.Y, key.Y);
        if (w <= 0 || h <= 0)
            return;

        var pixels = new Rgba32[w * h];
        if (!CanvasTileCodec.TryDecodePng(data, w, h, pixels))
            return;

        var size = new Vector2i(w, h);
        var texture = _clyde.CreateBlankTexture<Rgba32>(size, "adt-painting-view");
        texture.SetSubImage(Vector2i.Zero, size, new ReadOnlySpan<Rgba32>(pixels));

        if (_textures.Remove((lod, key), out var old))
            old.Dispose();

        _textures[(lod, key)] = texture;
    }

    private void ClearTextures()
    {
        foreach (var texture in _textures.Values)
        {
            texture.Dispose();
        }

        _textures.Clear();
        _solids.Clear();
        _waiting.Clear();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _blobs.BlobArrived -= OnBlobArrived;
        ClearTextures();
    }
}
