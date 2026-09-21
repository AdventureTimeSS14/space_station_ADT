using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Hierophant.Loot;

[RegisterComponent, NetworkedComponent]
public sealed partial class HierophantBeaconComponent : Component
{
    [DataField]
    public EntityUid? Club;
}

[Serializable, NetSerializable]
public enum HierophantBeaconVisuals : byte
{
    Charging,
}

[Serializable, NetSerializable]
public enum HierophantClubVisuals : byte
{
    Ready,
    HasBeacon,
}
