using Content.Shared.ADT.VendingMachines;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.VendingMachines
{
    [Prototype]
    public sealed partial class VendingMachineInventoryPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        // ADT-Twek start
        [DataField("startingInventory", customTypeSerializer: typeof(VendingMachineInventorySerializer))]
        public Dictionary<string, VendingMachineInventoryData> StartingInventory { get; private set; } = new();
        // ADT-Twek end

        [DataField("emaggedInventory", customTypeSerializer:typeof(PrototypeIdDictionarySerializer<uint, EntityPrototype>))]
        public Dictionary<string, uint>? EmaggedInventory { get; private set; }

        [DataField("contrabandInventory", customTypeSerializer:typeof(PrototypeIdDictionarySerializer<uint, EntityPrototype>))]
        public Dictionary<string, uint>? ContrabandInventory { get; private set; }
    }
}
