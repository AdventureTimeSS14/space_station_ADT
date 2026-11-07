using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Cooking.Prototypes;

[Prototype("adtCookingMedium")]
public sealed partial class ADTCookingMediumPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    [DataField(required: true)]
    public ADTCookingMethod Method = ADTCookingMethod.Fried;

    [DataField]
    public FixedPoint2 MinAmount = 5;

    [DataField]
    public FixedPoint2 Consumption = 5;

    [DataField]
    public bool Flammable;

    [DataField]
    public List<ProtoId<TagPrototype>> BonusTags = new();

    [DataField]
    public List<EntProtoId> BonusIngredients = new();

    [DataField]
    public Dictionary<ADTStoveHeat, float> BurnMultipliers = new();

    [DataField]
    public Dictionary<ADTStoveHeat, float> SmokeToFire = new();
}
