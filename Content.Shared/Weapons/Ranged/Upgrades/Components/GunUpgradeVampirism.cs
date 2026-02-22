// Module ADT
using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Upgrades.Components;

/// <summary>
/// A <see cref="GunUpgradeComponent"/> that grants vampirism — heals the shooter on projectile hit via negative damage.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(GunUpgradeSystem))]
public sealed partial class GunUpgradeVampirismComponent : Component
{
    [DataField]
    public DamageSpecifier DamageOnHit = new();
}

[RegisterComponent, NetworkedComponent, Access(typeof(GunUpgradeSystem))]
public sealed partial class ProjectileVampirismComponent : Component
{
    [DataField]
    public DamageSpecifier DamageOnHit = new();
}
