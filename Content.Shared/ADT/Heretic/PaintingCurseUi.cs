using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Heretic;

[Serializable, NetSerializable]
public enum PaintingCurseUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public struct PaintingCurseTargetInfo
{
    public NetEntity Entity;
    public string Name;
}

[Serializable, NetSerializable]
public sealed class PaintingCurseBuiState(List<PaintingCurseTargetInfo> targets) : BoundUserInterfaceState
{
    public List<PaintingCurseTargetInfo> Targets = targets;
}

[Serializable, NetSerializable]
public sealed class PaintingCurseSelectMessage(NetEntity target) : BoundUserInterfaceMessage
{
    public NetEntity Target = target;
}
