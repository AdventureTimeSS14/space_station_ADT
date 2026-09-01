using Robust.Shared.Serialization;

namespace Content.Shared.VendingMachines
{
    [NetSerializable, Serializable]
    public sealed class VendingMachineInterfaceState : BoundUserInterfaceState
    {
        public List<VendingMachineInventoryEntry> Inventory;
        //ADT-Economy-Start
        public double PriceMultiplier;
        public int Credits;
        //ADT-Economy-End
        public VendingMachineInterfaceState(List<VendingMachineInventoryEntry> inventory, double priceMultiplier, int credits) //ADT-Economy
        {
            Inventory = inventory;
            //ADT-Economy-Start
            PriceMultiplier = priceMultiplier;
            Credits = credits;
            //ADT-Economy-End
        }
    }
    //ADT-Economy-Start
    [Serializable, NetSerializable]
    public sealed class VendingMachineWithdrawMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed class VendingMachineEjectCountMessage : BoundUserInterfaceMessage
    {
        public readonly VendingMachineInventoryEntry Entry;
        public readonly int Count;
        public readonly Color? PaintColor; // ADT-Tweak
        public VendingMachineEjectCountMessage(VendingMachineInventoryEntry entry, int count, Color? paintColor = null) // ADT-Tweak
        {
            Entry = entry;
            Count = count;
            PaintColor = paintColor; // ADT-Tweak
        }
    }

    [Serializable, NetSerializable]
    public sealed class VendingMachineUserInfoMessage : BoundUserInterfaceMessage
    {
        public readonly int Balance;
        public readonly bool IgnoreBalance;

        public VendingMachineUserInfoMessage(int balance, bool ignoreBalance = false)
        {
            Balance = balance;
            IgnoreBalance = ignoreBalance;
        }
    }
    //ADT-Economy-End

    [Serializable, NetSerializable]
    public sealed class VendingMachineEjectMessage : BoundUserInterfaceMessage
    {
        public readonly InventoryType Type;
        public readonly string ID;
        public VendingMachineEjectMessage(InventoryType type, string id)
        {
            Type = type;
            ID = id;
        }
    }

    [Serializable, NetSerializable]
    public enum VendingMachineUiKey
    {
        Key,
    }
}
