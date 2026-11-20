using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Chaplain.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class UnholyWardComponent : Component
{
    [DataField(required: true)]
    public DamageModifierSet Modifiers = default!;
}
