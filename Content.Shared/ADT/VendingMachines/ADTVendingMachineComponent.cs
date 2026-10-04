using Content.Shared.Actions;
using Content.Shared.Stacks;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.VendingMachines
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class ADTVendingMachineComponent : Component
    {
        /// <summary>
        /// PrototypeID for the vending machine's inventory, see <see cref="VendingMachineInventoryPrototype"/>
        /// </summary>
        [DataField("pack", required: true)]
        public ProtoId<VendingMachineInventoryPrototype> PackPrototypeId;

        [DataField]
        public Dictionary<string, VendingMachineInventoryEntry> Inventory = new();

        [DataField]
        public Dictionary<string, VendingMachineInventoryEntry> EmaggedInventory = new();

        [DataField]
        public Dictionary<string, VendingMachineInventoryEntry> ContrabandInventory = new();

        [DataField]
        public bool Contraband;

        public int NextItemReturnedCount;
        public Color? NextItemPaintColor;

        [DataField]
        public bool Broken;

        /// <summary>
        /// The quality of the stock in the vending machine on spawn.
        /// Represents the percentage chance (0.0f = 0%, 1.0f = 100%) each set of items in the machine is fully-stocked.
        /// If not fully stocked, the stock will have a random value between 0 (inclusive) and max stock (exclusive).
        /// </summary>
        [DataField]
        public float InitialStockQuality = 1.0f;

        /// <summary>
        /// Audio entity used during restock in case the doafter gets canceled.
        /// </summary>
        [DataField]
        public EntityUid? RestockStream;

        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public double PriceMultiplier = 0.75;

        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public bool AllForFree = false;

        public ProtoId<StackPrototype> CreditStackPrototype = "Credit";

        [DataField]
        public string CurrencyType = "SpaceCash";

        [DataField]
        public SoundSpecifier SoundInsertCurrency =
            new SoundPathSpecifier("/Audio/ADT/Machines/polaroid2.ogg");

        [DataField]
        public SoundSpecifier SoundWithdrawCurrency =
            new SoundPathSpecifier("/Audio/ADT/Machines/polaroid1.ogg");

        [ViewVariables]
        public int Credits;

        public int NextItemCount = 1;

        [DataField]
        public Color UiButtonBorderColor = Color.FromHex("#4972A1");

        [DataField]
        public Color UiButtonBaseColor = Color.FromHex("#141F2F");

        [DataField]
        public Color UiButtonHoveredColor = Color.FromHex("#4972A1");

        [DataField]
        public Color UiButtonDisabledColor = Color.FromHex("#3f3f3fff");

        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public Dictionary<string, uint> ReturnedInventory = new();

        public const string ReturnedItemsContainerId = "ADTVendingReturnedItems";

    }

    [Serializable, NetSerializable, DataDefinition]
    public sealed partial class VendingMachineInventoryEntry
    {
        [DataField]
        public InventoryType Type;

        [DataField]
        public string ID;

        [DataField]
        public uint Amount;
        [ViewVariables(VVAccess.ReadWrite)]
        public int Price;

        [DataField]
        public uint MaxAmount;

        [DataField]
        public string? Category;

        public VendingMachineInventoryEntry(InventoryType type, string id, uint amount, int price, uint maxAmount, string? category = null)
        {
            Type = type;
            ID = id;
            Amount = amount;
            Price = price;
            MaxAmount = maxAmount;
            Category = category;
        }

        public VendingMachineInventoryEntry(VendingMachineInventoryEntry entry)
        {
            Type = entry.Type;
            ID = entry.ID;
            Amount = entry.Amount;
            Price = entry.Price;
            MaxAmount = entry.MaxAmount;
            Category = entry.Category;
        }
    }

    [Serializable, NetSerializable]
    public enum InventoryType : byte
    {
        Regular,
        Emagged,
        Contraband
    }

    [Serializable, NetSerializable]
    public enum VendingMachineVisuals
    {
        VisualState
    }

    [Serializable, NetSerializable]
    public enum VendingMachineVisualState
    {
        Normal,
        Off,
        Broken,
        Eject,
        Deny,
    }

    public enum VendingMachineVisualLayers : byte
    {
        /// <summary>
        /// Off / Broken. The other layers will overlay this if the machine is on.
        /// </summary>
        Base,
        /// <summary>
        /// Normal / Deny / Eject
        /// </summary>
        BaseUnshaded,
        /// <summary>
        /// Screens that are persistent (where the machine is not off or broken)
        /// </summary>
        Screen
    }

    [Serializable, NetSerializable]
    public enum ContrabandWireKey : byte
    {
        StatusKey,
        TimeoutKey
    }

    [Serializable, NetSerializable]
    public enum EjectWireKey : byte
    {
        StatusKey,
    }

    public sealed partial class VendingMachineSelfDispenseEvent : InstantActionEvent
    {

    };

    [Serializable, NetSerializable]
    public sealed class VendingMachineComponentState : ComponentState
    {
        public Dictionary<string, VendingMachineInventoryEntry> Inventory = new();

        public Dictionary<string, VendingMachineInventoryEntry> EmaggedInventory = new();

        public Dictionary<string, VendingMachineInventoryEntry> ContrabandInventory = new();

        public Dictionary<string, uint> ReturnedInventory = new();

        public bool Contraband;

        public bool Broken;
    }
}