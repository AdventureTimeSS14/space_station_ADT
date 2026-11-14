using Content.Server.Database;
using Content.Shared.Database;

namespace Content.Server.ADT.Administration.Logs;

public sealed class AdtLogChunkHeader
{
    public const byte FlagHasNonPlayerLogs = 1;

    public int RoundId;
    public int ChunkIndex;
    public byte Format;
    public int LogCount;
    public int FirstLogId;
    public int LastLogId;
    public DateTime FirstDate;
    public DateTime LastDate;
    public int RawSize;
    public byte Flags;
    public byte ImpactMask;
    public byte[] TypeMask = Array.Empty<byte>();
    public Guid[] Players = Array.Empty<Guid>();

    public bool HasNonPlayerLogs => (Flags & FlagHasNonPlayerLogs) != 0;

    public bool HasType(LogType type)
    {
        var index = (int) type;
        var offset = index >> 3;

        if (offset >= TypeMask.Length)
            return false;

        return (TypeMask[offset] & (1 << (index & 7))) != 0;
    }

    public bool HasImpact(LogImpact impact)
    {
        return (ImpactMask & AdtLogChunkFormat.ImpactBit(impact)) != 0;
    }

    public bool HasAnyPlayer(Guid[] players)
    {
        foreach (var player in players)
        {
            if (Array.IndexOf(Players, player) >= 0)
                return true;
        }

        return false;
    }

    public static AdtLogChunkHeader FromEntity(AdtAdminLogChunk chunk)
    {
        return new AdtLogChunkHeader
        {
            RoundId = chunk.RoundId,
            ChunkIndex = chunk.ChunkIndex,
            Format = chunk.Format,
            LogCount = chunk.LogCount,
            FirstLogId = chunk.FirstLogId,
            LastLogId = chunk.LastLogId,
            FirstDate = chunk.FirstDate,
            LastDate = chunk.LastDate,
            RawSize = chunk.RawSize,
            Flags = chunk.Flags,
            ImpactMask = chunk.ImpactMask,
            TypeMask = chunk.TypeMask,
            Players = AdtLogChunkFormat.ReadPlayers(chunk.Players),
        };
    }
}
