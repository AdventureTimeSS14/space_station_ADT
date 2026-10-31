using System.IO;
using System.Linq;
using System.Numerics;
using Content.Shared.ADT.Drawing;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client.ADT.Drawing.Canvas;

public sealed class PaintSurface : IDisposable
{
    public const int TileSize = CanvasConstants.TileSize;
    public const int MaxSize = CanvasConstants.MaxSide;

    public long UndoBudget = 160L * 1024 * 1024;

    public readonly int Width;
    public readonly int Height;
    public readonly int TilesX;
    public readonly int TilesY;

    private readonly Tile[] _tiles;
    private const int MaxPooledMasks = 32;
    private const float BlurKernelPerRadius = 0.3f;
    private const int MaxBlurKernel = 16;
    private const float WarpScaleRate = 0.08f;

    private readonly List<int> _strokeTiles = new();
    private readonly Dictionary<int, Stack<float[]>> _maskPool = new();
    private readonly LinkedList<HistoryStep> _undo = new();
    private readonly Stack<HistoryStep> _redo = new();
    private long _undoBytes;

    private bool _inStroke;
    private Rgba32 _strokeColor;
    private bool _strokeErase;
    private int _strokeClip = -1;
    private CanvasSelection? _strokeSelection;
    private long[] _blurSums = Array.Empty<long>();
    private Vector2[] _warpBuffer = Array.Empty<Vector2>();

    public PaintSurface(int width, int height, Color background, bool sparse = false)
    {
        if (width <= 0 || height <= 0 || width > MaxSize || height > MaxSize)
            throw new ArgumentOutOfRangeException(nameof(width), $"Canvas size must be 1..{MaxSize}");

        Width = width;
        Height = height;
        TilesX = CanvasConstants.TileCount(width);
        TilesY = CanvasConstants.TileCount(height);
        _tiles = new Tile[TilesX * TilesY];

        var fill = ToRgba(background);
        for (var ty = 0; ty < TilesY; ty++)
        {
            for (var tx = 0; tx < TilesX; tx++)
            {
                var w = CanvasConstants.TileExtent(width, tx);
                var h = CanvasConstants.TileExtent(height, ty);
                var tile = new Tile(tx * TileSize, ty * TileSize, w, h);
                if (!sparse)
                    Array.Fill(tile.Pixels, fill);

                _tiles[ty * TilesX + tx] = tile;
            }
        }
    }

    public bool History { get; init; } = true;

    public Color Paper { get; init; } = Color.White;

    public bool EraseToPaper { get; init; } = true;

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public bool InStroke => _inStroke;
    public int TileCount => _tiles.Length;

    public event Action? HistoryChanged;

    #region Тайлы

    public int TileIndex(int tx, int ty)
    {
        return ty * TilesX + tx;
    }

    public (int X, int Y) TileCoords(int index)
    {
        return (index % TilesX, index / TilesX);
    }

    public Vector2i GetTileSize(int index)
    {
        var tile = _tiles[index];
        return new Vector2i(tile.W, tile.H);
    }

    public Rgba32[] GetTilePixels(int index)
    {
        return _tiles[index].Pixels;
    }

    public void ReplaceTile(int index, ReadOnlySpan<Rgba32> pixels)
    {
        var tile = _tiles[index];
        if (pixels.Length != tile.Pixels.Length)
            throw new ArgumentException("Pixel count does not match tile size", nameof(pixels));

        if (_inStroke && tile.StrokeBase != null)
        {
            pixels.CopyTo(tile.StrokeBase);
            Array.Clear(tile.StrokeMask!);
        }

        var replaced = new Rgba32[tile.Pixels.Length];
        pixels.CopyTo(replaced);
        tile.Pixels = replaced;
        tile.Dirty = true;
    }

    public void AdoptTile(int index, Rgba32[] pixels)
    {
        var tile = _tiles[index];
        if (pixels.Length != tile.W * tile.H)
            throw new ArgumentException("Pixel count does not match tile size", nameof(pixels));

        if (_inStroke && tile.StrokeBase != null)
        {
            pixels.CopyTo(tile.StrokeBase, 0);
            Array.Clear(tile.StrokeMask!);
        }

        tile.Pixels = pixels;
        tile.Dirty = true;
    }

    public void FillTile(int index, Rgba32 color)
    {
        var tile = _tiles[index];
        if (_inStroke && tile.StrokeBase != null)
        {
            Array.Fill(tile.StrokeBase, color);
            Array.Clear(tile.StrokeMask!);
        }

        Array.Fill(tile.Pixels, color);
        tile.Dirty = true;
    }

    public void MergeTile(int index, Rgba32[] from, Rgba32[] to)
    {
        var tile = _tiles[index];
        var pixels = tile.Pixels;
        if (from.Length != pixels.Length || to.Length != pixels.Length)
            return;

        for (var i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].PackedValue == from[i].PackedValue)
                pixels[i] = to[i];
        }

        tile.Dirty = true;
    }

    public void ResetTile(int index)
    {
        var tile = _tiles[index];
        if (tile.StrokeBase != null || !tile.Allocated)
            return;

        tile.Release();
        tile.Dirty = true;
    }

    public IEnumerable<int> AllocatedTiles()
    {
        for (var i = 0; i < _tiles.Length; i++)
        {
            if (_tiles[i].Allocated)
                yield return i;
        }
    }

    public void ReleaseAll()
    {
        EndStroke(record: false);
        foreach (var tile in _tiles)
        {
            tile.Release();
            tile.Dirty = true;
        }

        _undo.Clear();
        _redo.Clear();
        _undoBytes = 0;
        HistoryChanged?.Invoke();
    }

    public int[] CompositeTiles(Dictionary<int, Rgba32[]> sources, float opacity, int tag)
    {
        EndStroke();

        var step = new HistoryStep(tag);
        var touched = new List<int>();
        foreach (var (index, source) in sources)
        {
            var tile = _tiles[index];
            if (source.Length != tile.W * tile.H)
                continue;

            var before = History ? (Rgba32[]) tile.Pixels.Clone() : null;
            CompositeTile(index, source, opacity);
            touched.Add(index);

            if (before != null)
                step.Tiles.Add(new HistoryTile(index, before, (Rgba32[]) tile.Pixels.Clone()));
        }

        if (History && step.Tiles.Count > 0)
            PushUndo(step);

        return touched.ToArray();
    }

    public Dictionary<int, RestoreTile> ReplaceTiles(Dictionary<int, Rgba32[]> sources, int tag)
    {
        EndStroke();

        var step = new HistoryStep(tag);
        var changes = new Dictionary<int, RestoreTile>();
        foreach (var (index, pixels) in sources)
        {
            var tile = _tiles[index];
            if (pixels.Length != tile.W * tile.H)
                continue;

            var before = tile.Pixels;
            tile.Pixels = pixels;
            tile.Dirty = true;

            var after = (Rgba32[]) pixels.Clone();
            changes[index] = new RestoreTile(before, after);
            if (History)
                step.Tiles.Add(new HistoryTile(index, before, after));
        }

        if (History && step.Tiles.Count > 0)
            PushUndo(step);

        return changes;
    }

    public void CompositeTile(int index, Rgba32[] source, float opacity)
    {
        var tile = _tiles[index];
        if (source.Length != tile.Pixels.Length)
            return;

        CompositePixels(tile.Pixels, source, opacity);
        tile.Dirty = true;
    }

    public static void CompositePixels(Rgba32[] destination, Rgba32[] source, float opacity)
    {
        if (source.Length != destination.Length)
            return;

        opacity = Math.Clamp(opacity, 0f, 1f);
        for (var i = 0; i < destination.Length; i++)
        {
            var src = source[i];
            if (src.A == 0)
                continue;

            destination[i] = Over(destination[i], src, src.A / 255f * opacity);
        }
    }

    public Rgba32 GetPixelRaw(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return default;

        var tile = _tiles[(y / TileSize) * TilesX + x / TileSize];
        if (!tile.Allocated)
            return default;

        return tile.Pixels[(y - tile.Y) * tile.W + (x - tile.X)];
    }

    public static Rgba32 Over(Rgba32 dst, Rgba32 src, float sa)
    {
        var da = dst.A / 255f;
        var outA = sa + da * (1f - sa);
        if (outA <= 0f)
            return default;

        var k = da * (1f - sa);
        return new Rgba32(
            ToByte((src.R * sa + dst.R * k) / outA / 255f),
            ToByte((src.G * sa + dst.G * k) / outA / 255f),
            ToByte((src.B * sa + dst.B * k) / outA / 255f),
            ToByte(outA));
    }

    public bool IsAllocated(int index)
    {
        return _tiles[index].Allocated;
    }

    #endregion

    #region Штрих

    public void BeginStroke(Color color, bool erase, int clipTile = -1, CanvasSelection? selection = null)
    {
        if (_inStroke)
            EndStroke();

        _inStroke = true;
        _strokeErase = erase && !EraseToPaper;
        _strokeColor = ToRgba(erase && EraseToPaper ? Paper : color);
        _strokeClip = clipTile;
        _strokeSelection = selection;
    }

    public void Dab(Vector2 center, float radius, float hardness, float alpha)
    {
        if (!_inStroke ||
            !float.IsFinite(center.X) ||
            !float.IsFinite(center.Y) ||
            !float.IsFinite(radius) ||
            !float.IsFinite(hardness) ||
            !float.IsFinite(alpha) ||
            alpha <= 0f)
        {
            return;
        }

        if (radius < 0.5f)
        {
            alpha *= MathF.Max(radius, 0f) / 0.5f;
            radius = 0.5f;
        }

        radius = MathF.Min(radius, MaxSize);
        alpha = Math.Clamp(alpha, 0f, 1f);
        hardness = Math.Clamp(hardness, 0f, 1f);

        var reach = radius + 1f;
        var minX = Math.Max(0, (int) MathF.Floor(MathF.Max(center.X - reach, -1f)));
        var minY = Math.Max(0, (int) MathF.Floor(MathF.Max(center.Y - reach, -1f)));
        var maxX = Math.Min(Width - 1, (int) MathF.Ceiling(MathF.Min(center.X + reach, Width)));
        var maxY = Math.Min(Height - 1, (int) MathF.Ceiling(MathF.Min(center.Y + reach, Height)));
        if (minX > maxX || minY > maxY)
            return;

        var tx0 = minX / TileSize;
        var tx1 = maxX / TileSize;
        var ty0 = minY / TileSize;
        var ty1 = maxY / TileSize;

        for (var ty = ty0; ty <= ty1; ty++)
        {
            for (var tx = tx0; tx <= tx1; tx++)
            {
                var index = ty * TilesX + tx;
                if (_strokeClip >= 0 && index != _strokeClip)
                    continue;

                var tile = _tiles[index];
                EnsureStrokeData(index, tile);
                DabTile(tile, center, radius, hardness, alpha,
                    Math.Max(minX, tile.X) - tile.X, Math.Max(minY, tile.Y) - tile.Y,
                    Math.Min(maxX, tile.X + tile.W - 1) - tile.X, Math.Min(maxY, tile.Y + tile.H - 1) - tile.Y);
            }
        }
    }

    public void FillCells(int left, int top, int size, float alpha)
    {
        if (!_inStroke || size <= 0 || !float.IsFinite(alpha) || alpha <= 0f)
            return;

        alpha = Math.Clamp(alpha, 0f, 1f);
        var x0 = Math.Max(0, left);
        var y0 = Math.Max(0, top);
        var x1 = Math.Min(Width - 1, left + size - 1);
        var y1 = Math.Min(Height - 1, top + size - 1);

        for (var y = y0; y <= y1; y++)
        {
            for (var x = x0; x <= x1; x++)
            {
                var index = (y / TileSize) * TilesX + x / TileSize;
                if (_strokeClip >= 0 && index != _strokeClip)
                    continue;

                if (_strokeSelection is { } selection && !selection.Contains(x, y))
                    continue;

                var tile = _tiles[index];
                EnsureStrokeData(index, tile);

                var i = (y - tile.Y) * tile.W + (x - tile.X);
                var mask = tile.StrokeMask!;
                if (alpha <= mask[i])
                    continue;

                mask[i] = alpha;
                tile.Pixels[i] = Composite(tile.StrokeBase![i], alpha);
                tile.Dirty = true;
            }
        }
    }

    public int[] EndStroke(bool record = true, int tag = 0)
    {
        if (!_inStroke)
            return Array.Empty<int>();

        _inStroke = false;
        _strokeClip = -1;
        _strokeSelection = null;

        var touched = _strokeTiles.ToArray();
        _strokeTiles.Clear();

        if (touched.Length == 0)
            return touched;

        record &= History;
        var step = new HistoryStep(tag);
        foreach (var index in touched)
        {
            var tile = _tiles[index];
            if (record)
                step.Tiles.Add(new HistoryTile(index, tile.StrokeBase!, (Rgba32[]) tile.Pixels.Clone()));

            tile.StrokeBase = null;
            tile.StrokeWarp = null;
            ReturnMask(tile.StrokeMask!);
            tile.StrokeMask = null;
        }

        if (record)
            PushUndo(step);

        return touched;
    }

    private float[] RentMask(int length)
    {
        if (_maskPool.TryGetValue(length, out var pool) && pool.TryPop(out var mask))
        {
            Array.Clear(mask);
            return mask;
        }

        return new float[length];
    }

    private void ReturnMask(float[] mask)
    {
        if (!_maskPool.TryGetValue(mask.Length, out var pool))
        {
            pool = new Stack<float[]>();
            _maskPool[mask.Length] = pool;
        }

        if (pool.Count < MaxPooledMasks)
            pool.Push(mask);
    }

    private void EnsureStrokeData(int index, Tile tile)
    {
        if (tile.StrokeBase != null)
            return;

        tile.StrokeBase = (Rgba32[]) tile.Pixels.Clone();
        tile.StrokeMask = RentMask(tile.Pixels.Length);
        _strokeTiles.Add(index);
    }

    private void DabTile(Tile tile, Vector2 center, float radius, float hardness, float alpha,
        int x0, int y0, int x1, int y1)
    {
        var pixels = tile.Pixels;
        var mask = tile.StrokeMask!;
        var baseline = tile.StrokeBase!;
        var stride = tile.W;
        var invRadius = 1f / radius;
        var reach = (radius + 0.5f) * (radius + 0.5f);
        var selection = _strokeSelection;
        var changed = false;

        for (var y = y0; y <= y1; y++)
        {
            var dy = tile.Y + y + 0.5f - center.Y;
            var dy2 = dy * dy;
            if (dy2 >= reach)
                continue;

            var row = y * stride;

            for (var x = x0; x <= x1; x++)
            {
                var dx = tile.X + x + 0.5f - center.X;
                var d2 = dx * dx + dy2;
                if (d2 >= reach)
                    continue;

                if (selection != null && !selection.Contains(tile.X + x, tile.Y + y))
                    continue;

                var dist = MathF.Sqrt(d2);

                var coverage = Falloff(dist, radius, invRadius, hardness) * alpha;
                var i = row + x;
                if (coverage <= mask[i])
                    continue;

                mask[i] = coverage;
                pixels[i] = Composite(baseline[i], coverage);
                changed = true;
            }
        }

        if (changed)
            tile.Dirty = true;
    }

    public void BlurDab(Vector2 center, float radius, float hardness, float alpha)
    {
        if (!_inStroke ||
            !float.IsFinite(center.X) ||
            !float.IsFinite(center.Y) ||
            !float.IsFinite(radius) ||
            !float.IsFinite(hardness) ||
            !float.IsFinite(alpha) ||
            alpha <= 0f)
        {
            return;
        }

        radius = Math.Clamp(radius, 0.5f, MaxSize);
        alpha = Math.Clamp(alpha, 0f, 1f);
        hardness = Math.Clamp(hardness, 0f, 1f);

        var reach = radius + 1f;
        var minX = Math.Max(0, (int) MathF.Floor(MathF.Max(center.X - reach, -1f)));
        var minY = Math.Max(0, (int) MathF.Floor(MathF.Max(center.Y - reach, -1f)));
        var maxX = Math.Min(Width - 1, (int) MathF.Ceiling(MathF.Min(center.X + reach, Width)));
        var maxY = Math.Min(Height - 1, (int) MathF.Ceiling(MathF.Min(center.Y + reach, Height)));
        if (minX > maxX || minY > maxY)
            return;

        var kernel = Math.Clamp((int) MathF.Round(radius * BlurKernelPerRadius), 1, MaxBlurKernel);
        var areaLeft = Math.Max(0, minX - kernel);
        var areaTop = Math.Max(0, minY - kernel);
        var area = UIBox2i.FromDimensions(
            areaLeft,
            areaTop,
            Math.Min(Width - 1, maxX + kernel) - areaLeft + 1,
            Math.Min(Height - 1, maxY + kernel) - areaTop + 1);
        BuildBlurSums(area);

        for (var ty = minY / TileSize; ty <= maxY / TileSize; ty++)
        {
            for (var tx = minX / TileSize; tx <= maxX / TileSize; tx++)
            {
                var index = ty * TilesX + tx;
                if (_strokeClip >= 0 && index != _strokeClip)
                    continue;

                var tile = _tiles[index];
                EnsureStrokeData(index, tile);
                BlurTile(tile, center, radius, hardness, alpha, kernel, area,
                    Math.Max(minX, tile.X) - tile.X, Math.Max(minY, tile.Y) - tile.Y,
                    Math.Min(maxX, tile.X + tile.W - 1) - tile.X, Math.Min(maxY, tile.Y + tile.H - 1) - tile.Y);
            }
        }
    }

    private void BuildBlurSums(UIBox2i area)
    {
        var width = area.Width;
        var height = area.Height;
        var stride = (width + 1) * 4;
        var length = stride * (height + 1);
        if (_blurSums.Length < length)
            _blurSums = new long[length];

        var sums = _blurSums;
        for (var i = 0; i < stride; i++)
        {
            sums[i] = 0;
        }

        for (var y = 0; y < height; y++)
        {
            var row = (y + 1) * stride;
            sums[row] = 0;
            sums[row + 1] = 0;
            sums[row + 2] = 0;
            sums[row + 3] = 0;

            long r = 0, g = 0, b = 0, a = 0;
            for (var x = 0; x < width; x++)
            {
                var pixel = GetPixelRaw(area.Left + x, area.Top + y);
                r += pixel.R * pixel.A;
                g += pixel.G * pixel.A;
                b += pixel.B * pixel.A;
                a += pixel.A;

                var i = row + (x + 1) * 4;
                sums[i] = sums[i - stride] + r;
                sums[i + 1] = sums[i + 1 - stride] + g;
                sums[i + 2] = sums[i + 2 - stride] + b;
                sums[i + 3] = sums[i + 3 - stride] + a;
            }
        }
    }

    private void BlurTile(Tile tile, Vector2 center, float radius, float hardness, float alpha, int kernel, UIBox2i area,
        int x0, int y0, int x1, int y1)
    {
        var pixels = tile.Pixels;
        var sums = _blurSums;
        var stride = (area.Width + 1) * 4;
        var invRadius = 1f / radius;
        var reach = (radius + 0.5f) * (radius + 0.5f);
        var selection = _strokeSelection;

        for (var y = y0; y <= y1; y++)
        {
            var dy = tile.Y + y + 0.5f - center.Y;
            var dy2 = dy * dy;
            if (dy2 >= reach)
                continue;

            var cy = tile.Y + y - area.Top;
            var top = Math.Max(cy - kernel, 0) * stride;
            var bottom = (Math.Min(cy + kernel, area.Height - 1) + 1) * stride;
            var rows = (bottom - top) / stride;

            for (var x = x0; x <= x1; x++)
            {
                var dx = tile.X + x + 0.5f - center.X;
                var d2 = dx * dx + dy2;
                if (d2 >= reach)
                    continue;

                if (selection != null && !selection.Contains(tile.X + x, tile.Y + y))
                    continue;

                var coverage = Falloff(MathF.Sqrt(d2), radius, invRadius, hardness) * alpha;
                if (coverage <= 0f)
                    continue;

                var cx = tile.X + x - area.Left;
                var left = Math.Max(cx - kernel, 0) * 4;
                var right = (Math.Min(cx + kernel, area.Width - 1) + 1) * 4;
                var count = (float) ((right - left) / 4 * rows);

                var i = y * tile.W + x;
                var current = pixels[i];
                var currentA = (float) current.A;
                var outA = Mix(currentA, BoxSum(sums, top, bottom, left, right, 3) / count, coverage);
                if (outA <= 0f)
                {
                    pixels[i] = default;
                    continue;
                }

                var outR = Mix(current.R * currentA, BoxSum(sums, top, bottom, left, right, 0) / count, coverage);
                var outG = Mix(current.G * currentA, BoxSum(sums, top, bottom, left, right, 1) / count, coverage);
                var outB = Mix(current.B * currentA, BoxSum(sums, top, bottom, left, right, 2) / count, coverage);
                pixels[i] = new Rgba32(
                    ToByte(outR / outA / 255f),
                    ToByte(outG / outA / 255f),
                    ToByte(outB / outA / 255f),
                    ToByte(outA / 255f));
            }
        }

        tile.Dirty = true;
    }

    public void WarpDab(Vector2 center, Vector2 motion, float radius, float hardness, float strength, LiquifyMode mode, bool nearest)
    {
        if (!_inStroke ||
            !float.IsFinite(center.X) ||
            !float.IsFinite(center.Y) ||
            !float.IsFinite(motion.X) ||
            !float.IsFinite(motion.Y) ||
            !float.IsFinite(radius) ||
            !float.IsFinite(hardness) ||
            !float.IsFinite(strength) ||
            strength <= 0f)
        {
            return;
        }

        radius = Math.Clamp(radius, 0.5f, MaxSize);
        strength = Math.Clamp(strength, 0f, 1f);
        hardness = Math.Clamp(hardness, 0f, 1f);

        var bound = radius + 1f;
        var minX = Math.Max(0, (int) MathF.Floor(MathF.Max(center.X - bound, -1f)));
        var minY = Math.Max(0, (int) MathF.Floor(MathF.Max(center.Y - bound, -1f)));
        var maxX = Math.Min(Width - 1, (int) MathF.Ceiling(MathF.Min(center.X + bound, Width)));
        var maxY = Math.Min(Height - 1, (int) MathF.Ceiling(MathF.Min(center.Y + bound, Height)));
        if (minX > maxX || minY > maxY)
            return;

        var width = maxX - minX + 1;
        var height = maxY - minY + 1;
        if (_warpBuffer.Length < width * height)
            _warpBuffer = new Vector2[width * height];

        var buffer = _warpBuffer;
        var invRadius = 1f / radius;
        var reach = (radius + 0.5f) * (radius + 0.5f);
        var selection = _strokeSelection;

        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                var i = (y - minY) * width + x - minX;
                var point = new Vector2(x + 0.5f, y + 0.5f);
                var offset = point - center;
                var d2 = offset.LengthSquared();
                var influence = d2 < reach ? Falloff(MathF.Sqrt(d2), radius, invRadius, hardness) * strength : 0f;
                if (influence <= 0f || selection != null && !selection.Contains(x, y))
                {
                    buffer[i] = new Vector2(float.NaN);
                    continue;
                }

                var source = mode switch
                {
                    LiquifyMode.Grow => point - offset * WarpScaleRate * influence,
                    LiquifyMode.Shrink => point + offset * WarpScaleRate * influence,
                    _ => point - motion * influence,
                };
                buffer[i] = source - point + SampleWarp(source);
            }
        }

        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                var warp = buffer[(y - minY) * width + x - minX];
                if (float.IsNaN(warp.X))
                    continue;

                var index = (y / TileSize) * TilesX + x / TileSize;
                var tile = _tiles[index];
                EnsureStrokeData(index, tile);
                tile.StrokeWarp ??= new Vector2[tile.W * tile.H];

                var i = (y - tile.Y) * tile.W + (x - tile.X);
                tile.StrokeWarp[i] = warp;
                tile.Pixels[i] = SampleBase(new Vector2(x + 0.5f, y + 0.5f) + warp, nearest);
                tile.Dirty = true;
            }
        }
    }

    private Vector2 WarpAt(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return Vector2.Zero;

        var tile = _tiles[(y / TileSize) * TilesX + x / TileSize];
        return tile.StrokeWarp == null ? Vector2.Zero : tile.StrokeWarp[(y - tile.Y) * tile.W + (x - tile.X)];
    }

    private Vector2 SampleWarp(Vector2 position)
    {
        var fx = position.X - 0.5f;
        var fy = position.Y - 0.5f;
        var x = (int) MathF.Floor(fx);
        var y = (int) MathF.Floor(fy);
        var tx = fx - x;
        var ty = fy - y;

        var top = Vector2.Lerp(WarpAt(x, y), WarpAt(x + 1, y), tx);
        var bottom = Vector2.Lerp(WarpAt(x, y + 1), WarpAt(x + 1, y + 1), tx);
        return Vector2.Lerp(top, bottom, ty);
    }

    private Rgba32 BaseAt(int x, int y)
    {
        x = Math.Clamp(x, 0, Width - 1);
        y = Math.Clamp(y, 0, Height - 1);

        var tile = _tiles[(y / TileSize) * TilesX + x / TileSize];
        if (tile.StrokeBase == null && !tile.Allocated)
            return default;

        var pixels = tile.StrokeBase ?? tile.Pixels;
        return pixels[(y - tile.Y) * tile.W + (x - tile.X)];
    }

    private Rgba32 SampleBase(Vector2 position, bool nearest)
    {
        if (nearest)
            return BaseAt((int) MathF.Floor(position.X), (int) MathF.Floor(position.Y));

        var fx = position.X - 0.5f;
        var fy = position.Y - 0.5f;
        var x = (int) MathF.Floor(fx);
        var y = (int) MathF.Floor(fy);
        var tx = fx - x;
        var ty = fy - y;

        float r = 0f, g = 0f, b = 0f, a = 0f;
        for (var iy = 0; iy < 2; iy++)
        {
            for (var ix = 0; ix < 2; ix++)
            {
                var pixel = BaseAt(x + ix, y + iy);
                var weight = (ix == 0 ? 1f - tx : tx) * (iy == 0 ? 1f - ty : ty) * pixel.A;
                r += pixel.R * weight;
                g += pixel.G * weight;
                b += pixel.B * weight;
                a += weight;
            }
        }

        if (a <= 0f)
            return default;

        return new Rgba32(ToByte(r / a / 255f), ToByte(g / a / 255f), ToByte(b / a / 255f), ToByte(a / 255f));
    }

    private static float BoxSum(long[] sums, int top, int bottom, int left, int right, int channel)
    {
        return sums[bottom + right + channel] - sums[top + right + channel] - sums[bottom + left + channel] + sums[top + left + channel];
    }

    private static float Mix(float from, float to, float t)
    {
        return from + (to - from) * t;
    }

    private static float Falloff(float dist, float radius, float invRadius, float hardness)
    {
        var edge = Math.Clamp(radius + 0.5f - dist, 0f, 1f);
        if (hardness >= 1f)
            return edge;

        var t = dist * invRadius;
        if (t <= hardness)
            return edge;

        var s = Math.Clamp((t - hardness) / (1f - hardness), 0f, 1f);
        var soft = 1f - s * s * (3f - 2f * s);
        return MathF.Min(edge, soft);
    }

    private Rgba32 Composite(Rgba32 dst, float coverage)
    {
        var da = dst.A / 255f;
        if (_strokeErase)
            return new Rgba32(dst.R, dst.G, dst.B, ToByte(da * (1f - coverage)));

        var sa = _strokeColor.A / 255f * coverage;
        var outA = sa + da * (1f - sa);
        if (outA <= 0f)
            return default;

        var k = da * (1f - sa);
        return new Rgba32(
            ToByte((_strokeColor.R * sa + dst.R * k) / outA / 255f),
            ToByte((_strokeColor.G * sa + dst.G * k) / outA / 255f),
            ToByte((_strokeColor.B * sa + dst.B * k) / outA / 255f),
            ToByte(outA));
    }

    #endregion

    #region История

    public bool Undo(out int tag, out IReadOnlyList<HistoryTile> tiles)
    {
        tag = 0;
        tiles = Array.Empty<HistoryTile>();
        if (_inStroke || _undo.Last is not { } node)
            return false;

        var step = node.Value;
        _undo.RemoveLast();
        _undoBytes -= step.Bytes;
        foreach (var tile in step.Tiles)
        {
            MergeTile(tile.Index, tile.After, tile.Before);
        }

        _redo.Push(step);
        tag = step.Tag;
        tiles = step.Tiles;
        HistoryChanged?.Invoke();
        return true;
    }

    public bool Redo(out int tag, out IReadOnlyList<HistoryTile> tiles)
    {
        tag = 0;
        tiles = Array.Empty<HistoryTile>();
        if (_inStroke || !_redo.TryPop(out var step))
            return false;

        foreach (var tile in step.Tiles)
        {
            MergeTile(tile.Index, tile.Before, tile.After);
        }

        _undo.AddLast(step);
        _undoBytes += step.Bytes;
        TrimUndo();
        tag = step.Tag;
        tiles = step.Tiles;
        HistoryChanged?.Invoke();
        return true;
    }

    public Dictionary<int, RestoreTile> LastChanges()
    {
        var changes = new Dictionary<int, RestoreTile>();
        if (_undo.Last is not { } node)
            return changes;

        foreach (var tile in node.Value.Tiles)
        {
            changes[tile.Index] = new RestoreTile(tile.Before, tile.After);
        }

        return changes;
    }

    public int[] Clear(Color color, int tag = 0)
    {
        EndStroke();

        var fill = ToRgba(color);
        var step = new HistoryStep(tag);
        var touched = new int[_tiles.Length];
        for (var i = 0; i < _tiles.Length; i++)
        {
            var tile = _tiles[i];
            var before = tile.Pixels;
            tile.Pixels = new Rgba32[before.Length];
            Array.Fill(tile.Pixels, fill);
            tile.Dirty = true;
            touched[i] = i;

            if (History)
                step.Tiles.Add(new HistoryTile(i, before, (Rgba32[]) tile.Pixels.Clone()));
        }

        if (History)
            PushUndo(step);

        return touched;
    }

    private void PushUndo(HistoryStep step)
    {
        _redo.Clear();
        _undo.AddLast(step);
        _undoBytes += step.Bytes;
        TrimUndo();
        HistoryChanged?.Invoke();
    }

    private void TrimUndo()
    {
        while (_undoBytes > UndoBudget && _undo.Count > 1)
        {
            _undoBytes -= _undo.First!.Value.Bytes;
            _undo.RemoveFirst();
        }
    }

    #endregion

    #region Чтение/экспорт

    public Color GetPixel(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return Color.Transparent;

        var tile = _tiles[(y / TileSize) * TilesX + x / TileSize];
        var p = tile.Pixels[(y - tile.Y) * tile.W + (x - tile.X)];
        return new Color(p.R, p.G, p.B, p.A);
    }

    public void SavePng(Stream stream, IReadOnlyList<CanvasLayer>? layers = null)
    {
        using var image = new Image<Rgba32>(Width, Height);
        var span = image.GetPixelSpan();
        for (var i = 0; i < _tiles.Length; i++)
        {
            var tile = _tiles[i];
            var pixels = tile.Pixels;

            if (layers != null)
            {
                pixels = (Rgba32[]) pixels.Clone();
                foreach (var layer in layers)
                {
                    if (layer.Visible && layer.Surface.IsAllocated(i))
                        CompositePixels(pixels, layer.Surface.GetTilePixels(i), layer.Opacity);
                }
            }

            for (var y = 0; y < tile.H; y++)
            {
                pixels.AsSpan(y * tile.W, tile.W).CopyTo(span.Slice((tile.Y + y) * Width + tile.X, tile.W));
            }
        }

        image.SaveAsPng(stream);
    }

    #endregion

    #region GPU

    public void Flush(IClyde clyde, int maxUploads = int.MaxValue)
    {
        var uploads = 0;
        foreach (var tile in _tiles)
        {
            if (!tile.Allocated)
            {
                tile.Dirty = false;
                continue;
            }

            if (!tile.Dirty && tile.Texture != null)
                continue;

            if (uploads++ >= maxUploads)
                return;

            tile.Texture ??= clyde.CreateBlankTexture<Rgba32>(new Vector2i(tile.W, tile.H), "adt-paint-tile");
            tile.Texture.SetSubImage(Vector2i.Zero, new Vector2i(tile.W, tile.H), new ReadOnlySpan<Rgba32>(tile.Pixels));
            tile.Dirty = false;
        }
    }

    public IEnumerable<(int Index, UIBox2i Rect, Texture? Texture)> EnumerateTiles()
    {
        for (var i = 0; i < _tiles.Length; i++)
        {
            var tile = _tiles[i];
            if (!tile.Allocated)
                continue;

            yield return (i, UIBox2i.FromDimensions(tile.X, tile.Y, tile.W, tile.H), tile.Texture);
        }
    }

    public Texture? TileTexture(int index)
    {
        return _tiles[index].Texture;
    }

    public void Dispose()
    {
        foreach (var tile in _tiles)
        {
            tile.Texture?.Dispose();
            tile.Texture = null;
        }
    }

    #endregion

    public static Rgba32 ToRgba(Color c)
    {
        return new Rgba32(ToByte(c.R), ToByte(c.G), ToByte(c.B), ToByte(c.A));
    }

    private static byte ToByte(float v)
    {
        return (byte) Math.Clamp(MathF.Round(v * 255f), 0f, 255f);
    }

    private sealed class Tile
    {
        public readonly int X;
        public readonly int Y;
        public readonly int W;
        public readonly int H;

        private Rgba32[]? _pixels;
        public OwnedTexture? Texture;
        public bool Dirty = true;

        public Rgba32[]? StrokeBase;
        public float[]? StrokeMask;
        public Vector2[]? StrokeWarp;

        public Tile(int x, int y, int w, int h)
        {
            X = x;
            Y = y;
            W = w;
            H = h;
        }

        public Rgba32[] Pixels
        {
            get => _pixels ??= new Rgba32[W * H];
            set => _pixels = value;
        }

        public bool Allocated => _pixels != null;

        public void Release()
        {
            _pixels = null;
        }
    }

    private sealed class HistoryStep
    {
        public readonly int Tag;
        public readonly List<HistoryTile> Tiles = new();

        public HistoryStep(int tag)
        {
            Tag = tag;
        }

        public long Bytes => Tiles.Sum(t => (long) (t.Before.Length + t.After.Length) * 4);
    }
}

public readonly record struct HistoryTile(int Index, Rgba32[] Before, Rgba32[] After);
