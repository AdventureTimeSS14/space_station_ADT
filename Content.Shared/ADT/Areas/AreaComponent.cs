using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Areas;

/// <summary>
/// Marker component for all areas, used for area lookup.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AreaComponent : Component;