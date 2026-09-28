using Robust.Shared.Prototypes;

<<<<<<< HEAD:Content.Shared/ADT/VendingMachines/VendingMachineInventoryPrototype.cs
namespace Content.Shared.ADT.VendingMachines
=======
namespace Content.Shared.VendingMachines;

[Prototype]
public sealed partial class VendingMachineInventoryPrototype : IPrototype
>>>>>>> wizards-filtered:Content.Shared/VendingMachines/VendingMachineInventoryPrototype.cs
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

<<<<<<< HEAD:Content.Shared/ADT/VendingMachines/VendingMachineInventoryPrototype.cs
        [DataField("startingInventory", customTypeSerializer: typeof(VendingMachineInventorySerializer))]
        public Dictionary<string, VendingMachineInventoryData> StartingInventory { get; private set; } = new();
=======
    [DataField]
    public Dictionary<EntProtoId, uint> StartingInventory { get; private set; } = new();
>>>>>>> wizards-filtered:Content.Shared/VendingMachines/VendingMachineInventoryPrototype.cs

    [DataField]
    public Dictionary<EntProtoId, uint>? EmaggedInventory { get; private set; }

    [DataField]
    public Dictionary<EntProtoId, uint>? ContrabandInventory { get; private set; }
}
