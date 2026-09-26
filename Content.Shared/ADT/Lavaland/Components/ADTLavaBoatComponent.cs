using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Lavaland.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTLavaBoatComponent : Component
{
    [DataField]
    public List<EntProtoId> Surfaces = new() { "FloorLavaEntity" };
}
