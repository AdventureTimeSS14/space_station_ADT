using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Cooking.Prototypes;

[Prototype("adtCookingRecipe")]
public sealed partial class ADTCookingRecipePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public ADTCookingMethod Method = ADTCookingMethod.Fried;

    [DataField(required: true)]
    public ADTCookwareType Cookware = ADTCookwareType.Pan;

    [DataField]
    public List<ADTCookingIngredient> Ingredients = new();

    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> Reagents = new();

    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> BonusReagents = new();

    [DataField]
    public bool RequireMedium;

    [DataField]
    public List<ProtoId<ADTCookingMediumPrototype>> PreferredMedia = new();

    [DataField]
    public float Time = 20f;

    [DataField]
    public float? OvercookTime;

    [DataField]
    public float? BurnTime;

    [DataField]
    public EntProtoId? Result;

    [DataField]
    public int ResultCount = 1;

    [DataField]
    public LocId? ServingName;

    [DataField]
    public bool ConsumePlate;

    [DataField]
    public bool Secret;

    [DataField]
    public string Group = "Other";

    [DataField]
    public bool HideInGuidebook;

    public int IngredientCount()
    {
        var count = 0;
        foreach (var ingredient in Ingredients)
        {
            count += ingredient.Count;
        }

        return count;
    }
}

[DataDefinition]
public sealed partial class ADTCookingIngredient
{
    [DataField]
    public EntProtoId? Id;

    [DataField]
    public ProtoId<TagPrototype>? Tag;

    [DataField]
    public int Count = 1;
}
