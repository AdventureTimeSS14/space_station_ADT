using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Body.Legs;

[RegisterComponent, NetworkedComponent]
public sealed partial class LegOrganComponent : Component
{
    [DataField(required: true)]
    public LegParts Part;
}
