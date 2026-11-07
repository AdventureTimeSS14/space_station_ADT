using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Body.Limbs;

[RegisterComponent, NetworkedComponent]
public sealed partial class LimbOrganComponent : Component
{
    [DataField(required: true)]
    public LimbParts Part;
}
