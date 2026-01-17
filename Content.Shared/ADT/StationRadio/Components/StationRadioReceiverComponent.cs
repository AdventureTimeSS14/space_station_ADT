using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.StationRadio.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class StationRadioReceiverComponent : Component
{
    [DataField]
    public EntityUid? SoundEntity;

    [DataField]
    public bool Active = true;

    [DataField]
    public string? SelectedChannelId;

    /// <summary>
    /// Текущее воспроизводимое медиа (для защиты от дублей)
    /// </summary>
    [DataField]
    public SoundPathSpecifier? CurrentMedia;

    /// <summary>
    /// Время старта текущего трека (от сервера) - для синхронизации
    /// </summary>
    [DataField]
    public TimeSpan? StartTime;

    /// <summary>
    /// Уникальный ID текущего трека для предотвращения дублей
    /// </summary>
    [DataField]
    public Guid? CurrentTrackId;
}

[Serializable, NetSerializable]
public sealed class StationRadioReceiverComponentState : ComponentState
{
    public bool Active { get; }
    public string? SelectedChannelId { get; }
    public SoundPathSpecifier? CurrentMedia { get; }
    public TimeSpan? StartTime { get; }
    public Guid? CurrentTrackId { get; }

    public StationRadioReceiverComponentState(bool active, string? selectedChannelId,
        SoundPathSpecifier? currentMedia, TimeSpan? startTime, Guid? currentTrackId)
    {
        Active = active;
        SelectedChannelId = selectedChannelId;
        CurrentMedia = currentMedia;
        StartTime = startTime;
        CurrentTrackId = currentTrackId;
    }
}