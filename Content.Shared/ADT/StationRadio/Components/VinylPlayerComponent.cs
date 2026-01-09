using Robust.Shared.GameStates;

namespace Content.Shared.ADT.StationRadio.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VinylPlayerComponent : Component
{
    [DataField]
    public bool RelayToRadios;

    [DataField, AutoNetworkedField]
    public EntityUid? SoundEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? InsertedVinyl;
}