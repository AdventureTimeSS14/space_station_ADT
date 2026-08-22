using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.OreFurnace.Prototypes;

[Prototype("oreSmeltPack")]
public sealed partial class OreSmeltPackPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public List<ProtoId<OreSmeltRecipePrototype>> Recipes = new();
}
