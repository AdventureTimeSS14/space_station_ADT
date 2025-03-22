using Content.Shared.Damage;

namespace Content.Shared.ADT.Weapons.Melee;

[RegisterComponent]

public sealed partial class MeleeThrowOnHitDamageComponent : Component
{
    [DataField]
    public DamageSpecifier CollideDamage = new();

    [DataField]
    public DamageSpecifier ToCollideDamage = new();
}
