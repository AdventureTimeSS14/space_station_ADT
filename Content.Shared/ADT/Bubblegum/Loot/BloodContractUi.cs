using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Bubblegum.Loot;

[Serializable, NetSerializable]
public enum BloodContractUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public struct BloodContractTargetInfo
{
    public NetEntity Entity;
    public string Name;
}

[Serializable, NetSerializable]
public sealed class BloodContractBuiState : BoundUserInterfaceState
{
    public List<BloodContractTargetInfo> Targets;

    public BloodContractBuiState(List<BloodContractTargetInfo> targets)
    {
        Targets = targets;
    }
}

[Serializable, NetSerializable]
public sealed class BloodContractSelectMessage : BoundUserInterfaceMessage
{
    public NetEntity Target;

    public BloodContractSelectMessage(NetEntity target)
    {
        Target = target;
    }
}
