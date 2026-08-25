// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License
using Content.Shared.Damage;
using Content.Shared.Physics;
using Content.Shared.Whitelist;

namespace Content.Shared._RMC14.OnCollide;

[RegisterComponent]
public sealed partial class RMCDamageOnCollideComponent : Component
{
    [ViewVariables]
    public bool InitDamaged;

    [ViewVariables]
    public EntityUid? Chain;

    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    [DataField]
    public DamageSpecifier ChainDamage = new();

    [ViewVariables]
    public HashSet<EntityUid> Damaged = new();

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public bool DamageDead;

    [DataField]
    public bool Fire;

    [DataField]
    public CollisionGroup Collision = CollisionGroup.HighImpassable | CollisionGroup.MidImpassable | CollisionGroup.LowImpassable | CollisionGroup.BulletImpassable | CollisionGroup.InteractImpassable;

    [DataField]
    public int DirectHitMultiplier = 3;

    [DataField]
    public TimeSpan Paralyze;

    [DataField]
    public bool IgnoreResistances;

    [DataField]
    public int ArmorPenetration;

    [DataField]
    public bool CanRehit;

    [ViewVariables]
    public bool Disabled;
}
