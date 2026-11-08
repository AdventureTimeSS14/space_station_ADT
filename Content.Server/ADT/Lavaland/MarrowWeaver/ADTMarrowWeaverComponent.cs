using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Lavaland.MarrowWeaver;

[RegisterComponent]
public sealed partial class ADTMarrowWeaverComponent : Component
{
    [DataField]
    public ProtoId<DamageTypePrototype> DamageType = "Piercing";

    [DataField]
    public float CalmMinDamage = 13f;

    [DataField]
    public float CalmMaxDamage = 16f;

    [DataField]
    public float AngryMinDamage = 15f;

    [DataField]
    public float AngryMaxDamage = 20f;

    [DataField]
    public ProtoId<ReagentPrototype> CalmPoison = "ADTSporeToxin";

    [DataField]
    public FixedPoint2 CalmPoisonAmount = 5;

    [DataField]
    public ProtoId<ReagentPrototype> AngryPoison = "ADTVenom";

    [DataField]
    public FixedPoint2 AngryPoisonAmount = 6;

    [DataField]
    public float AngrySpeedMultiplier = 2f;

    [DataField]
    public float AngryThreshold = 1f / 3f;

    [DataField]
    public float CalmThreshold = 0.5f;

    [DataField]
    public LocId AngryPopup = "adt-marrow-weaver-angry";

    [DataField]
    public LocId CalmPopup = "adt-marrow-weaver-calm";

    [ViewVariables]
    public bool Angry;
}

