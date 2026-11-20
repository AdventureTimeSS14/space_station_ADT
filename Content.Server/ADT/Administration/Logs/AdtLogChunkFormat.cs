using System.Buffers;
using System.IO;
using Content.Shared.Database;
using Robust.Shared.Utility;

namespace Content.Server.ADT.Administration.Logs;

/// <summary>
/// Compressed admin log chunk layout and reading primitives.
/// </summary>
/// <remarks>
/// Payload version 1, packed without alignment.
/// The payload contains the version, log count, and for each log: delta ID, LogType, LogImpact + 1, delta timestamp, player count, player indices, message length, and UTF-8 message.
/// Messages are stored inside the payload, so the entire chunk must be decompressed. Chunks are bounded by log count and size, keeping peak memory usage fixed per chunk.
/// </remarks>
public static class AdtLogChunkFormat
{
    public const byte Version = 1;

    public const int TypeMaskBytes = 32;
    public const int GuidBytes = 16;

    public static byte ImpactBit(LogImpact impact)
    {
        return (byte) (1 << (impact - LogImpact.Low));
    }

    public static byte[] WritePlayers(IReadOnlyList<Guid> players)
    {
        if (players.Count == 0)
            return Array.Empty<byte>();

        var bytes = new byte[players.Count * GuidBytes];

        for (var i = 0; i < players.Count; i++)
        {
            players[i].TryWriteBytes(bytes.AsSpan(i * GuidBytes, GuidBytes));
        }

        return bytes;
    }

    public static Guid[] ReadPlayers(byte[]? bytes)
    {
        if (bytes == null || bytes.Length < GuidBytes)
            return Array.Empty<Guid>();

        var players = new Guid[bytes.Length / GuidBytes];

        for (var i = 0; i < players.Length; i++)
        {
            players[i] = new Guid(bytes.AsSpan(i * GuidBytes, GuidBytes));
        }

        return players;
    }

    public static ulong ReadVarUInt(ReadOnlySpan<byte> data, ref int pos)
    {
        ulong result = 0;
        var shift = 0;

        while (true)
        {
            var b = data[pos++];
            result |= (ulong) (b & 0x7F) << shift;

            if ((b & 0x80) == 0)
                return result;

            shift += 7;

            if (shift > 63)
                throw new InvalidDataException("Повреждённый varint в чанке админ-логов.");
        }
    }

    public static int ReadVarInt32(ReadOnlySpan<byte> data, ref int pos)
    {
        return (int) ReadVarUInt(data, ref pos);
    }

    public static long ReadZigZag(ReadOnlySpan<byte> data, ref int pos)
    {
        var raw = ReadVarUInt(data, ref pos);
        return (long) (raw >> 1) ^ -(long) (raw & 1);
    }

    public static ulong ZigZagEncode(long value)
    {
        return (ulong) ((value << 1) ^ (value >> 63));
    }

    public static long ToUnixMs(DateTime date)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(date, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
    }

    public static DateTime FromUnixMs(long ms)
    {
        return DateTime.UnixEpoch.AddMilliseconds(ms);
    }

    public static byte[] Compress(ReadOnlySpan<byte> raw, int level)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(ZStd.CompressBound(raw.Length));

        try
        {
            var written = ZStd.Compress(buffer, raw, level);
            return buffer.AsSpan(0, written).ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static int Decompress(byte[] payload, Span<byte> into)
    {
        using var source = new MemoryStream(payload, false);
        using var zstd = new ZStdDecompressStream(source, false);

        var total = 0;

        while (total < into.Length)
        {
            var read = zstd.Read(into[total..]);

            if (read == 0)
                break;

            total += read;
        }

        return total;
    }
}
