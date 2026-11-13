using Content.Shared.Damage;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.CleavingSaw;

[RegisterComponent]
public sealed partial class ADTCleavingSawComponent : Component
{
    [DataField]
    public float OpenAttackRate = 1.25f;

    [DataField]
    public float ClosedAttackRate = 2.5f;

    [DataField]
    public TimeSpan TransformAttackDelay = TimeSpan.FromSeconds(0.2);

    [DataField]
    public EntityWhitelist? NemesisWhitelist;

    [DataField]
    public DamageSpecifier NemesisBonusDamage = new();

    [DataField]
    public float ClumsyChance = 0.5f;

    [DataField]
    public DamageSpecifier ClumsyDamage = new();

    [DataField]
    public int BleedStart = 3;

    [DataField]
    public int BleedBuildup = 3;

    [DataField]
    public int BleedCap = 10;

    [DataField]
    public TimeSpan BleedTickInterval = TimeSpan.FromSeconds(0.6);

    [DataField]
    public TimeSpan BleedDecayDelay = TimeSpan.FromSeconds(0.5);

    [DataField]
    public DamageSpecifier BleedDamage = new();

    [DataField]
    public SoundSpecifier? BleedBurstSound;

    [DataField]
    public EntProtoId? BleedBurstEffect;
}

[RegisterComponent]
public sealed partial class ADTSawBleedComponent : Component
{
    [ViewVariables]
    public int Amount;

    [ViewVariables]
    public int Cap;

    [ViewVariables]
    public TimeSpan NextTick;

    [ViewVariables]
    public TimeSpan TickInterval;

    [ViewVariables]
    public TimeSpan DecayDelay;

    [ViewVariables]
    public DamageSpecifier Damage = new();

    [ViewVariables]
    public SoundSpecifier? BurstSound;

    [ViewVariables]
    public EntProtoId? BurstEffect;

    [ViewVariables]
    public EntityUid? Source;
}
