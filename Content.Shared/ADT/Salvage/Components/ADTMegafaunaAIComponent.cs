using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Salvage.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ADTMegafaunaAIComponent : Component
{
    [DataField]
    public TimeSpan DecisionInterval = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan DecisionJitterMin = TimeSpan.Zero;

    [DataField]
    public TimeSpan DecisionJitterMax = TimeSpan.Zero;

    [DataField]
    public bool MeleeOncePerDecision;

    [ViewVariables]
    public bool MeleeReady = true;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextDecisionAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextRangedAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan RecoveryUntil;

    [DataField]
    public bool RangedWhenAdjacent;

    [DataField]
    public bool AdjacentOnlyOnGrid;

    [DataField]
    public bool DecideWhileInactive = true;

    [DataField]
    public bool TargetAggressor = true;

    [DataField]
    public bool HandleMelee = true;

    [ViewVariables]
    public EntityUid? ManualTarget;

    [ViewVariables]
    public EntityUid? LastTarget;

    [DataField]
    public float AngerDamageDivisor = 50f;

    [DataField]
    public float MaxAnger = 20f;

    [ViewVariables]
    public float AngerModifier;

    [DataField]
    public List<EntProtoId> Actions = new();

    [DataField]
    public float DevourHealFraction = 0.5f;

    [DataField]
    public ProtoId<DamageGroupPrototype> DevourHealGroup = "Brute";

    [DataField]
    public bool SmashObstacles = true;

    [DataField]
    public float SmashReach = 0.6f;

    [DataField]
    public TimeSpan SmashCooldown = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextSmashAt;

    [DataField]
    public DamageSpecifier SmashDamage = new()
    {
        DamageDict = new()
        {
            { "Structural", 400 },
        },
    };
}
