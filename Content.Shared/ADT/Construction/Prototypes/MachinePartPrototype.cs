using Content.Shared.ADT.Construction;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Construction.Prototypes;

[Prototype("machinePart")]
public sealed partial class MachinePartPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField(required: true)]
    public EntProtoId StockPartPrototype = string.Empty;

    [DataField]
    public List<EntProtoId> TierPrototypes = new();

    [DataField]
    public Dictionary<MachineStat, float> Stats = new();

    [DataField]
    public int SortPriority = 0;

    public EntProtoId GetTierPrototype(int tier)
    {
        return tier > 0 && tier <= TierPrototypes.Count
            ? TierPrototypes[tier - 1]
            : StockPartPrototype;
    }
}
