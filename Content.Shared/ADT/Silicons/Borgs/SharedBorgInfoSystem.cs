using Robust.Shared.Serialization;
using Content.Shared.Actions;

namespace Content.Shared.ADT.Silicons.Borgs;

[Serializable, NetSerializable]
public enum BorgInfoUiKey : byte
{
    BorgInfo
}

public sealed partial class BorgInfoActionEvent : InstantActionEvent
{
}
