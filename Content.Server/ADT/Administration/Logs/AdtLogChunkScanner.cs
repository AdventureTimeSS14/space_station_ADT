using System.Buffers.Binary;
using System.IO;
using Content.Shared.Database;

namespace Content.Server.ADT.Administration.Logs;

public ref struct AdtLogChunkScanner
{
    private readonly ReadOnlySpan<byte> _data;
    private readonly Span<int> _playerIndices;
    private int _pos;
    private int _prevId;
    private long _prevMs;

    public readonly int Count;

    public int Id { get; private set; }
    public LogType Type { get; private set; }
    public LogImpact Impact { get; private set; }
    public long DateMs { get; private set; }
    public int PlayerCount { get; private set; }
    public ReadOnlySpan<byte> Message { get; private set; }

    public readonly ReadOnlySpan<int> Players => _playerIndices[..PlayerCount];

    public AdtLogChunkScanner(ReadOnlySpan<byte> data, Span<int> playerIndices)
    {
        _data = data;
        _playerIndices = playerIndices;
        _pos = 0;
        _prevId = 0;
        _prevMs = 0;

        Id = 0;
        Type = default;
        Impact = default;
        DateMs = 0;
        PlayerCount = 0;
        Message = default;

        var version = _data[_pos++];

        if (version != AdtLogChunkFormat.Version)
            throw new InvalidDataException($"Неизвестная версия чанка админ-логов: {version}.");

        Count = BinaryPrimitives.ReadInt32LittleEndian(_data.Slice(_pos, 4));
        _pos += 4;
    }

    public bool MoveNext()
    {
        if (_pos >= _data.Length)
            return false;

        Id = _prevId + (int) AdtLogChunkFormat.ReadZigZag(_data, ref _pos);
        _prevId = Id;

        Type = (LogType) _data[_pos++];
        Impact = (LogImpact) (_data[_pos++] + (int) LogImpact.Low);

        DateMs = _prevMs + AdtLogChunkFormat.ReadZigZag(_data, ref _pos);
        _prevMs = DateMs;

        PlayerCount = AdtLogChunkFormat.ReadVarInt32(_data, ref _pos);

        for (var i = 0; i < PlayerCount; i++)
        {
            var index = AdtLogChunkFormat.ReadVarInt32(_data, ref _pos);

            if (i < _playerIndices.Length)
                _playerIndices[i] = index;
        }

        if (PlayerCount > _playerIndices.Length)
            PlayerCount = _playerIndices.Length;

        var length = AdtLogChunkFormat.ReadVarInt32(_data, ref _pos);
        Message = _data.Slice(_pos, length);
        _pos += length;

        return true;
    }
}
