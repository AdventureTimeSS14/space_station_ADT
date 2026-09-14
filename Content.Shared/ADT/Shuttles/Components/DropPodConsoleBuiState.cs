using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Shuttles.Components;

/// <summary>
/// State sent to the client: the list of valid (non-blacklisted) landing beacons and launch readiness.
/// </summary>
[Serializable, NetSerializable]
public sealed class DropPodConsoleBuiState : BoundUserInterfaceState
{
    /// <summary>Beacons available for targeting (blacklisted ones are excluded).</summary>
    public List<DropPodBeaconInfo> ValidBeacons { get; init; } = new();
    public bool CanLaunch { get; init; }
    public bool AlreadyLaunched { get; init; }
    /// <summary>Remaining cooldown in whole seconds; zero when ready.</summary>
    public int CooldownRemaining { get; init; }
    /// <summary>Station grid to display on the nav map.</summary>
    public NetEntity? StationGrid { get; init; }
    /// <summary>World-space centroid of all beacons, used to centre the nav map view.</summary>
    public Vector2 StationWorldCenter { get; init; }
    public int TcBalance { get; init; }
    public int CurrentCost { get; init; }
    public bool IsAtWar { get; init; }
    public int WarCooldownRemaining { get; init; }
}
