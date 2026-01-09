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
}