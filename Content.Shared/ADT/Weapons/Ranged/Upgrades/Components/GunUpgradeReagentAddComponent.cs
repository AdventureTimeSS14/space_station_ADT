using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.FixedPoint;

namespace Content.Shared.Weapons.Ranged.Upgrades.Components;

/// <summary>
/// A <see cref="GunUpgradeComponent"/> for increasing the damage of a gun's projectile.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(GunUpgradeSystem))]
public sealed partial class GunUpgradeReagentAddComponent : Component
{
    [DataField(required: true)]
    public string ReagentOnHit = "Water";

    [DataField(required: true)]
    public FixedPoint2 ReagentCount = FixedPoint2.New(1);
}

[RegisterComponent, NetworkedComponent, Access(typeof(GunUpgradeSystem))]
public sealed partial class ProjectileReagentAddComponent : Component
{
    [DataField(required: true)]
    public string ReagentOnHit = "Water";

    [DataField(required: true)]
    public FixedPoint2 ReagentCount = FixedPoint2.New(1);
}
