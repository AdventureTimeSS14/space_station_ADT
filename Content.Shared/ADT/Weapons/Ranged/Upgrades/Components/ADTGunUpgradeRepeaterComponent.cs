using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Weapons.Ranged.Upgrades.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTGunUpgradeRepeaterComponent : Component
{
    [DataField]
    public float RechargeCoefficient = 4f;

    [DataField]
    public float HitCoefficient = 0.25f;

    [DataField]
    public float FireRateCoefficient = 2.5f;
}

[RegisterComponent]
public sealed partial class ADTProjectileRepeaterComponent : Component
{
    [DataField]
    public EntityUid? Gun;

    [DataField]
    public float HitCoefficient = 0.25f;
}
