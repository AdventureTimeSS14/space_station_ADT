using System.IO;
using Content.Server.ADT.Drawing.Database;
using Content.Shared.ADT.Drawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Content.Server.ADT.Drawing;

public sealed class CanvasPyramid
{
    public readonly List<PaintingLevelTileRecord> Tiles = new();
    public readonly Dictionary<CanvasHash, byte[]> Blobs = new();
    public CanvasHash Thumb;
    public byte[] ThumbData = Array.Empty<byte>();
}

public static class CanvasPyramidBuilder
{
    public static CanvasPyramid Build(int width, int height, IReadOnlyList<PaintingTileRecord> tiles,
        IReadOnlyDictionary<CanvasHash, byte[]> blobs)
    {
        var result = new CanvasPyramid();

        using var full = new Image<Rgba32>(width, height);
        foreach (var tile in tiles)
        {
            Paste(full, tile, blobs);
        }

        var levels = CanvasConstants.LevelCount(width, height);
        var level = full;
        for (var lod = 1; lod < levels; lod++)
        {
            var size = CanvasConstants.LevelSize(width, height, lod);
            var next = level.Clone(ctx => ctx.Resize(size.X, size.Y, KnownResamplers.Box));
            if (level != full)
                level.Dispose();

            level = next;
            Cut(level, (byte) lod, result);
        }

        if (level != full)
            level.Dispose();

        var thumbSize = CanvasConstants.ThumbSize(width, height);
        using var thumb = full.Clone(ctx => ctx.Resize(thumbSize.X, thumbSize.Y, KnownResamplers.Triangle));
        result.ThumbData = Encode(thumb);
        result.Thumb = CanvasHash.Compute(result.ThumbData);
        return result;
    }

    private static void Paste(Image<Rgba32> target, PaintingTileRecord tile, IReadOnlyDictionary<CanvasHash, byte[]> blobs)
    {
        var x0 = tile.Key.X * CanvasConstants.TileSize;
        var y0 = tile.Key.Y * CanvasConstants.TileSize;
        var w = CanvasConstants.TileExtent(target.Width, tile.Key.X);
        var h = CanvasConstants.TileExtent(target.Height, tile.Key.Y);
        if (w <= 0 || h <= 0)
            return;

        switch (tile.Content.Kind)
        {
            case CanvasTileKind.Solid:
            {
                var color = Unpack(tile.Content.Rgba);
                target.ProcessPixelRows(access =>
                {
                    for (var y = 0; y < h; y++)
                    {
                        access.GetRowSpan(y0 + y).Slice(x0, w).Fill(color);
                    }
                });
                break;
            }

            case CanvasTileKind.Blob:
            {
                if (!blobs.TryGetValue(tile.Content.Hash, out var data))
                    throw new InvalidDataException($"Missing blob {tile.Content.Hash} for tile {tile.Key}");

                using var stream = new MemoryStream(data, false);
                using var image = Image.Load<Rgba32>(stream);
                if (image.Width != w || image.Height != h)
                    throw new InvalidDataException($"Tile {tile.Key} has size {image.Width}x{image.Height}, expected {w}x{h}");

                image.ProcessPixelRows(target, (source, destination) =>
                {
                    for (var y = 0; y < h; y++)
                    {
                        source.GetRowSpan(y).CopyTo(destination.GetRowSpan(y0 + y).Slice(x0, w));
                    }
                });
                break;
            }
        }
    }

    private static void Cut(Image<Rgba32> level, byte lod, CanvasPyramid result)
    {
        var tilesX = CanvasConstants.TileCount(level.Width);
        var tilesY = CanvasConstants.TileCount(level.Height);

        for (var ty = 0; ty < tilesY; ty++)
        {
            for (var tx = 0; tx < tilesX; tx++)
            {
                var w = CanvasConstants.TileExtent(level.Width, tx);
                var h = CanvasConstants.TileExtent(level.Height, ty);
                var rect = new Rectangle(tx * CanvasConstants.TileSize, ty * CanvasConstants.TileSize, w, h);

                using var tile = level.Clone(ctx => ctx.Crop(rect));
                var key = new CanvasTileKey(0, (ushort) tx, (ushort) ty);

                var content = Classify(tile);
                if (content == null)
                {
                    var data = Encode(tile);
                    var hash = CanvasHash.Compute(data);
                    result.Blobs[hash] = data;
                    content = CanvasTileContent.Blob(hash);
                }

                result.Tiles.Add(new PaintingLevelTileRecord(lod, key, content.Value));
            }
        }
    }

    private static CanvasTileContent? Classify(Image<Rgba32> image)
    {
        var first = image[0, 0];
        var transparent = first.A == 0;
        var uniform = true;

        image.ProcessPixelRows(access =>
        {
            for (var y = 0; y < access.Height && uniform; y++)
            {
                foreach (var p in access.GetRowSpan(y))
                {
                    if (transparent ? p.A != 0 : p != first)
                    {
                        uniform = false;
                        break;
                    }
                }
            }
        });

        if (!uniform)
            return null;

        if (transparent)
            return CanvasTileContent.Empty;

        return CanvasTileContent.Solid((uint) first.R << 24 | (uint) first.G << 16 | (uint) first.B << 8 | first.A);
    }

    private static byte[] Encode(Image<Rgba32> image)
    {
        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return stream.ToArray();
    }

    private static Rgba32 Unpack(uint rgba)
    {
        return new Rgba32((byte) (rgba >> 24), (byte) (rgba >> 16), (byte) (rgba >> 8), (byte) rgba);
    }
}
