using Content.Shared.ADT.Grab;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.MartialArts;

[Prototype("martialArt")]
public sealed class MartialArtPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private init; } = default!;

    [DataField]
    public MartialArtsForms MartialArtsForm = MartialArtsForms.CloseQuartersCombat;

    [DataField]
    public int MinRandomDamageModifier;

    [DataField]
    public int MaxRandomDamageModifier = 5;

    [DataField]
    public FixedPoint2 BaseDamageModifier;

    [DataField]
    public ProtoId<DamageTypePrototype> DamageModifierType = "Blunt";

    [DataField]
    public bool RandomDamageModifier;

    [DataField]
    public ProtoId<ComboListPrototype> RoundstartCombos = "CQCMoves";

    [DataField]
    public List<LocId> RandomSayings = [];

    [DataField]
    public List<LocId> RandomSayingsDowned = [];

    [DataField]
    public GrabStage StartingStage = GrabStage.Soft;
}
