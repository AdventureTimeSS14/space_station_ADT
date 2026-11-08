using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Lavaland.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTLavaImmuneComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<EntProtoId> Lava = new() { "FloorLavaEntity", "ADTFloorLavaCovered", "ADTDrakeTempLava" };
}
