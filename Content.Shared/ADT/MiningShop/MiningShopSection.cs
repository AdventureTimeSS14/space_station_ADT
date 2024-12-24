using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.MiningShop;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class SharedMiningShopSection
{
    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField(required: true)]
    public List<MiningShopEntry> Entries = new();

    // Only used by Spec Vendors to mark the kit section for RMCVendorSpecialistComponent logic.
    [DataField]
    public int? SharedSpecLimit;

    [DataField]
    public List<ProtoId<JobPrototype>> Jobs = new();
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
