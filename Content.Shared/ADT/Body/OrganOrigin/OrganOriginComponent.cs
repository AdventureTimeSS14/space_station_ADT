using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Body.OrganOrigin;

[RegisterComponent, NetworkedComponent]
public sealed partial class OrganOriginComponent : Component
{
    [DataField]
    public ProtoId<SpeciesPrototype>? Species;
}
