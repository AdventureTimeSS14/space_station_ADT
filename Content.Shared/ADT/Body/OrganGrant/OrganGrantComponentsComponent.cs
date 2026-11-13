using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Body.OrganGrant;

[RegisterComponent]
public sealed partial class OrganGrantComponentsComponent : Component
{
    [DataField(required: true)]
    [AlwaysPushInheritance]
    public ComponentRegistry Components = new();
}
