using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.MusicRecorder;

/// <summary>
/// Пустая кассета. Список треков реплицируется, сами байты звука остаются на сервере.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MusicCassetteComponent : Component
{
    public const int MaxTracks = 8;
    public const int MaxTrackBytes = 1_572_864;
    // Under a 32KB net message, including the BUI envelope. 48KB chunks were dropped, so the server never saw a full file.
    public const int ChunkBytes = 16 * 1024;
    public const float MaxDurationSeconds = 15f * 60f;

    [DataField, AutoNetworkedField]
    public List<MusicCassetteTrack> Tracks = new();
}

[Serializable, NetSerializable]
public sealed class MusicCassetteTrack
{
    public string Id = string.Empty;
    public string Name = string.Empty;
    public float Duration;
    public int Size;
}
