using Content.Shared.Materials;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.OreFurnace.Prototypes;

[Prototype("oreSmeltRecipe")]
public sealed partial class OreSmeltRecipePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public LocId? Name;

    [DataField(required: true)]
    public EntProtoId Result;

    [DataField(required: true)]
    public Dictionary<ProtoId<MaterialPrototype>, int> Materials = new();

    [DataField]
    public uint MiningPoints;

    [DataField]
    public SpriteSpecifier? Icon;
}
