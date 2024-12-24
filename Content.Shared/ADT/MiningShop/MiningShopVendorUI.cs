using Robust.Shared.Serialization;

namespace Content.Shared.ADT.MiningShop;

[Serializable, NetSerializable]
public enum MiningShopUI : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class MiningShopBuiMsg(int section, int entry) : BoundUserInterfaceMessage
{
    public readonly int Section = section;
    public readonly int Entry = entry;
}

[Serializable, NetSerializable]
public sealed class MiningShopExpressDeliveryBuiMsg() : BoundUserInterfaceMessage
{

}

[Serializable, NetSerializable]
public sealed class MiningShopRefreshBuiMsg : BoundUserInterfaceMessage;
