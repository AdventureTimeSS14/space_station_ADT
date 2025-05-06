using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Cytology;

[Prototype]
public sealed class CellModifierPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = string.Empty;

    [DataField]
    public LocId Name;

    [DataField]
    public Color Color;

    [DataField]
    public readonly List<CellModifier> Modifiers = [];
}
