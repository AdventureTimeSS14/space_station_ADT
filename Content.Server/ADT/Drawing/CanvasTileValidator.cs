using System.IO;
using Content.Shared.ADT.Drawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Server.ADT.Drawing;

public readonly record struct CanvasValidation(bool Valid, CanvasTileContent Content);

public static class CanvasTileValidator
{
    private const int ClassifyBelowBytes = 2048;

    public static CanvasValidation Validate(byte[] data, int width, int height)
    {
        if (data.Length == 0 ||
            data.Length > CanvasConstants.MaxTileBytes ||
            !CanvasPngHeader.TryRead(data, out var header) ||
            !header.IsValidTile(width, height))
        {
            return default;
        }

        var blob = CanvasTileContent.Blob(CanvasHash.Compute(data));

        try
        {
            using var stream = new MemoryStream(data, false);
            using var image = Image.Load<Rgba32>(stream);
            if (image.Width != width || image.Height != height)
                return default;

            if (data.Length >= ClassifyBelowBytes || !image.DangerousTryGetSinglePixelMemory(out var memory))
                return new CanvasValidation(true, blob);

            return new CanvasValidation(true, Classify(memory.Span) ?? blob);
        }
        catch (Exception)
        {
            return default;
        }
    }

    private static CanvasTileContent? Classify(ReadOnlySpan<Rgba32> pixels)
    {
        var first = pixels[0];
        var transparent = first.A == 0;

        for (var i = 1; i < pixels.Length; i++)
        {
            var p = pixels[i];
            if (transparent)
            {
                if (p.A != 0)
                    return null;

                continue;
            }

            if (p != first)
                return null;
        }

        if (transparent)
            return CanvasTileContent.Empty;

        var rgba = (uint) first.R << 24 | (uint) first.G << 16 | (uint) first.B << 8 | first.A;
        return CanvasTileContent.Solid(rgba);
    }
}
