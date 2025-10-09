namespace Content.Server.ADT.Salvage.Components;

/// <summary>
///     Marker component for the miner's jaunter item to enable special use behavior.
/// </summary>
[RegisterComponent]
public sealed partial class JaunterComponent : Component
{
}

/// <summary>
///     Marker component attached to portals spawned by the jaunter in order
///     to adjust despawn timers on first entry.
/// </summary>
[RegisterComponent]
public sealed partial class JaunterPortalComponent : Component
{
}

/// <summary>
///     Marker component for black kill-portal behavior when no beacons are available.
/// </summary>
[RegisterComponent]
public sealed partial class JaunterKillPortalComponent : Component
{
}
