
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.HandleItemState;

[Serializable, NetSerializable]
public enum HandleEnabledItemStateVisual : sbyte
{
    Visual,
    On,
    Off
}
