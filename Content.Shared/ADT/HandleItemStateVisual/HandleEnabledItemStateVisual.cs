using Robust.Shared.Serialization;

namespace Content.Shared.ADT.HandleItemStateVisual;

[Serializable, NetSerializable]
public enum HandleEnabledItemStateVisual : sbyte
{
    Visual,
    On,
    Off
}
