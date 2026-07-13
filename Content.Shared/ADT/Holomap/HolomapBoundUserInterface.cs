using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Holomap;

[Serializable, NetSerializable]
public sealed class HolomapBoundUserInterfaceState : BoundUserInterfaceState
{
    public HolomapMode Mode;

    public HolomapBoundUserInterfaceState(HolomapMode mode)
    {
        Mode = mode;
    }
}

[Serializable, NetSerializable]
public sealed class HolomapModeSelectedMessage : BoundUserInterfaceMessage
{
    public HolomapMode Mode;

    public HolomapModeSelectedMessage(HolomapMode mode)
    {
        Mode = mode;
    }
}
