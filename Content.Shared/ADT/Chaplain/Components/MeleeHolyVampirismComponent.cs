using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Chaplain.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(HolyVampirismSystem))]
public sealed partial class MeleeHolyVampirismComponent : Component
{
    [DataField]
    public DamageSpecifier HealOnHit = new();
}