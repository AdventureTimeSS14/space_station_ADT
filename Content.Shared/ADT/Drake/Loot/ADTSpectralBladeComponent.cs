using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Drake.Loot;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ADTSpectralBladeComponent : Component
{
    [DataField(required: true)]
    public DamageSpecifier DamagePerSpirit = new();

    [DataField]
    public FixedPoint2 MaxDamage = 75;

    [DataField]
    public float BlockChancePerSpirit = 0.05f;

    [DataField]
    public float MaxBlockChance = 0.75f;

    [DataField]
    public TimeSpan SummonCooldown = TimeSpan.FromSeconds(60);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextSummon;

    [ViewVariables]
    public EntityUid? Wielder;

    [ViewVariables]
    public HashSet<EntityUid> Spirits = new();
}

[RegisterComponent]
public sealed partial class ADTSpectralBladeWielderComponent : Component
{
    [ViewVariables]
    public HashSet<EntityUid> Blades = new();
}

[RegisterComponent]
public sealed partial class ADTSpectralSpiritComponent : Component
{
    [ViewVariables]
    public int Blades;
}
