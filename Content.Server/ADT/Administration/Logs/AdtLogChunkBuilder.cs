using System.Buffers.Binary;
using System.Text;
using Content.Server.Database;
using Content.Shared.Database;

namespace Content.Server.ADT.Administration.Logs;

public sealed class AdtLogChunkBuilder
{
    private const int HeaderBytes = 5; // версия + количество логов

    private byte[] _buffer = new byte[64 * 1024];
    private int _pos;

    private readonly List<Guid> _players = new();
    private readonly Dictionary<Guid, int> _playerIndices = new();
    private readonly byte[] _typeMask = new byte[AdtLogChunkFormat.TypeMaskBytes];

    private int _roundId;
    private int _chunkIndex;
    private int _count;
    private int _prevId;
    private long _prevMs;
    private byte _impactMask;
    private byte _flags;
    private int _firstLogId;
    private int _lastLogId;
    private long _minMs;
    private long _maxMs;

    public int Count => _count;
    public int RawSize => _pos;

    public void Reset(int roundId, int chunkIndex)
    {
        _roundId = roundId;
        _chunkIndex = chunkIndex;
        _count = 0;
        _prevId = 0;
        _prevMs = 0;
        _impactMask = 0;
        _flags = 0;
        _firstLogId = 0;
        _lastLogId = 0;
        _minMs = long.MaxValue;
        _maxMs = long.MinValue;

        _players.Clear();
        _playerIndices.Clear();
        Array.Clear(_typeMask);

        _pos = HeaderBytes;
    }

    public void Append(AdminLog log)
    {
        var ms = AdtLogChunkFormat.ToUnixMs(log.Date);

        if (_count == 0)
            _firstLogId = log.Id;

        _lastLogId = log.Id;

        if (ms < _minMs)
            _minMs = ms;

        if (ms > _maxMs)
            _maxMs = ms;

        var typeIndex = (int) log.Type;
        if (typeIndex >= 0 && typeIndex < AdtLogChunkFormat.TypeMaskBytes * 8)
            _typeMask[typeIndex >> 3] |= (byte) (1 << (typeIndex & 7));

        _impactMask |= AdtLogChunkFormat.ImpactBit(log.Impact);

        var message = log.Message;
        var messageBytes = Encoding.UTF8.GetByteCount(message);

        EnsureCapacity(messageBytes + 64 + (log.Players?.Count ?? 0) * 5);

        WriteZigZag(log.Id - _prevId);
        _prevId = log.Id;

        _buffer[_pos++] = (byte) log.Type;
        _buffer[_pos++] = (byte) (log.Impact - LogImpact.Low);

        WriteZigZag(ms - _prevMs);
        _prevMs = ms;

        var players = log.Players;

        if (players == null || players.Count == 0)
        {
            WriteVarUInt(0);
            _flags |= AdtLogChunkHeader.FlagHasNonPlayerLogs;
        }
        else
        {
            WriteVarUInt((ulong) players.Count);

            foreach (var player in players)
            {
                WriteVarUInt((ulong) GetPlayerIndex(player.PlayerUserId));
            }
        }

        WriteVarUInt((ulong) messageBytes);
        _pos += Encoding.UTF8.GetBytes(message, _buffer.AsSpan(_pos));

        _count++;
    }

    public AdtAdminLogChunk Finish(int compressionLevel)
    {
        _buffer[0] = AdtLogChunkFormat.Version;
        BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(1, 4), _count);

        return new AdtAdminLogChunk
        {
            RoundId = _roundId,
            ChunkIndex = _chunkIndex,
            Format = AdtLogChunkFormat.Version,
            LogCount = _count,
            FirstLogId = _firstLogId,
            LastLogId = _lastLogId,
            FirstDate = AdtLogChunkFormat.FromUnixMs(_minMs),
            LastDate = AdtLogChunkFormat.FromUnixMs(_maxMs),
            RawSize = _pos,
            Flags = _flags,
            ImpactMask = _impactMask,
            TypeMask = _typeMask.AsSpan().ToArray(),
            Players = AdtLogChunkFormat.WritePlayers(_players),
            Payload = AdtLogChunkFormat.Compress(_buffer.AsSpan(0, _pos), compressionLevel),
        };
    }

    private int GetPlayerIndex(Guid player)
    {
        if (_playerIndices.TryGetValue(player, out var index))
            return index;

        index = _players.Count;
        _players.Add(player);
        _playerIndices.Add(player, index);

        return index;
    }

    private void EnsureCapacity(int extra)
    {
        var needed = _pos + extra;

        if (needed <= _buffer.Length)
            return;

        var size = _buffer.Length;

        while (size < needed)
        {
            size *= 2;
        }

        Array.Resize(ref _buffer, size);
    }

    private void WriteVarUInt(ulong value)
    {
        while (value >= 0x80)
        {
            _buffer[_pos++] = (byte) (value | 0x80);
            value >>= 7;
        }

        _buffer[_pos++] = (byte) value;
    }

    private void WriteZigZag(long value)
    {
        WriteVarUInt(AdtLogChunkFormat.ZigZagEncode(value));
    }
}
