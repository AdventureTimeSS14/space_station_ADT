using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Weapons.Ranged.Upgrades.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTGunUpgradeDamageComponent : Component
{
    [DataField]
    public DamageSpecifier Damage = new();

    [DataField]
    public bool SplitAcrossProjectiles = true;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTGunUpgradeRangeComponent : Component
{
    [DataField]
    public float Coefficient = 1.34f;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTGunUpgradeFireRateComponent : Component
{
    [DataField]
    public float Coefficient = 1.2f;

    [DataField]
    public float RechargeCoefficient = 0.8f;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTGunUpgradeVampirismComponent : Component
{
    [DataField]
    public DamageSpecifier DamageOnHit = new();
}

[RegisterComponent]
public sealed partial class ADTProjectileVampirismComponent : Component
{
    [DataField]
    public DamageSpecifier DamageOnHit = new();
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTGunUpgradeIndoorsComponent : Component
{
    [DataField]
    public float Coefficient = 2f;
}
