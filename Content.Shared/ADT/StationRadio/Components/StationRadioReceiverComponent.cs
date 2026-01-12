using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.StationRadio.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StationRadioReceiverComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? SoundEntity;

    [DataField, AutoNetworkedField]
    public bool Active = true;

    [DataField, AutoNetworkedField]
    public string? SelectedChannelId;

    /// <summary>
    /// Текущее воспроизводимое медиа (для точной защиты от дублей)
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundPathSpecifier? CurrentMedia;
}
