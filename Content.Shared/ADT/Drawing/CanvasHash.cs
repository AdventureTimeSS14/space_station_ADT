using System.Buffers.Binary;
using Robust.Shared.Serialization;
using SpaceWizards.Sodium;

namespace Content.Shared.ADT.Drawing;

[Serializable, NetSerializable]
public readonly record struct CanvasHash(ulong High, ulong Low)
{
    public static CanvasHash Compute(ReadOnlySpan<byte> data)
    {
        var bytes = CryptoGenericHashBlake2B.Hash(CanvasConstants.HashLength, data, ReadOnlySpan<byte>.Empty);
        return FromBytes(bytes);
    }

    public static CanvasHash FromBytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != CanvasConstants.HashLength)
            throw new ArgumentException("Wrong hash length", nameof(bytes));

        return new CanvasHash(
            BinaryPrimitives.ReadUInt64BigEndian(bytes[..8]),
            BinaryPrimitives.ReadUInt64BigEndian(bytes[8..]));
    }

    public void WriteTo(Span<byte> destination)
    {
        BinaryPrimitives.WriteUInt64BigEndian(destination[..8], High);
        BinaryPrimitives.WriteUInt64BigEndian(destination[8..], Low);
    }

    public string ToHex()
    {
        return High.ToString("x16") + Low.ToString("x16");
    }

    public static bool TryParseHex(string hex, out CanvasHash hash)
    {
        hash = default;
        if (hex.Length != CanvasConstants.HashLength * 2)
            return false;

        if (!ulong.TryParse(hex.AsSpan(0, 16), System.Globalization.NumberStyles.AllowHexSpecifier, null, out var high) ||
            !ulong.TryParse(hex.AsSpan(16, 16), System.Globalization.NumberStyles.AllowHexSpecifier, null, out var low))
        {
            return false;
        }

        hash = new CanvasHash(high, low);
        return true;
    }

    public override string ToString()
    {
        return ToHex();
    }
}
