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
public sealed class BloodContractBuiState(List<BloodContractTargetInfo> targets) : BoundUserInterfaceState
{
    public List<BloodContractTargetInfo> Targets = targets;
}

[Serializable, NetSerializable]
public sealed class BloodContractSelectMessage(NetEntity target) : BoundUserInterfaceMessage
{
    public NetEntity Target = target;
}
