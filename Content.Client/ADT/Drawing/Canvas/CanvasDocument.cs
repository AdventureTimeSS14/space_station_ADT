using System.Linq;
using System.Numerics;
using Content.Client.ADT.Drawing.Input;
using Content.Shared.ADT.Drawing;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.ADT.Drawing.Canvas;

public sealed class CanvasDocument : IDisposable
{
    private readonly TileState[] _tiles;
    private readonly List<CanvasOperation> _pending = new();
    private readonly Dictionary<int, DeferredTile> _deferred = new();
    private uint _nextOperationId = 1;
    private CanvasLayer? _strokeLayer;

    public CanvasDocument(int width, int height, Color background, BrushSettings brush)
    {
        Surface = new PaintSurface(width, height, background);
        Stroker = new BrushStroker(Surface, brush);
        Stroker.StrokeFinished += OnStrokeFinished;
        Layers = new CanvasLayerStack(width, height, brush);
        Layers.Changed += OnLayersChanged;
        Surface.HistoryChanged += () => HistoryChanged?.Invoke();

        var initial = InitialContent(PaintSurface.ToRgba(background));
        _tiles = new TileState[Surface.TileCount];
        for (var i = 0; i < _tiles.Length; i++)
        {
            _tiles[i] = new TileState
            {
                Content = initial,
            };
        }
    }

    public PaintSurface Surface { get; }

    public BrushStroker Stroker { get; }

    public CanvasLayerStack Layers { get; }

    public PaintSurface ActiveSurface => Layers.Active?.Surface ?? Surface;

    public bool ActiveStroke => Stroker.Active || Layers.Active is { Stroker.Active: true };

    public event Action? HistoryChanged;

    public int Width => Surface.Width;

    public int Height => Surface.Height;

    public IReadOnlyList<CanvasOperation> Pending => _pending;

    public event Action? ModifiedChanged;

    public event Action? Edited;

    public event Action<int>? TileApplied;

    public event Action? WorkSettled;

    public bool HasPendingWork
    {
        get
        {
            foreach (var tile in _tiles)
            {
                if (tile.Modified || tile.InFlight)
                    return true;
            }

            return false;
        }
    }

    public void HandleSample(in PenSample sample, float taperOutLength)
    {
        if (sample.Phase == PenPhase.Hover)
            return;

        if (sample.Phase == PenPhase.Down)
            _strokeLayer = Layers.Active;

        if (_strokeLayer is { } layer)
        {
            if (Layers.Find(layer.Id) != layer)
            {
                _strokeLayer = null;
                return;
            }

            layer.Stroker.TaperOutLength = taperOutLength;
            layer.Stroker.Handle(sample, 0);
            if (sample.Phase == PenPhase.Up)
                _strokeLayer = null;

            return;
        }

        Stroker.TaperOutLength = taperOutLength;
        Stroker.Handle(sample, (int) _nextOperationId);

        if (!Stroker.Active)
            FlushDeferred();
    }

    public bool MergeLayer(CanvasLayer layer)
    {
        var layers = Layers.Layers;
        var top = -1;
        for (var i = 0; i < layers.Count; i++)
        {
            if (layers[i] == layer)
                top = i;
        }

        if (top < 0)
            return false;

        var group = new List<CanvasLayer>();
        for (var i = 0; i <= top; i++)
        {
            if (layers[i] == layer || layers[i].Visible)
                group.Add(layers[i]);
        }

        return MergeLayers(group, layer);
    }

    public bool MergeVisibleLayers()
    {
        return MergeLayers(Layers.Layers.Where(l => l.Visible).ToList(), null);
    }

    private bool MergeLayers(List<CanvasLayer> group, CanvasLayer? selected)
    {
        if (Stroker.Active || group.Any(l => l.Stroker.Active))
            return false;

        var tiles = new Dictionary<int, Rgba32[]>();
        foreach (var layer in group)
        {
            if ((layer != selected && !layer.Visible) || layer.Opacity <= 0f)
                continue;

            foreach (var index in layer.Surface.AllocatedTiles())
            {
                var source = layer.Surface.GetTilePixels(index);
                if (!tiles.TryGetValue(index, out var pixels))
                {
                    pixels = new Rgba32[source.Length];
                    tiles[index] = pixels;
                }

                PaintSurface.CompositePixels(pixels, source, layer.Opacity);
            }
        }

        if (tiles.Count > 0)
        {
            var id = _nextOperationId++;
            var touched = Surface.CompositeTiles(tiles, 1f, (int) id);
            AddOperation(CanvasOperation.Merging(id, tiles, 1f, touched));
        }

        foreach (var layer in group)
        {
            Layers.Remove(layer);
        }

        return true;
    }

    public bool FloodFill(int x, int y, Color color, float opacity, float tolerance)
    {
        var surface = ActiveSurface;
        if (ActiveStroke || x < 0 || y < 0 || x >= surface.Width || y >= surface.Height)
            return false;

        var width = surface.Width;
        var height = surface.Height;
        var target = surface.GetPixelRaw(x, y);
        var limit = (int) MathF.Round(Math.Clamp(tolerance, 0f, 1f) * 255f);
        var fill = PaintSurface.ToRgba(color);
        var tiles = new Dictionary<int, Rgba32[]>();
        var visited = new bool[width * height];
        var stack = new Stack<(int X, int Y)>();
        stack.Push((x, y));

        bool Open(int px, int py)
        {
            return !visited[py * width + px] && Matches(surface.GetPixelRaw(px, py), target, limit);
        }

        void Scan(int left, int right, int row)
        {
            var inSpan = false;
            for (var px = left; px <= right; px++)
            {
                if (!Open(px, row))
                {
                    inSpan = false;
                    continue;
                }

                if (inSpan)
                    continue;

                stack.Push((px, row));
                inSpan = true;
            }
        }

        while (stack.TryPop(out var seed))
        {
            if (!Open(seed.X, seed.Y))
                continue;

            var left = seed.X;
            while (left > 0 && Open(left - 1, seed.Y))
            {
                left--;
            }

            var right = seed.X;
            while (right < width - 1 && Open(right + 1, seed.Y))
            {
                right++;
            }

            for (var px = left; px <= right; px++)
            {
                visited[seed.Y * width + px] = true;
                SetRegionPixel(surface, tiles, px, seed.Y, fill);
            }

            if (seed.Y > 0)
                Scan(left, right, seed.Y - 1);

            if (seed.Y < height - 1)
                Scan(left, right, seed.Y + 1);
        }

        return ApplyRegion(tiles, opacity);
    }

    public bool FillShape(Vector2 start, Vector2 end, bool ellipse, Color color, float opacity)
    {
        var surface = ActiveSurface;
        if (ActiveStroke)
            return false;

        var min = Vector2.Min(start, end);
        var max = Vector2.Max(start, end);
        var x0 = Math.Max(0, (int) MathF.Floor(min.X));
        var y0 = Math.Max(0, (int) MathF.Floor(min.Y));
        var x1 = Math.Min(surface.Width - 1, (int) MathF.Ceiling(max.X));
        var y1 = Math.Min(surface.Height - 1, (int) MathF.Ceiling(max.Y));

        var center = (min + max) / 2f;
        var radius = (max - min) / 2f;
        var fill = PaintSurface.ToRgba(color);
        var tiles = new Dictionary<int, Rgba32[]>();

        for (var py = y0; py <= y1; py++)
        {
            for (var px = x0; px <= x1; px++)
            {
                var point = new Vector2(px + 0.5f, py + 0.5f);
                if (point.X < min.X || point.Y < min.Y || point.X > max.X || point.Y > max.Y)
                    continue;

                if (ellipse)
                {
                    if (radius.X <= 0f || radius.Y <= 0f)
                        continue;

                    var d = (point - center) / radius;
                    if (d.LengthSquared() > 1f)
                        continue;
                }

                SetRegionPixel(surface, tiles, px, py, fill);
            }
        }

        return ApplyRegion(tiles, opacity);
    }

    private static bool Matches(Rgba32 pixel, Rgba32 target, int limit)
    {
        return Math.Abs(pixel.R - target.R) <= limit &&
               Math.Abs(pixel.G - target.G) <= limit &&
               Math.Abs(pixel.B - target.B) <= limit &&
               Math.Abs(pixel.A - target.A) <= limit;
    }

    private static void SetRegionPixel(PaintSurface surface, Dictionary<int, Rgba32[]> tiles, int x, int y, Rgba32 fill)
    {
        var index = surface.TileIndex(x / PaintSurface.TileSize, y / PaintSurface.TileSize);
        var size = surface.GetTileSize(index);
        if (!tiles.TryGetValue(index, out var pixels))
        {
            pixels = new Rgba32[size.X * size.Y];
            tiles[index] = pixels;
        }

        pixels[(y % PaintSurface.TileSize) * size.X + x % PaintSurface.TileSize] = fill;
    }

    private bool ApplyRegion(Dictionary<int, Rgba32[]> tiles, float opacity)
    {
        if (tiles.Count == 0)
            return false;

        if (Layers.Active is { } layer)
        {
            layer.Surface.CompositeTiles(tiles, opacity, 0);
            return true;
        }

        var id = _nextOperationId++;
        var touched = Surface.CompositeTiles(tiles, opacity, (int) id);
        AddOperation(CanvasOperation.Merging(id, tiles, opacity, touched));
        return true;
    }

    public Rgba32 ComposedPixel(int x, int y)
    {
        var basePixel = PaintSurface.ToRgba(Surface.GetPixel(x, y));
        return Layers.Compose(basePixel, x, y);
    }

    private void OnLayersChanged()
    {
        foreach (var layer in Layers.Layers)
        {
            layer.Surface.HistoryChanged -= OnLayerHistory;
            layer.Surface.HistoryChanged += OnLayerHistory;
        }

        HistoryChanged?.Invoke();
    }

    private void OnLayerHistory()
    {
        HistoryChanged?.Invoke();
    }

    public void Undo()
    {
        if (Layers.Active is { } layer)
        {
            layer.Surface.Undo(out _, out _);
            return;
        }

        if (!Surface.Undo(out var tag, out var tiles))
            return;

        var id = (uint) tag;
        var operation = _pending.Find(o => o.Id == id);
        if (operation != null)
            _pending.Remove(operation);

        var restore = new Dictionary<int, RestoreTile>();
        foreach (var tile in tiles)
        {
            if (operation == null || !operation.UncommittedTiles.Contains(tile.Index))
                restore[tile.Index] = new RestoreTile(tile.After, tile.Before);
        }

        if (restore.Count > 0)
            _pending.Add(CanvasOperation.Restoring(_nextOperationId++, restore));

        MarkModified(tiles.Select(t => t.Index));
    }

    public void Redo()
    {
        if (Layers.Active is { } layer)
        {
            layer.Surface.Redo(out _, out _);
            return;
        }

        if (!Surface.Redo(out _, out var tiles))
            return;

        var restore = new Dictionary<int, RestoreTile>();
        foreach (var tile in tiles)
        {
            restore[tile.Index] = new RestoreTile(tile.Before, tile.After);
        }

        if (restore.Count > 0)
            _pending.Add(CanvasOperation.Restoring(_nextOperationId++, restore));

        MarkModified(tiles.Select(t => t.Index));
    }

    public void Clear(Color color)
    {
        var id = _nextOperationId++;
        var tiles = Surface.Clear(color, (int) id);
        AddOperation(new CanvasOperation(id, null, PaintSurface.ToRgba(color), tiles));
    }

    #region Коммиты

    public IEnumerable<int> ModifiedTiles()
    {
        for (var i = 0; i < _tiles.Length; i++)
        {
            if (_tiles[i].Modified && !_tiles[i].InFlight)
                yield return i;
        }
    }

    public CanvasTileKey KeyOf(int index)
    {
        var (x, y) = Surface.TileCoords(index);
        return new CanvasTileKey(0, (ushort) x, (ushort) y);
    }

    public bool TryGetIndex(CanvasTileKey key, out int index)
    {
        index = -1;
        if (key.Layer != 0 || key.X >= Surface.TilesX || key.Y >= Surface.TilesY)
            return false;

        index = Surface.TileIndex(key.X, key.Y);
        return true;
    }

    public CanvasTileState GetTileState(int index)
    {
        var tile = _tiles[index];
        if (_deferred.TryGetValue(index, out var deferred))
            return new CanvasTileState(deferred.Version, deferred.Content, tile.Modified, tile.InFlight);

        return new CanvasTileState(tile.Version, tile.Content, tile.Modified, tile.InFlight);
    }

    public CanvasTilePrepared PrepareCommit(int index)
    {
        var tile = _tiles[index];
        var size = Surface.GetTileSize(index);
        var pixels = (Rgba32[]) Surface.GetTilePixels(index).Clone();

        var operations = _pending
            .Where(o => o.UncommittedTiles.Contains(index))
            .Select(o => o.Id)
            .Take(CanvasConstants.MaxStrokeIds)
            .ToArray();

        tile.Modified = false;
        tile.InFlight = true;
        tile.InFlightOperations = operations;
        return new CanvasTilePrepared(KeyOf(index), tile.Version, size, pixels, operations);
    }

    public static CanvasTileCommit Encode(CanvasTilePrepared prepared)
    {
        byte[]? png = null;
        if (!CanvasTileCodec.TryClassify(prepared.Pixels, out var content))
        {
            png = CanvasTileCodec.EncodePng(prepared.Pixels, prepared.Size.X, prepared.Size.Y);
            content = CanvasTileContent.Blob(CanvasHash.Compute(png));
        }

        return new CanvasTileCommit(prepared.Key, prepared.BaseVersion, content, png, prepared.Operations);
    }

    public void ConfirmCommit(int index, uint version, CanvasTileContent content)
    {
        var tile = _tiles[index];
        tile.Version = version;
        tile.Content = content;
        tile.InFlight = false;

        foreach (var id in tile.InFlightOperations)
        {
            var operation = _pending.Find(o => o.Id == id);
            operation?.UncommittedTiles.Remove(index);
        }

        tile.InFlightOperations = Array.Empty<uint>();
        _pending.RemoveAll(o => o.UncommittedTiles.Count == 0);

        if (!HasPendingWork)
            WorkSettled?.Invoke();
    }

    public void RejectCommit(int index)
    {
        var tile = _tiles[index];
        tile.InFlight = false;
        tile.InFlightOperations = Array.Empty<uint>();
        tile.Modified = true;
        ModifiedChanged?.Invoke();
    }

    public bool IsStale(int index, uint version)
    {
        var state = GetTileState(index);
        return version < state.Version || version == state.Version && state.Version != 0;
    }

    public void ApplyAuthoritative(int index, uint version, CanvasTileContent content, Rgba32[]? pixels)
    {
        if (IsStale(index, version))
            return;

        if (Stroker.Active)
        {
            _deferred[index] = new DeferredTile(version, content, pixels);
            return;
        }

        Apply(index, version, content, pixels);
    }

    #endregion

    private void Apply(int index, uint version, CanvasTileContent content, Rgba32[]? pixels)
    {
        var tile = _tiles[index];
        tile.Version = version;
        tile.Content = content;

        if (pixels != null)
            Surface.AdoptTile(index, pixels);
        else
            Surface.FillTile(index, content.Kind == CanvasTileKind.Solid ? CanvasTileCodec.UnpackRgba(content.Rgba) : default);

        var rebased = false;
        foreach (var operation in _pending)
        {
            if (!operation.UncommittedTiles.Contains(index))
                continue;

            Replay(operation, index);
            rebased = true;
        }

        if (rebased && !tile.Modified)
        {
            tile.Modified = true;
            ModifiedChanged?.Invoke();
        }

        TileApplied?.Invoke(index);
    }

    private void FlushDeferred()
    {
        if (_deferred.Count == 0)
            return;

        foreach (var (index, deferred) in _deferred.ToList())
        {
            _deferred.Remove(index);
            Apply(index, deferred.Version, deferred.Content, deferred.Pixels);
        }
    }

    private void Replay(CanvasOperation operation, int index)
    {
        if (operation.Stroke != null)
        {
            Stroker.Replay(operation.Stroke, index);
            return;
        }

        if (operation.Restore != null)
        {
            if (operation.Restore.TryGetValue(index, out var restore))
                Surface.MergeTile(index, restore.From, restore.To);

            return;
        }

        if (operation.Layer != null)
        {
            if (operation.Layer.TryGetValue(index, out var source))
                Surface.CompositeTile(index, source, operation.LayerOpacity);

            return;
        }

        Surface.FillTile(index, operation.Fill);
    }

    private void OnStrokeFinished(StrokeRecord stroke, int tag, int[] tiles)
    {
        var id = (uint) tag;
        if (id >= _nextOperationId)
            _nextOperationId = id + 1;

        AddOperation(new CanvasOperation(id, stroke, default, tiles));
    }

    private void AddOperation(CanvasOperation operation)
    {
        _pending.Add(operation);
        MarkModified(operation.UncommittedTiles);
    }

    private void MarkModified(IEnumerable<int> tiles)
    {
        var changed = false;
        foreach (var index in tiles)
        {
            if (_tiles[index].Modified)
                continue;

            _tiles[index].Modified = true;
            changed = true;
        }

        if (changed)
            ModifiedChanged?.Invoke();

        Edited?.Invoke();
    }

    private static CanvasTileContent InitialContent(Rgba32 background)
    {
        return background.A == 0
            ? CanvasTileContent.Empty
            : CanvasTileContent.Solid(CanvasTileCodec.PackRgba(background));
    }

    public void Dispose()
    {
        Surface.Dispose();
        Layers.Dispose();
    }

    private sealed class TileState
    {
        public uint Version;
        public CanvasTileContent Content;
        public bool Modified;
        public bool InFlight;
        public uint[] InFlightOperations = Array.Empty<uint>();
    }

    private readonly record struct DeferredTile(uint Version, CanvasTileContent Content, Rgba32[]? Pixels);
}

public sealed record CanvasTilePrepared(
    CanvasTileKey Key,
    uint BaseVersion,
    Vector2i Size,
    Rgba32[] Pixels,
    uint[] Operations);

public readonly record struct RestoreTile(Rgba32[] From, Rgba32[] To);

public sealed class CanvasOperation
{
    public readonly uint Id;
    public readonly StrokeRecord? Stroke;
    public readonly Rgba32 Fill;
    public readonly Dictionary<int, RestoreTile>? Restore;
    public readonly Dictionary<int, Rgba32[]>? Layer;
    public readonly float LayerOpacity;
    public readonly HashSet<int> UncommittedTiles;

    public CanvasOperation(uint id, StrokeRecord? stroke, Rgba32 fill, IEnumerable<int> tiles)
    {
        Id = id;
        Stroke = stroke;
        Fill = fill;
        UncommittedTiles = new HashSet<int>(tiles);
    }

    private CanvasOperation(uint id, Dictionary<int, RestoreTile> restore)
    {
        Id = id;
        Restore = restore;
        UncommittedTiles = new HashSet<int>(restore.Keys);
    }

    private CanvasOperation(uint id, Dictionary<int, Rgba32[]> layer, float opacity, IEnumerable<int> tiles)
    {
        Id = id;
        Layer = layer;
        LayerOpacity = opacity;
        UncommittedTiles = new HashSet<int>(tiles);
    }

    public static CanvasOperation Restoring(uint id, Dictionary<int, RestoreTile> restore)
    {
        return new CanvasOperation(id, restore);
    }

    public static CanvasOperation Merging(uint id, Dictionary<int, Rgba32[]> layer, float opacity, IEnumerable<int> tiles)
    {
        return new CanvasOperation(id, layer, opacity, tiles);
    }
}

public readonly record struct CanvasTileState(uint Version, CanvasTileContent Content, bool Modified, bool InFlight);

public sealed record CanvasTileCommit(
    CanvasTileKey Key,
    uint BaseVersion,
    CanvasTileContent Content,
    byte[]? Png,
    uint[] Operations);
