using System.Buffers.Binary;

namespace Content.Shared.ADT.Drawing;

public readonly record struct CanvasPngHeader(int Width, int Height, byte BitDepth, byte ColorType)
{
    private const int SignatureLength = 8;
    private const int HeaderEnd = SignatureLength + 8 + 13;

    private static readonly byte[] Signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    public static bool TryRead(ReadOnlySpan<byte> data, out CanvasPngHeader header)
    {
        header = default;
        if (data.Length < HeaderEnd || !data[..SignatureLength].SequenceEqual(Signature))
            return false;

        var chunkLength = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(SignatureLength, 4));
        var chunkType = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(SignatureLength + 4, 4));
        if (chunkLength != 13 || chunkType != 0x49484452)
            return false;

        var ihdr = data.Slice(SignatureLength + 8, 13);
        var width = BinaryPrimitives.ReadUInt32BigEndian(ihdr[..4]);
        var height = BinaryPrimitives.ReadUInt32BigEndian(ihdr.Slice(4, 4));
        if (width == 0 || height == 0 || width > CanvasConstants.MaxSide || height > CanvasConstants.MaxSide)
            return false;

        header = new CanvasPngHeader((int) width, (int) height, ihdr[8], ihdr[9]);
        return true;
    }

    public bool IsValidTile(int width, int height)
    {
        return Width == width &&
               Height == height &&
               BitDepth == 8 &&
               ColorType is 0 or 2 or 3 or 4 or 6;
    }
}
