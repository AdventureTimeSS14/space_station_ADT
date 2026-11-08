using System.Numerics;
using Content.Shared.DisplacementMap;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Cooking.Prototypes;

[Prototype("adtPlateLayout")]
public sealed partial class ADTPlateLayoutPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public Dictionary<int, List<ADTPlateSlot>> Layouts = new();

    [DataField]
    public Color ShadowColor = Color.Transparent;

    [DataField]
    public Vector2 ShadowOffset;

    public List<ADTPlateSlot>? GetLayout(int count)
    {
        for (var i = count; i > 0; i--)
        {
            if (Layouts.TryGetValue(i, out var layout))
                return layout;
        }

        return null;
    }
}

[DataDefinition]
public sealed partial class ADTPlateSlot
{
    [DataField]
    public Vector2 Offset;

    [DataField]
    public float Scale = 0.5f;

    [DataField]
    public float Rotation;

    [DataField]
    public DisplacementData? Displacement;
}
