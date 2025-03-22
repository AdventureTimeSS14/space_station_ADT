using Content.Shared.Damage;

namespace Content.Shared.ADT.Weapons.Melee;

[RegisterComponent]

public sealed partial class MeleeThrownComponent : Component
{
    public DamageSpecifier CollideDamage = new();

    public DamageSpecifier ToCollideDamage = new();
}
