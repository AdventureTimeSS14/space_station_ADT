using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Lavaland.Components;

[RegisterComponent]
public sealed partial class ADTDragonStrengthStatusEffectComponent : Component
{
    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(3);

    [DataField]
    public FixedPoint2 DamageThreshold = 70;

    [DataField]
    public FixedPoint2 MultiplierOffset = 60;

    [DataField]
    public FixedPoint2 MultiplierScale = 50;

    [DataField]
    public float MaxMultiplier = 3f;

    [DataField]
    public Dictionary<ProtoId<DamageGroupPrototype>, FixedPoint2> Healing = new();

    [DataField]
    public float HopeChance = 0.05f;

    [DataField]
    public float WarChance = 0.02f;

    [DataField]
    public List<LocId> HopeMessages = new();

    [DataField]
    public List<LocId> WarMessages = new();

    [ViewVariables]
    public EntityUid? Target;

    [ViewVariables]
    public TimeSpan NextTick;
}
