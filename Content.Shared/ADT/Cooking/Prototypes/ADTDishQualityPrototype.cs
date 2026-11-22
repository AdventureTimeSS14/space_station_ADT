using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Cooking.Prototypes;

[Prototype("adtDishQuality")]
public sealed partial class ADTDishQualityPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public int Level;

    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public Color Color = Color.White;

    [DataField]
    public float NutritionMultiplier = 1f;

    [DataField]
    public EntProtoId? WellFedEffect;

    [DataField]
    public TimeSpan WellFedDuration = TimeSpan.Zero;
}
