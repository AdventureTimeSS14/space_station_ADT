using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Shuttles.Components;

/// <summary>
/// A console that allows launching a drop pod at a chosen beacon on the station.
/// Must be placed on a grid that has <see cref="DropPodComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DropPodConsoleComponent : Component
{
    /// <summary>
    /// Beacon prototype IDs that cannot be targeted.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string>? BeaconBlacklist;

    /// <summary>
    /// Total flight time in seconds from launch to impact. Also used as the announcement lead time.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FlightTime = 30f;

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(120);

    [DataField]
    public TimeSpan LastLaunchTime = TimeSpan.Zero;

    /// <summary>
    /// Prototype spawned at the landing site before the pod arrives.
    /// Null disables the effect entirely.
    /// </summary>
    [DataField]
    public string? PreLandingSpawnPrototype = "ADTDroppodTarget";

    /// <summary>
    /// How many seconds before landing to spawn <see cref="PreLandingSpawnPrototype"/>.
    /// </summary>
    [DataField]
    public float PreLandingSpawnLeadTime = 15f;

    /// <summary>
    /// Cost of launching the drop pod in peacetime.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int PeaceCost = 35;

    /// <summary>
    /// Cost of launching the drop pod during war (discounted).
    /// </summary>
    [DataField, AutoNetworkedField]
    public int WarCost = 15;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? WarDeclaredTime;
}

[ByRefEvent]
public record struct DropPodWarSyncEvent(TimeSpan? WarDeclaredTime);

[Serializable, NetSerializable]
public enum DropPodConsoleUiKey : byte { Key }

/// <summary>
/// Identifies a valid (non-blacklisted) landing beacon sent to the client UI.
/// </summary>
[Serializable, NetSerializable]
public sealed class DropPodBeaconInfo
{
    public NetEntity Uid { get; init; }
    public string Name { get; init; } = string.Empty;
    /// <summary>World-space position used to highlight this beacon on the nav map.</summary>
    public Vector2 WorldPos { get; init; }
}

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

/// <summary>
/// Sent by the client to request launching toward a specific beacon.
/// The server applies a random offset so the exact tile is never revealed in advance.
/// </summary>
[Serializable, NetSerializable]
public sealed class DropPodConsoleDeployMessage : BoundUserInterfaceMessage
{
    public NetEntity TargetBeacon { get; init; }
}
