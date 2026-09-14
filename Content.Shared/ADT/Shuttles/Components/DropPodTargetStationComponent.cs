using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Shuttles.Components;

/// <summary>
/// Marks a station as the target for drop pod launches.
/// Only beacons on grids belonging to this station will be shown in the drop pod console.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DropPodTargetStationComponent : Component
{
}
