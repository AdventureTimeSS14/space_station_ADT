using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Fishing;

[Serializable, NetSerializable]
public enum ADTFishingUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class ADTFishingHoldMessage : BoundUserInterfaceMessage
{
    public bool Holding;

    public ADTFishingHoldMessage(bool holding)
    {
        Holding = holding;
    }
}
