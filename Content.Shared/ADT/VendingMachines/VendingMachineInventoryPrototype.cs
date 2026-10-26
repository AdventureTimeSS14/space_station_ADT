using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.VendingMachines;

[Prototype]
public sealed partial class VendingMachineInventoryPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("startingInventory", customTypeSerializer: typeof(VendingMachineInventorySerializer))]
    public Dictionary<string, VendingMachineInventoryData> StartingInventory { get; private set; } = new();

    [DataField]
    public Dictionary<EntProtoId, uint>? EmaggedInventory { get; private set; }

    [DataField]
    public Dictionary<EntProtoId, uint>? ContrabandInventory { get; private set; }
}
