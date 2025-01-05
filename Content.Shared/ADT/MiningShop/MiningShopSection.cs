using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.MiningShop;

[Prototype("miningShopSection")]
public sealed partial class SharedMiningShopSectionPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField(required: true)]
    public List<MiningShopEntry> Entries = new();
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial record MiningShopEntry
{
    [DataField(required: true)]
    public EntProtoId Id;

    [DataField]
    public string? Name;

    [DataField]
    public uint? Price;
}
