using Content.Shared.Body;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Implants;

[RegisterComponent, NetworkedComponent]
public sealed partial class OrganImplantComponent : Component
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Organ;
}
