using System.Numerics;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Cooking.Prototypes;

[Prototype("adtPlateLayout")]
public sealed partial class ADTPlateLayoutPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public List<List<ADTPlateRow>> Arrangements = new();

    [DataField]
    public Color ShadowColor = Color.Transparent;

    [DataField]
    public Vector2 ShadowOffset;

    [DataField]
    public float MaxHeight = 22f;

    [DataField]
    public float MinScale = 0.25f;

    [DataField]
    public float MaxScale = 1f;

    [DataField]
    public float Spacing = 0.75f;
}

[DataDefinition]
public sealed partial class ADTPlateRow
{
    [DataField]
    public Vector2 Offset;

    [DataField]
    public float Width = 24f;
}
