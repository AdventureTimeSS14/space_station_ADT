using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.VendingMachines
{
    [NetSerializable, Serializable]
    public sealed class VendingMachineInterfaceState : BoundUserInterfaceState
    {
        public List<VendingMachineInventoryEntry> Inventory;
        public double PriceMultiplier;
        public int Credits;

        public Dictionary<string, NetEntity> ReturnedEntities = new();

        public VendingMachineInterfaceState(List<VendingMachineInventoryEntry> inventory, double priceMultiplier, int credits,
            Dictionary<string, NetEntity>? returnedEntities = null)
        {
            Inventory = inventory;
            PriceMultiplier = priceMultiplier;
            Credits = credits;
            if (returnedEntities != null)
                ReturnedEntities = returnedEntities;
        }
    }

    [Serializable, NetSerializable]
    public sealed class VendingMachineWithdrawMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed class VendingMachineEjectCountMessage : BoundUserInterfaceMessage
    {
        public readonly VendingMachineInventoryEntry Entry;
        public readonly int Count;
        public readonly Color? PaintColor;
        public VendingMachineEjectCountMessage(VendingMachineInventoryEntry entry, int count, Color? paintColor = null)
        {
            Entry = entry;
            Count = count;
            PaintColor = paintColor;
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
