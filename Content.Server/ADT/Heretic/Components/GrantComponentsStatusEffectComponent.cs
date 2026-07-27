using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Heretic.Components;

// ADT: перенесено из Content.Goobstation.Server.ComponentsRegistry

[RegisterComponent]
public sealed partial class GrantComponentsStatusEffectComponent : Component
{
    [DataField(required: true)]
    [AlwaysPushInheritance]
    public ComponentRegistry Components { get; private set; } = new();
}
