using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Artillery;

[Serializable, NetSerializable]
public sealed class ArtilleryConsoleBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly List<ArtilleryBeaconEntry> Beacons;
    public readonly NetEntity SelectedBeacon;
    public readonly TimeSpan CooldownRemaining;
    public readonly bool HasCannon;
    public readonly bool CanFire;
    public readonly bool CanToggle;
    public readonly bool CannonEnabled;
    public readonly float ReceivedPower;
    public readonly float RequiredPower;
    public readonly bool FullyPowered;

    public ArtilleryConsoleBoundUserInterfaceState(
        List<ArtilleryBeaconEntry> beacons,
        NetEntity selectedBeacon,
        TimeSpan cooldownRemaining,
        bool hasCannon,
        bool canFire,
        bool canToggle,
        bool cannonEnabled,
        float receivedPower,
        float requiredPower,
        bool fullyPowered)
    {
        Beacons = beacons;
        SelectedBeacon = selectedBeacon;
        CooldownRemaining = cooldownRemaining;
        HasCannon = hasCannon;
        CanFire = canFire;
        CanToggle = canToggle;
        CannonEnabled = cannonEnabled;
        ReceivedPower = receivedPower;
        RequiredPower = requiredPower;
        FullyPowered = fullyPowered;
    }
}

[Serializable, NetSerializable]
public readonly record struct ArtilleryBeaconEntry(NetEntity NetEntity, string Name);

[Serializable, NetSerializable]
public sealed class ArtilleryConsoleSelectBeaconMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity Beacon;

    public ArtilleryConsoleSelectBeaconMessage(NetEntity beacon)
    {
        Beacon = beacon;
    }
}

[Serializable, NetSerializable]
public sealed class ArtilleryConsoleFireMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class ArtilleryConsoleToggleMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public enum ArtilleryConsoleUiKey
{
    Key
}

[ByRefEvent]
public record struct ArtilleryCannonFireEvent(MapCoordinates TargetMapCoords);

