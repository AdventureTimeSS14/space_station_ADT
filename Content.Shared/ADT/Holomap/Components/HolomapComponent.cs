using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Holomap;

[RegisterComponent, NetworkedComponent]
public sealed partial class HolomapComponent : Component
{
    [DataField("mode")]
    public HolomapMode Mode = HolomapMode.Battlemap;
}

[Serializable, NetSerializable]
public enum HolomapMode : byte
{
    Battlemap,
    Lavaland
}

[Serializable, NetSerializable]
public enum HolomapVisuals : byte
{
    Powered,
    Mode
}
