using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Audio.Jukebox;

[NetworkedComponent, RegisterComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedJukeboxSystem))]
public sealed partial class JukeboxComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<JukeboxPrototype>? SelectedSongId;

    [DataField, AutoNetworkedField]
    public EntityUid? AudioStream;

    /// <summary>
    /// RSI state for the jukebox being on.
    /// </summary>
    [DataField]
    public string? OnState;

    /// <summary>
    /// RSI state for the jukebox being on.
    /// </summary>
    [DataField]
    public string? OffState;

    /// <summary>
    /// RSI state for the jukebox track being selected.
    /// </summary>
    [DataField]
    public string? SelectState;

    [ViewVariables]
    public bool Selecting;

    [ViewVariables]
    public float SelectAccumulator;

    /// ADT-Tweak start
    [DataField, AutoNetworkedField]
    public float Volume = 50f;

    [DataField]
    public float MinVolume = -30f;

    [DataField]
    public float MaxVolume = 0f;

    [DataField]
    public float MinSlider = 0f;

    [DataField]
    public float MaxSlider = 100f;

    [DataField, AutoNetworkedField]
    public bool LoopEnabled = false;

    [DataField]
    public TimeSpan? PlaybackStartTime;

    [DataField]
    public float CurrentPlaybackOffset = 0f;

    [DataField, AutoNetworkedField]
    public JukeboxVolumeLevel CurrentVolumeLevel = JukeboxVolumeLevel.Level3;

    [DataField]
    public double TrackLengthCache;

    /// ADT-Tweak end
}

[Serializable, NetSerializable]
public sealed class JukeboxPlayingMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class JukeboxPauseMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class JukeboxStopMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class JukeboxSelectedMessage(ProtoId<JukeboxPrototype> songId) : BoundUserInterfaceMessage
{
    public ProtoId<JukeboxPrototype> SongId { get; } = songId;
}

[Serializable, NetSerializable]
public sealed class JukeboxSetTimeMessage(float songTime) : BoundUserInterfaceMessage
{
    public float SongTime { get; } = songTime;
}

/// ADT-Tweak start
[Serializable, NetSerializable]
public sealed class JukeboxSetVolumeMessage(float volume) : BoundUserInterfaceMessage
{
    public float Volume { get; } = volume;
}


[Serializable, NetSerializable]
public sealed class JukeboxToggleLoopMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class JukeboxEjectMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public enum JukeboxVolumeLevel : byte
{
    Level0 = 0,
    Level1 = 1,
    Level2 = 2,
    Level3 = 3,
    Level4 = 4
}
/// ADT-Tweak end

[Serializable, NetSerializable]
public enum JukeboxVisuals : byte
{
    VisualState,
    HasDisk, //ADT-Tweak
    VolumeLevel //ADT-Tweak
}

[Serializable, NetSerializable]
public enum JukeboxVisualState : byte
{
    On,
    Off,
    Select,
}

public enum JukeboxVisualLayers : byte
{
    Base
}
