using Content.Shared.Research.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.SeedDna.Prototypes;

[Prototype("seedDnaGene")]
public sealed partial class SeedDnaGenePrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField("valueType")] public SeedDnaGeneType ValueType = SeedDnaGeneType.Float;
    [DataField] public float Min;
    [DataField] public float Max;
    [DataField] public ProtoId<TechnologyPrototype>? RequiredTechnology;
    [DataField] public int Cost;
    [DataField] public float CostPerUnit;
    [DataField] public int SellPrice;
    [DataField] public LocId? Description;
}

public enum SeedDnaGeneType : byte
{
    Float,
    Int,
    Bool,
    HarvestType,
    Chemical,
    Gas,
}
