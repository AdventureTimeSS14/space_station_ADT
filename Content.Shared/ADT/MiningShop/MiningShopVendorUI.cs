using Robust.Shared.Serialization;

namespace Content.Shared.ADT.MiningShop;

[Serializable, NetSerializable]
public enum MiningShopUI : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class MiningShopBuiMsg(MiningShopEntry entry) : BoundUserInterfaceMessage
{
    public readonly MiningShopEntry Entry = entry;
}

[Serializable, NetSerializable]
public sealed class MiningShopExpressDeliveryBuiMsg() : BoundUserInterfaceMessage
{

}

[Serializable, NetSerializable]
public sealed class MiningShopRefreshBuiMsg : BoundUserInterfaceMessage;
