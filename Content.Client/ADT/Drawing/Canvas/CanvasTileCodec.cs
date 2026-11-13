using System.IO;
using Content.Shared.ADT.Drawing;
using Robust.Client.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.ADT.Drawing.Canvas;

public static class CanvasTileCodec
{
    public static uint PackRgba(Rgba32 color)
    {
        return (uint) color.R << 24 | (uint) color.G << 16 | (uint) color.B << 8 | color.A;
    }

    public static Rgba32 UnpackRgba(uint rgba)
    {
        return new Rgba32((byte) (rgba >> 24), (byte) (rgba >> 16), (byte) (rgba >> 8), (byte) rgba);
    }

    public static bool TryClassify(ReadOnlySpan<Rgba32> pixels, out CanvasTileContent content)
    {
        content = CanvasTileContent.Empty;
        if (pixels.Length == 0)
            return true;

        var first = pixels[0];
        var allTransparent = first.A == 0;
        var allSame = true;

        for (var i = 1; i < pixels.Length; i++)
        {
            var p = pixels[i];
            if (p.A != 0)
                allTransparent = false;

            if (p != first)
                allSame = false;

            if (!allTransparent && !allSame)
                return false;
        }

        if (allTransparent)
            return true;

        content = CanvasTileContent.Solid(PackRgba(first));
        return true;
    }

    public static byte[] EncodePng(ReadOnlySpan<Rgba32> pixels, int width, int height)
    {
        if (pixels.Length != width * height)
            throw new ArgumentException("Pixel count does not match tile size", nameof(pixels));

        using var image = new Image<Rgba32>(width, height);
        pixels.CopyTo(image.GetPixelSpan());

        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return stream.ToArray();
    }

    public static bool TryDecodePng(byte[] data, int width, int height, Span<Rgba32> destination)
    {
        if (destination.Length != width * height ||
            data.Length > CanvasConstants.MaxTileBytes ||
            !CanvasPngHeader.TryRead(data, out var header) ||
            !header.IsValidTile(width, height))
        {
            return false;
        }

        try
        {
            using var stream = new MemoryStream(data, false);
            using var image = Image.Load<Rgba32>(stream);
            if (image.Width != width || image.Height != height)
                return false;

            image.GetPixelSpan().CopyTo(destination);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static void Fill(CanvasTileContent content, Span<Rgba32> destination)
    {
        var color = content.Kind == CanvasTileKind.Solid ? UnpackRgba(content.Rgba) : default;
        destination.Fill(color);
    }
}
