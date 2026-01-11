using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.StationRadio.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StationRadioServerComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? ChannelId;

    /// <summary>
    /// Текущая трансляция на этом сервере (null — ничего не играет)
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundPathSpecifier? CurrentMedia;
}