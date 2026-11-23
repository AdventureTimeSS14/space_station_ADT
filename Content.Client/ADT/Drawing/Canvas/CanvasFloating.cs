using System.Numerics;
using Robust.Client.Graphics;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.ADT.Drawing.Canvas;

public sealed class CanvasFloating : IDisposable
{
    public readonly PaintSurface Target;
    public readonly CanvasLayer? Layer;
    public readonly CanvasSelection Selection;
    public readonly bool AutoSelected;
    public readonly PaintSurface Hole;

    public Vector2 Start;
    public Vector2 End;

    private readonly Rgba32[] _pixels;
    private OwnedTexture? _texture;

    public CanvasFloating(PaintSurface target, CanvasLayer? layer, CanvasSelection selection, bool autoSelected)
    {
        Target = target;
        Layer = layer;
        Selection = selection;
        AutoSelected = autoSelected;
        Start = new Vector2(selection.Left, selection.Top);
        End = new Vector2(selection.Right, selection.Bottom);

        _pixels = new Rgba32[selection.Width * selection.Height];
        for (var y = selection.Top; y < selection.Bottom; y++)
        {
            for (var x = selection.Left; x < selection.Right; x++)
            {
                if (selection.Contains(x, y))
                    _pixels[(y - selection.Top) * selection.Width + x - selection.Left] = target.GetPixelRaw(x, y);
            }
        }

        Hole = new PaintSurface(target.Width, target.Height, Color.Transparent, sparse: true)
        {
            History = false,
        };

        foreach (var (index, pixels) in BuildCut())
        {
            Hole.AdoptTile(index, pixels);
        }
    }

    public Texture? Texture => _texture;

    public bool Moved => Start != new Vector2(Selection.Left, Selection.Top) ||
                         End != new Vector2(Selection.Right, Selection.Bottom);

    public void Upload(IClyde clyde)
    {
        if (_texture != null)
            return;

        var size = new Vector2i(Selection.Width, Selection.Height);
        _texture = clyde.CreateBlankTexture<Rgba32>(size, "adt-paint-floating");
        _texture.SetSubImage(Vector2i.Zero, size, new ReadOnlySpan<Rgba32>(_pixels));
    }

    public Dictionary<int, Rgba32[]> Bake(bool pixelMode)
    {
        var tiles = BuildCut();

        var min = Vector2.Min(Start, End);
        var max = Vector2.Max(Start, End);
        var size = max - min;
        var scale = new Vector2(Selection.Width, Selection.Height) / size;
        var smooth = !pixelMode && (size.X != Selection.Width || size.Y != Selection.Height);
        var flipX = End.X < Start.X;
        var flipY = End.Y < Start.Y;

        var x0 = Math.Max(0, (int) MathF.Floor(min.X));
        var y0 = Math.Max(0, (int) MathF.Floor(min.Y));
        var x1 = Math.Min(Target.Width, (int) MathF.Ceiling(max.X));
        var y1 = Math.Min(Target.Height, (int) MathF.Ceiling(max.Y));

        for (var y = y0; y < y1; y++)
        {
            var v = (y + 0.5f - min.Y) * scale.Y;
            if (flipY)
                v = Selection.Height - v;

            for (var x = x0; x < x1; x++)
            {
                var u = (x + 0.5f - min.X) * scale.X;
                if (flipX)
                    u = Selection.Width - u;

                var source = smooth ? SampleBox(u, v, scale) : SampleNearest(u, v);
                if (source.A == 0)
                    continue;

                var index = TileAt(x, y);
                var pixels = TileCopy(tiles, index);
                var offset = PixelOffset(index, x, y);
                pixels[offset] = PaintSurface.Over(pixels[offset], source, source.A / 255f);
            }
        }

        return tiles;
    }

    private Dictionary<int, Rgba32[]> BuildCut()
    {
        var tiles = new Dictionary<int, Rgba32[]>();
        var fill = Target.EraseToPaper ? PaintSurface.ToRgba(Target.Paper) : default;

        for (var y = Selection.Top; y < Selection.Bottom; y++)
        {
            for (var x = Selection.Left; x < Selection.Right; x++)
            {
                if (!Selection.Contains(x, y))
                    continue;

                var index = TileAt(x, y);
                if (!Target.IsAllocated(index))
                    continue;

                TileCopy(tiles, index)[PixelOffset(index, x, y)] = fill;
            }
        }

        return tiles;
    }

    private int TileAt(int x, int y)
    {
        return Target.TileIndex(x / PaintSurface.TileSize, y / PaintSurface.TileSize);
    }

    private int PixelOffset(int index, int x, int y)
    {
        return (y % PaintSurface.TileSize) * Target.GetTileSize(index).X + x % PaintSurface.TileSize;
    }

    private Rgba32[] TileCopy(Dictionary<int, Rgba32[]> tiles, int index)
    {
        if (tiles.TryGetValue(index, out var pixels))
            return pixels;

        pixels = (Rgba32[]) Target.GetTilePixels(index).Clone();
        tiles[index] = pixels;
        return pixels;
    }

    private Rgba32 SampleNearest(float u, float v)
    {
        var x = Math.Clamp((int) MathF.Floor(u), 0, Selection.Width - 1);
        var y = Math.Clamp((int) MathF.Floor(v), 0, Selection.Height - 1);
        return _pixels[y * Selection.Width + x];
    }

    private Rgba32 SampleBox(float u, float v, Vector2 scale)
    {
        var halfU = MathF.Max(1f, scale.X) * 0.5f;
        var halfV = MathF.Max(1f, scale.Y) * 0.5f;
        var u0 = u - halfU;
        var u1 = u + halfU;
        var v0 = v - halfV;
        var v1 = v + halfV;

        var left = Math.Max(0, (int) MathF.Floor(u0));
        var right = Math.Min(Selection.Width, (int) MathF.Ceiling(u1));
        var top = Math.Max(0, (int) MathF.Floor(v0));
        var bottom = Math.Min(Selection.Height, (int) MathF.Ceiling(v1));

        float r = 0f, g = 0f, b = 0f, a = 0f;
        for (var y = top; y < bottom; y++)
        {
            var weightY = MathF.Min(v1, y + 1f) - MathF.Max(v0, y);
            for (var x = left; x < right; x++)
            {
                var pixel = _pixels[y * Selection.Width + x];
                var weight = (MathF.Min(u1, x + 1f) - MathF.Max(u0, x)) * weightY * pixel.A;
                r += pixel.R * weight;
                g += pixel.G * weight;
                b += pixel.B * weight;
                a += weight;
            }
        }

        if (a <= 0f)
            return default;

        return new Rgba32(ToByte(r / a), ToByte(g / a), ToByte(b / a), ToByte(a / ((u1 - u0) * (v1 - v0))));
    }

    private static byte ToByte(float value)
    {
        return (byte) Math.Clamp(MathF.Round(value), 0f, 255f);
    }

    public void Dispose()
    {
        _texture?.Dispose();
        _texture = null;
        Hole.Dispose();
    }
}
