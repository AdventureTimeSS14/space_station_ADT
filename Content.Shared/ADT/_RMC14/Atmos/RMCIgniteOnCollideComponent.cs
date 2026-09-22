// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License

using Content.Shared.Damage;
using Content.Shared.Physics;
using Content.Shared.Whitelist;

namespace Content.Shared._RMC14.Atmos;

[RegisterComponent]
public sealed partial class RMCIgniteOnCollideComponent : Component
{
    [DataField]
    public int? MaxStacks;

    [DataField]
    public int Intensity = 15;

    [DataField]
    public int Duration = 55;

    [ViewVariables]
    public bool InitDamaged;

    [DataField]
    public DamageSpecifier? TileDamage;

    [DataField]
    public float ArmorMultiplier = 1;

    [DataField]
    public EntityWhitelist? ArmorWhitelist;

    [DataField]
    public bool BurnsInVacuum;

    [DataField]
    public TimeSpan VacuumBurnout = TimeSpan.FromSeconds(1.5);

    [DataField]
    public CollisionGroup Collision = CollisionGroup.HighImpassable | CollisionGroup.MidImpassable | CollisionGroup.LowImpassable | CollisionGroup.BulletImpassable | CollisionGroup.InteractImpassable;

    [DataField]
    public Color BurnColor = Color.Orange;
}
