using Robust.Shared.GameStates;

namespace Content.Shared.ADT.StationRadio.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StationRadioServerComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? ChannelId;
}