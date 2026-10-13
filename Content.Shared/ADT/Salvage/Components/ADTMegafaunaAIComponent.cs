using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Salvage.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ADTMegafaunaAIComponent : Component
{
    [DataField]
    public TimeSpan DecisionInterval = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextDecisionAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextRangedAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan RecoveryUntil;

    [DataField]
    public bool RangedWhenAdjacent;

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
}
