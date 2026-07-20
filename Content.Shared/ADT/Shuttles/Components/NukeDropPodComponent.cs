using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.ADT.Shuttles.Components;

/// <summary>
/// Marks a grid as a syndicate drop pod.
/// Only grids with this component can be targeted and launched by <see cref="DropPodConsoleComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NukeDropPodComponent : Component
{
    /// <summary>
    /// Whether this drop pod has already been launched and is unavailable for further launches.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Launched = false;

    /// <summary>
    /// The station grid this pod will merge into when it lands.
    /// Set by DropPodConsoleSystem at launch time (server-only, not networked).
    /// </summary>
    public EntityUid? TargetStationGrid;

    /// <summary>
    /// Game-time at which to spawn the pre-landing effect prototype (server-only).
    /// Null when no spawn is pending.
    /// </summary>
    public TimeSpan? PendingSpawnAt;

    /// <summary>
    /// Coordinates at which to spawn the pre-landing effect (server-only).
    /// </summary>
    public EntityCoordinates? PendingSpawnCoords;

    /// <summary>
    /// Prototype ID to spawn at <see cref="PendingSpawnCoords"/> when <see cref="PendingSpawnAt"/> is reached (server-only).
    /// </summary>
    public string? PendingSpawnPrototype;
}