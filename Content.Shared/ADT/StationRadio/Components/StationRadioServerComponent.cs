using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.StationRadio.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class StationRadioServerComponent : Component
{
    [DataField]
    public string? ChannelId;

    /// <summary>
    /// Текущая трансляция на этом сервере (null — ничего не играет)
    /// </summary>
    [DataField]
    public SoundPathSpecifier? CurrentMedia;

    /// <summary>
    /// Время начала текущей трансляции (для синхронизации)
    /// </summary>
    [DataField]
    public TimeSpan? BroadcastStartTime;

    /// <summary>
    /// Уникальный ID текущей трансляции
    /// </summary>
    [DataField]
    public Guid? CurrentBroadcastId;
}

[Serializable, NetSerializable]
public sealed class StationRadioServerComponentState : ComponentState
{
    public string? ChannelId { get; }
    public SoundPathSpecifier? CurrentMedia { get; }
    public TimeSpan? BroadcastStartTime { get; }
    public Guid? CurrentBroadcastId { get; }

    public StationRadioServerComponentState(string? channelId, SoundPathSpecifier? currentMedia,
        TimeSpan? broadcastStartTime, Guid? currentBroadcastId)
    {
        ChannelId = channelId;
        CurrentMedia = currentMedia;
        BroadcastStartTime = broadcastStartTime;
        CurrentBroadcastId = currentBroadcastId;
    }
}