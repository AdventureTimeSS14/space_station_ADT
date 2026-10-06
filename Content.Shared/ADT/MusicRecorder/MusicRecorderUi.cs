using Robust.Shared.Serialization;

namespace Content.Shared.ADT.MusicRecorder;

[Serializable, NetSerializable]
public enum MusicRecorderUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class MusicRecorderBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly bool HasCassette;
    public readonly string? CassetteName;
    public readonly MusicCassetteTrack[] Tracks;

    public MusicRecorderBoundUserInterfaceState(bool hasCassette, string? cassetteName, MusicCassetteTrack[] tracks)
    {
        HasCassette = hasCassette;
        CassetteName = cassetteName;
        Tracks = tracks;
    }
}

[Serializable, NetSerializable]
public sealed class MusicRecorderRenameMessage : BoundUserInterfaceMessage
{
    public readonly string Name;

    public MusicRecorderRenameMessage(string name)
    {
        Name = name;
    }
}

[Serializable, NetSerializable]
public sealed class MusicRecorderUploadHeaderMessage : BoundUserInterfaceMessage
{
    public readonly int TransferId;
    public readonly int TotalBytes;
    public readonly float Duration;
    public readonly string Name;

    public MusicRecorderUploadHeaderMessage(int transferId, int totalBytes, float duration, string name)
    {
        TransferId = transferId;
        TotalBytes = totalBytes;
        Duration = duration;
        Name = name;
    }
}

[Serializable, NetSerializable]
public sealed class MusicRecorderUploadChunkMessage : BoundUserInterfaceMessage
{
    public readonly int TransferId;
    public readonly int Offset;
    public readonly byte[] Data;

    public MusicRecorderUploadChunkMessage(int transferId, int offset, byte[] data)
    {
        TransferId = transferId;
        Offset = offset;
        Data = data;
    }
}

[Serializable, NetSerializable]
public sealed class MusicRecorderUploadFinishMessage : BoundUserInterfaceMessage
{
    public readonly int TransferId;

    public MusicRecorderUploadFinishMessage(int transferId)
    {
        TransferId = transferId;
    }
}

[Serializable, NetSerializable]
public sealed class MusicRecorderRemoveTrackMessage : BoundUserInterfaceMessage
{
    public readonly string TrackId;

    public MusicRecorderRemoveTrackMessage(string trackId)
    {
        TrackId = trackId;
    }
}

[Serializable, NetSerializable]
public sealed class MusicCassetteRequestAudioEvent : EntityEventArgs
{
    public readonly NetEntity Cassette;
    public readonly string TrackId;

    public MusicCassetteRequestAudioEvent(NetEntity cassette, string trackId)
    {
        Cassette = cassette;
        TrackId = trackId;
    }
}

[Serializable, NetSerializable]
public sealed class MusicCassetteAudioChunkEvent : EntityEventArgs
{
    public readonly string TrackId;
    public readonly int Total;
    public readonly int Offset;
    public readonly byte[] Data;

    public MusicCassetteAudioChunkEvent(string trackId, int total, int offset, byte[] data)
    {
        TrackId = trackId;
        Total = total;
        Offset = offset;
        Data = data;
    }
}
