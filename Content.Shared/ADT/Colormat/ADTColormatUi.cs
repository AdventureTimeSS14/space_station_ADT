using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Colormat;

[Serializable, NetSerializable]
public enum ADTColormatUiKey : byte
{
    Key,
}


[Serializable, NetSerializable]
public sealed class ADTColormatUiState(NetEntity? item) : BoundUserInterfaceState
{
    public readonly NetEntity? Item = item;
}


[Serializable, NetSerializable]
public sealed class ADTColormatSetColorMessage(Color? color) : BoundUserInterfaceMessage
{
    public readonly Color? Color = color;
}


[Serializable, NetSerializable]
public sealed class ADTColormatEjectMessage : BoundUserInterfaceMessage;
