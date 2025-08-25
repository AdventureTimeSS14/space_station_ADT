using Robust.Shared.Serialization;
using Content.Shared.Actions;

namespace Content.Shared.ADT.Ghost;

[Serializable, NetSerializable]
public enum GhostInfoUiKey : byte
{
    Key
}

public sealed partial class GhostInfoActionEvent : InstantActionEvent
{
}
